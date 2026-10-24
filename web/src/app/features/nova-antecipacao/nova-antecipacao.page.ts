import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCircleAlert,
  LucideDownload,
  LucideDynamicIcon,
  LucideFileSpreadsheet,
  LucideInfo,
  LucideLoaderCircle,
  LucideRotateCcw,
  LucideUpload,
  LucideWifiOff,
  LucideX,
} from '@lucide/angular';
import { AnaliseStore } from '../../core/state/analise.store';
import { COLUNAS_CSV } from '../../models/csv.models';
import { decodificar, lerCsvRecebiveis, verificarArquivo } from '../../shared/csv/csv-recebiveis';
import { formatarTamanho } from '../../shared/format/formatters';
import { explicarMotivo } from '../../shared/motivos/motivos-catalogo';
import { BrlPipe, CnpjPipe, DataBrPipe, InteiroPipe } from '../../shared/ui/pipes';
import { Skeleton } from '../../shared/ui/skeleton';
import { ProblemaCsv } from '../../models/csv.models';

const LINHAS_PREVIA = 6;

@Component({
  selector: 'app-nova-antecipacao',
  imports: [
    RouterLink,
    Skeleton,
    BrlPipe,
    CnpjPipe,
    DataBrPipe,
    InteiroPipe,
    LucideUpload,
    LucideFileSpreadsheet,
    LucideDownload,
    LucideX,
    LucideArrowRight,
    LucideLoaderCircle,
    LucideCircleAlert,
    LucideInfo,
    LucideRotateCcw,
    LucideDynamicIcon,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './nova-antecipacao.page.html',
  styleUrl: './nova-antecipacao.page.scss',
})
export class NovaAntecipacaoPage {
  private readonly store = inject(AnaliseStore);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  private readonly seletor = viewChild.required<ElementRef<HTMLInputElement>>('seletor');
  private readonly painelErro = viewChild<ElementRef<HTMLElement>>('painelErro');

  protected readonly lucideWifiOff = LucideWifiOff;
  protected readonly lucideCircleAlert = LucideCircleAlert;
  protected readonly colunas = COLUNAS_CSV;
  protected readonly exemplo =
    'ANTECIPE FIDC;92.234.400/0001-80;Atelier Ipê Móveis;11.111.111/0001-91;Mercado Serra Azul;58.206.002/0001-11;NF-1001;Duplicata;1250,90;15/01/2027;24/10/2026';

  protected readonly rascunho = this.store.rascunho;
  protected readonly envio = this.store.envio;
  protected readonly enviando = this.store.enviando;
  protected readonly erroApi = this.store.erro;
  protected readonly temAnalise = computed(() => this.store.analise() !== null);

  protected readonly lendo = signal(false);
  protected readonly arrastando = signal(false);
  protected readonly problemaArquivo = signal<ProblemaCsv | null>(null);
  protected readonly ignorarErros = signal(false);

  protected readonly erros = computed(
    () => this.rascunho()?.leitura.problemas.filter((p) => p.severidade === 'erro') ?? [],
  );
  protected readonly linhas = computed(() => this.rascunho()?.leitura.linhas ?? []);
  protected readonly previa = computed(() => this.linhas().slice(0, LINHAS_PREVIA));
  protected readonly tamanho = computed(() => {
    const r = this.rascunho();
    return r ? formatarTamanho(r.arquivo.size) : '';
  });

  /** Totais do próprio arquivo, só para conferência antes do envio. */
  protected readonly conferencia = computed(() => {
    const linhas = this.linhas();
    let centavos = 0;
    const vencimentos: string[] = [];
    const sacados = new Set<string>();
    for (const l of linhas) {
      if (l.valorNominal !== null) centavos += Math.round(l.valorNominal * 100);
      if (l.dataVencimento) vencimentos.push(l.dataVencimento);
      sacados.add(l.docSacado.replace(/\D/g, '') || l.nomeSacado);
    }
    vencimentos.sort();
    return {
      quantidade: linhas.length,
      valorFace: centavos / 100,
      primeiroVencimento: vencimentos[0] ?? null,
      ultimoVencimento: vencimentos.at(-1) ?? null,
      sacados: sacados.size,
    };
  });

  protected readonly podeEnviar = computed(() => {
    const r = this.rascunho();
    return !!r && !this.enviando() && (r.leitura.valido || this.ignorarErros());
  });

  protected readonly pendencias = computed(() =>
    (this.erroApi()?.pendencias ?? []).map((p) => ({
      numeroDocumento: p.numeroDocumento,
      motivos: p.motivos.map((m) => explicarMotivo(m, null, true)),
    })),
  );

  protected abrirSeletor(): void {
    this.seletor().nativeElement.click();
  }

  protected aoEscolher(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    void this.selecionar(input.files?.[0]);
    input.value = '';
  }

  protected aoArrastar(evento: DragEvent): void {
    evento.preventDefault();
    if (evento.dataTransfer) evento.dataTransfer.dropEffect = this.enviando() ? 'none' : 'copy';
    this.arrastando.set(true);
  }

  protected aoSair(evento: DragEvent): void {
    const destino = evento.relatedTarget as Node | null;
    if (!destino || !(evento.currentTarget as HTMLElement).contains(destino))
      this.arrastando.set(false);
  }

  protected aoSoltar(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastando.set(false);
    if (this.enviando()) return;
    void this.selecionar(evento.dataTransfer?.files?.[0]);
  }

  protected remover(): void {
    this.rascunho.set(null);
    this.problemaArquivo.set(null);
    this.ignorarErros.set(false);
    this.store.limparErro();
  }

  async selecionar(arquivo: File | undefined): Promise<void> {
    if (!arquivo || this.enviando()) return;
    this.store.limparErro();
    this.ignorarErros.set(false);
    const problema = verificarArquivo(arquivo);
    if (problema) {
      this.rascunho.set(null);
      this.problemaArquivo.set(problema);
      return;
    }
    this.problemaArquivo.set(null);
    this.lendo.set(true);
    try {
      const { texto, aviso } = decodificar(await arquivo.arrayBuffer());
      this.rascunho.set({ arquivo, leitura: lerCsvRecebiveis(texto), avisoCodificacao: aviso });
    } catch {
      this.rascunho.set(null);
      this.problemaArquivo.set({
        severidade: 'erro',
        mensagem: 'Não foi possível ler o arquivo. Ele pode estar aberto em outro programa.',
      });
    } finally {
      this.lendo.set(false);
    }
  }

  protected async analisar(): Promise<void> {
    const r = this.rascunho();
    if (!r || !this.podeEnviar()) return;
    const ok = await this.store.analisar(
      r.arquivo,
      r.leitura.linhas.length ? r.leitura.linhas : null,
    );
    if (ok) {
      await this.router.navigate(['/analise']);
    } else {
      afterNextRender(() => this.painelErro()?.nativeElement.focus(), { injector: this.injector });
    }
  }
}
