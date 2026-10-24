import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { formatarBrl, formatarPercentual } from '../format/formatters';

export interface SegmentoComposicao {
  rotulo: string;
  valor: number;
  tom: 'liquido' | 'desconto' | 'recusado' | 'pendente';
}

/** Barra fina que mostra como um total se divide (ex.: líquido · desconto · recusado). */
@Component({
  selector: 'app-composicao-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.inverso]': 'inverso()' },
  template: `
    <div class="barra" role="img" [attr.aria-label]="descricao()">
      @for (s of visiveis(); track s.rotulo) {
        <span
          class="barra__seg"
          [class]="'barra__seg tom-' + s.tom"
          [style.flex-grow]="s.valor"
        ></span>
      }
    </div>
    <ul class="legenda">
      @for (s of segmentos(); track s.rotulo) {
        <li>
          <span
            class="legenda__marca"
            [class]="'legenda__marca tom-' + s.tom"
            aria-hidden="true"
          ></span>
          <span class="legenda__rotulo">{{ s.rotulo }}</span>
          <span class="legenda__valor">{{ brl(s.valor) }}</span>
        </li>
      }
    </ul>
  `,
  styles: `
    :host {
      display: block;
      --seg-liquido: var(--c-emerald-600);
      --seg-desconto: var(--c-mint-300);
      --seg-recusado: var(--c-line-strong);
      --seg-pendente: #d9b56a;
      --legenda-cor: var(--c-ink-2);
    }
    :host(.inverso) {
      --seg-liquido: var(--c-mint-300);
      --seg-desconto: #3f8f6c;
      --seg-recusado: rgba(248, 241, 224, 0.22);
      --legenda-cor: var(--c-ink-inverse-2);
    }
    .barra {
      display: flex;
      gap: 3px;
      height: 10px;
    }
    .barra__seg {
      flex-basis: 0;
      min-width: 3px;
      border-radius: 1px;
      transform-origin: left;
      animation: crescer var(--dur-slow) var(--ease-out) both;
    }
    .barra__seg:nth-child(2) {
      animation-delay: 80ms;
    }
    .barra__seg:nth-child(3) {
      animation-delay: 160ms;
    }
    @keyframes crescer {
      from {
        transform: scaleX(0);
      }
    }
    .tom-liquido {
      background: var(--seg-liquido);
    }
    .tom-desconto {
      background: var(--seg-desconto);
    }
    .tom-recusado {
      background: var(--seg-recusado);
    }
    .tom-pendente {
      background: var(--seg-pendente);
    }
    .legenda {
      display: flex;
      flex-wrap: wrap;
      gap: var(--s-2) var(--s-5);
      margin: var(--s-3) 0 0;
      padding: 0;
      list-style: none;
      color: var(--legenda-cor);
      font-size: var(--t-sm);
    }
    .legenda li {
      display: inline-flex;
      align-items: center;
      gap: var(--s-2);
    }
    .legenda__marca {
      width: 10px;
      height: 10px;
      border-radius: 1px;
    }
    .legenda__valor {
      font-variant-numeric: tabular-nums;
      font-weight: 600;
    }
  `,
})
export class ComposicaoBar {
  readonly segmentos = input.required<SegmentoComposicao[]>();
  readonly inverso = input(false);

  protected readonly visiveis = computed(() => this.segmentos().filter((s) => s.valor > 0));
  protected readonly descricao = computed(() => {
    const total = this.segmentos().reduce((t, s) => t + s.valor, 0);
    return this.segmentos()
      .map(
        (s) =>
          `${s.rotulo}: ${formatarBrl(s.valor)} (${formatarPercentual(total > 0 ? s.valor / total : 0)})`,
      )
      .join('; ');
  });
  protected readonly brl = formatarBrl;
}
