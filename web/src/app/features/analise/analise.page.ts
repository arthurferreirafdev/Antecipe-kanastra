import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  LucideArrowDown,
  LucideArrowRight,
  LucideDownload,
  LucideDynamicIcon,
  LucidePlus,
} from '@lucide/angular';
import { AnaliseStore } from '../../core/state/analise.store';
import { StatusRecebivel } from '../../models/api.models';
import { NotaAnalisada } from '../../models/analise.models';
import { formatarData, formatarDataHora, formatarInteiro } from '../../shared/format/formatters';
import { explicarMotivo, tituloMotivo } from '../../shared/motivos/motivos-catalogo';
import { ComposicaoBar, SegmentoComposicao } from '../../shared/ui/composicao-bar';
import { Money } from '../../shared/ui/money';
import { BrlPipe, InteiroPipe, PctPipe } from '../../shared/ui/pipes';
import { SidePanel } from '../../shared/ui/side-panel';
import { Skeleton } from '../../shared/ui/skeleton';
import { NotaDetalhe } from './nota-detalhe';
import { FiltroStatus, NotasTabela } from './notas-tabela';

const STATUS_VALIDOS: readonly FiltroStatus[] = [
  'todas',
  'APROVADA',
  'RECUSADA',
  'PENDENTE_VALIDACAO',
];
const ROTULO_STATUS: Record<StatusRecebivel, string> = {
  APROVADA: 'Aprovada',
  RECUSADA: 'Recusada',
  PENDENTE_VALIDACAO: 'Pendente',
};

const decimalCsv = (v: number | null) => (v === null ? '' : v.toFixed(2).replace('.', ','));
const campoCsv = (t: string) => (/[;"\n]/.test(t) ? `"${t.replace(/"/g, '""')}"` : t);

@Component({
  selector: 'app-analise',
  imports: [
    RouterLink,
    Money,
    ComposicaoBar,
    Skeleton,
    SidePanel,
    NotasTabela,
    NotaDetalhe,
    BrlPipe,
    PctPipe,
    InteiroPipe,
    LucideArrowRight,
    LucideArrowDown,
    LucideDownload,
    LucidePlus,
    LucideDynamicIcon,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './analise.page.html',
  styleUrl: './analise.page.scss',
})
export class AnalisePage {
  private readonly store = inject(AnaliseStore);
  private readonly router = inject(Router);
  private readonly doc = inject(DOCUMENT);

  /** Query params (withComponentInputBinding). */
  readonly status = input<string>();
  readonly motivo = input<string>();
  readonly nota = input<string>();

  private readonly tabela = viewChild(NotasTabela);

  protected readonly analise = this.store.analise;
  protected readonly resumo = this.store.resumo;
  protected readonly notas = this.store.notas;
  protected readonly enviando = this.store.enviando;
  protected readonly guardada = this.store.guardadaNoNavegador;

  /** Oferta do CSV só com as aprovadas; a API só a envia quando houve recusadas ou duplicadas. */
  protected readonly download = computed(() => {
    const r = this.analise()?.resultado;
    return r?.csvSemRecusadas && r.perguntaDownload
      ? { pergunta: r.perguntaDownload, csv: r.csvSemRecusadas, duplicadas: r.duplicadas ?? [] }
      : null;
  });
  protected readonly downloadDispensado = signal(false);

  protected readonly filtroStatus = computed<FiltroStatus>(() => {
    const s = this.status() as FiltroStatus | undefined;
    return s && STATUS_VALIDOS.includes(s) ? s : 'todas';
  });
  protected readonly filtroMotivo = computed(() => this.motivo() || null);

  protected readonly quando = computed(() => {
    const a = this.analise();
    return a ? formatarDataHora(a.analisadaEm) : '';
  });

  protected readonly titulo = computed(() => {
    const r = this.resumo();
    if (!r) return '';
    const total = formatarInteiro(r.total);
    if (r.total === 1)
      return r.aprovadas === 1
        ? 'Sua nota pode ser antecipada.'
        : 'Sua nota não pode ser antecipada agora.';
    if (r.aprovadas === r.total) return `Todas as ${total} notas podem ser antecipadas.`;
    if (r.aprovadas === 0) return `Nenhuma das ${total} notas pode ser antecipada agora.`;
    return r.aprovadas === 1
      ? `1 de ${total} notas pode ser antecipada.`
      : `${formatarInteiro(r.aprovadas)} de ${total} notas podem ser antecipadas.`;
  });

  protected readonly segmentos = computed<SegmentoComposicao[]>(() => {
    const r = this.resumo();
    if (!r) return [];
    const fora = Math.round((r.valorNominalEnviado - r.valorNominalElegivel) * 100) / 100;
    return [
      { rotulo: 'Você recebe', valor: r.valorLiquido, tom: 'liquido' },
      { rotulo: 'Desconto', valor: r.desconto, tom: 'desconto' },
      { rotulo: 'Notas fora da antecipação', valor: fora, tom: 'recusado' },
    ];
  });

  protected readonly motivosResumo = computed(() =>
    (this.resumo()?.motivos ?? []).map((m) => {
      const e = explicarMotivo({ codigo: m.codigo, mensagem: '' });
      return {
        ...m,
        titulo: e.titulo,
        explicacao: e.explicacao.split('. ')[0]!.replace(/\.?$/, '.'),
        icone: e.icone,
      };
    }),
  );

  protected readonly notaAberta = computed<NotaAnalisada | null>(() => {
    const i = Number(this.nota());
    return this.nota() !== undefined && Number.isInteger(i) ? (this.notas()[i] ?? null) : null;
  });

  /** Posição da nota aberta dentro da lista visível (filtros e ordenação da tabela). */
  protected readonly navegacao = computed(() => {
    const aberta = this.notaAberta();
    if (!aberta) return null;
    const visiveis = this.tabela()?.filtradas() ?? this.notas();
    const lista = visiveis.some((n) => n.indice === aberta.indice) ? visiveis : this.notas();
    const pos = lista.findIndex((n) => n.indice === aberta.indice);
    return { lista, pos };
  });

  private definir(params: Record<string, string | number | null>, substituir = true): void {
    void this.router.navigate([], {
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: substituir,
    });
  }

  protected definirStatus(s: FiltroStatus): void {
    this.definir({ status: s === 'todas' ? null : s });
  }

  protected definirMotivo(codigo: string | null): void {
    this.definir({ motivo: codigo });
  }

  protected verNotasDoMotivo(codigo: string): void {
    this.definir({ motivo: codigo, status: null });
    this.doc.getElementById('notas')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected abrirNota(indice: number): void {
    this.definir({ nota: indice }, this.nota() !== undefined);
  }

  protected fecharNota(): void {
    this.definir({ nota: null });
  }

  protected vizinha(delta: number): void {
    const nav = this.navegacao();
    const alvo = nav?.lista[nav.pos + delta];
    if (alvo) this.definir({ nota: alvo.indice });
  }

  protected exportar(): void {
    const a = this.analise();
    if (!a) return;
    const cabecalho =
      'NU_DOCUMENTO;NOME_SACADO;DOC_SACADO;VALOR_NOMINAL;DATA_VENCIMENTO;STATUS;VALOR_LIQUIDO;DESCONTO;MOTIVOS';
    const linhas = this.notas().map((n) =>
      [
        n.numeroDocumento,
        n.csv?.nomeSacado ?? '',
        n.csv?.docSacado ?? '',
        decimalCsv(n.valorNominal),
        n.csv?.dataVencimento ? formatarData(n.csv.dataVencimento) : '',
        ROTULO_STATUS[n.status],
        decimalCsv(n.valorLiquido),
        decimalCsv(n.desconto),
        n.motivos.map((m) => `${tituloMotivo(m.codigo)}: ${m.mensagem}`).join(' | '),
      ]
        .map(campoCsv)
        .join(';'),
    );
    this.baixar([cabecalho, ...linhas].join('\r\n'), `resultado_${a.arquivoNome.replace(/\.csv$/i, '')}.csv`);
  }

  protected baixarSomenteAprovadas(): void {
    const a = this.analise();
    const d = this.download();
    if (!a || !d) return;
    this.baixar(d.csv, `${a.arquivoNome.replace(/\.csv$/i, '')}_somente_aprovadas.csv`);
    this.downloadDispensado.set(true);
  }

  private baixar(conteudo: string, nome: string): void {
    const blob = new Blob(['\uFEFF' + conteudo], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = this.doc.createElement('a');
    link.href = url;
    link.download = nome;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
