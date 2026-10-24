import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, map } from 'rxjs';
import {
  LucideArrowDown,
  LucideArrowUp,
  LucideArrowUpDown,
  LucideChevronDown,
  LucideChevronLeft,
  LucideChevronRight,
  LucideSearch,
  LucideX,
} from '@lucide/angular';
import { StatusRecebivel } from '../../models/api.models';
import { NotaAnalisada } from '../../models/analise.models';
import { MotivoExplicado, explicarMotivo, tituloMotivo } from '../../shared/motivos/motivos-catalogo';
import { StatusBadge } from '../../shared/ui/status-badge';
import { BrlPipe, CnpjPipe, DataBrPipe, InteiroPipe } from '../../shared/ui/pipes';

export type FiltroStatus = 'todas' | StatusRecebivel;
type Coluna = 'documento' | 'sacado' | 'vencimento' | 'valor' | 'status' | 'liquido';
interface Ordenacao {
  coluna: Coluna;
  direcao: 'asc' | 'desc';
}

const TAMANHO_PAGINA = 25;
const ORDEM_STATUS: Record<StatusRecebivel, number> = {
  APROVADA: 0,
  PENDENTE_VALIDACAO: 1,
  RECUSADA: 2,
};
const comparador = new Intl.Collator('pt-BR', { numeric: true, sensitivity: 'base' });

function normalizar(t: string): string {
  return t
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();
}

function chave(n: NotaAnalisada, coluna: Coluna): string | number {
  switch (coluna) {
    case 'documento':
      return n.numeroDocumento;
    case 'sacado':
      return n.csv?.nomeSacado ?? '';
    case 'vencimento':
      return n.csv?.dataVencimento ?? '';
    case 'valor':
      return n.valorNominal;
    case 'status':
      return ORDEM_STATUS[n.status];
    case 'liquido':
      return n.valorLiquido ?? -1;
  }
}

@Component({
  selector: 'app-notas-tabela',
  imports: [
    NgTemplateOutlet,
    ReactiveFormsModule,
    StatusBadge,
    BrlPipe,
    CnpjPipe,
    DataBrPipe,
    InteiroPipe,
    LucideSearch,
    LucideX,
    LucideArrowUp,
    LucideArrowDown,
    LucideArrowUpDown,
    LucideChevronDown,
    LucideChevronLeft,
    LucideChevronRight,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notas-tabela.html',
  styleUrl: './notas-tabela.scss',
})
export class NotasTabela {
  readonly notas = input.required<NotaAnalisada[]>();
  readonly status = input<FiltroStatus>('todas');
  readonly motivo = input<string | null>(null);

  readonly statusChange = output<FiltroStatus>();
  readonly motivoChange = output<string | null>();
  readonly abrir = output<number>();

  protected readonly busca = new FormControl('', { nonNullable: true });
  private readonly termo = toSignal(
    this.busca.valueChanges.pipe(
      debounceTime(150),
      map((t) => normalizar(t.trim())),
    ),
    {
      initialValue: '',
    },
  );

  protected readonly ordenacao = signal<Ordenacao | null>(null);
  protected readonly tituloMotivo = tituloMotivo;
  /** Índices das notas com o campo de motivos aberto na tabela. */
  protected readonly expandidas = signal<ReadonlySet<number>>(new Set());

  protected readonly contagens = computed(() => {
    const c = { todas: 0, APROVADA: 0, RECUSADA: 0, PENDENTE_VALIDACAO: 0 };
    for (const n of this.porMotivoEBusca()) {
      c.todas++;
      c[n.status]++;
    }
    return c;
  });

  protected readonly opcoesStatus = computed(() => {
    const c = this.contagens();
    const opcoes: { valor: FiltroStatus; rotulo: string; qtd: number }[] = [
      { valor: 'todas', rotulo: 'Todas', qtd: c.todas },
      { valor: 'APROVADA', rotulo: 'Aprovadas', qtd: c.APROVADA },
      { valor: 'RECUSADA', rotulo: 'Recusadas', qtd: c.RECUSADA },
    ];
    if (c.PENDENTE_VALIDACAO > 0)
      opcoes.push({ valor: 'PENDENTE_VALIDACAO', rotulo: 'Pendentes', qtd: c.PENDENTE_VALIDACAO });
    return opcoes;
  });

  private readonly porMotivoEBusca = computed(() => {
    const motivo = this.motivo();
    const termo = this.termo();
    const digitos = termo.replace(/\D/g, '');
    return this.notas().filter((n) => {
      if (motivo && !n.motivos.some((m) => m.codigo === motivo)) return false;
      if (!termo) return true;
      const campos = [n.numeroDocumento, n.csv?.nomeSacado ?? '', n.csv?.nomeCedente ?? ''];
      if (campos.some((c) => normalizar(c).includes(termo))) return true;
      return digitos.length >= 4 && (n.csv?.docSacado ?? '').replace(/\D/g, '').includes(digitos);
    });
  });

  /** Notas visíveis após filtros e ordenação — usado também para navegar no painel de detalhes. */
  readonly filtradas = computed(() => {
    const status = this.status();
    const lista =
      status === 'todas'
        ? [...this.porMotivoEBusca()]
        : this.porMotivoEBusca().filter((n) => n.status === status);
    const o = this.ordenacao();
    if (o) {
      const sinal = o.direcao === 'asc' ? 1 : -1;
      lista.sort((a, b) => {
        const ka = chave(a, o.coluna);
        const kb = chave(b, o.coluna);
        const r =
          typeof ka === 'number' && typeof kb === 'number'
            ? ka - kb
            : comparador.compare(String(ka), String(kb));
        return r * sinal || a.indice - b.indice;
      });
    }
    return lista;
  });

  protected readonly pagina = linkedSignal({ source: this.filtradas, computation: () => 0 });
  protected readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.filtradas().length / TAMANHO_PAGINA)),
  );
  protected readonly visiveis = computed(() => {
    const inicio = this.pagina() * TAMANHO_PAGINA;
    return this.filtradas().slice(inicio, inicio + TAMANHO_PAGINA);
  });
  protected readonly intervalo = computed(() => {
    const total = this.filtradas().length;
    const inicio = total === 0 ? 0 : this.pagina() * TAMANHO_PAGINA + 1;
    return { inicio, fim: Math.min(total, (this.pagina() + 1) * TAMANHO_PAGINA), total };
  });

  protected ordenar(coluna: Coluna): void {
    const atual = this.ordenacao();
    if (atual?.coluna !== coluna)
      this.ordenacao.set({
        coluna,
        direcao: coluna === 'valor' || coluna === 'liquido' ? 'desc' : 'asc',
      });
    else this.ordenacao.set({ coluna, direcao: atual.direcao === 'asc' ? 'desc' : 'asc' });
  }

  protected ariaSort(coluna: Coluna): 'ascending' | 'descending' | 'none' {
    const o = this.ordenacao();
    return o?.coluna === coluna ? (o.direcao === 'asc' ? 'ascending' : 'descending') : 'none';
  }

  protected alternarMotivos(indice: number): void {
    this.expandidas.update((atual) => {
      const novo = new Set(atual);
      if (!novo.delete(indice)) novo.add(indice);
      return novo;
    });
  }

  protected explicar(n: NotaAnalisada): MotivoExplicado[] {
    return n.motivos.map((m) => explicarMotivo(m, n, n.status === 'PENDENTE_VALIDACAO'));
  }

  protected irPara(p: number): void {
    this.pagina.set(Math.min(this.totalPaginas() - 1, Math.max(0, p)));
  }

  protected limparBusca(): void {
    this.busca.setValue('');
  }

  protected limparFiltros(): void {
    this.busca.setValue('');
    this.statusChange.emit('todas');
    this.motivoChange.emit(null);
  }
}
