import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { formatarBrl, partesBrl } from '../format/formatters';

/**
 * Valor em reais com tipografia editorial (símbolo e centavos menores que a parte inteira).
 * Com `animar`, o número sobe até o valor final — apenas efeito visual, o valor exibido ao
 * final é exatamente o recebido.
 */
@Component({
  selector: 'app-money',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'money money--' + tamanho()",
    '[attr.aria-label]': 'rotulo()',
    role: 'text',
  },
  template: `
    <span class="money__sinal" aria-hidden="true">{{ partes().sinal }}</span>
    <span class="money__simbolo" aria-hidden="true">{{ partes().simbolo }}</span>
    <span class="money__inteiro" aria-hidden="true">{{ partes().inteiro }}</span>
    <span class="money__centavos" aria-hidden="true">{{ partes().centavos }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: baseline;
      font-family: var(--f-display);
      font-stretch: 125%;
      font-variant-numeric: tabular-nums lining-nums;
      letter-spacing: -0.03em;
      line-height: 0.95;
      white-space: nowrap;
    }
    .money__simbolo {
      font-size: 0.36em;
      font-weight: 450;
      letter-spacing: 0;
      margin-right: 0.28em;
      align-self: flex-start;
      margin-top: 0.22em;
      opacity: 0.75;
    }
    .money__sinal,
    .money__inteiro {
      font-weight: 300;
    }
    .money__centavos {
      font-size: 0.42em;
      font-weight: 400;
      letter-spacing: -0.01em;
      opacity: 0.75;
    }
    :host(.money--hero) {
      font-size: var(--t-hero);
    }
    :host(.money--xl) {
      font-size: var(--t-3xl);
    }
    :host(.money--lg) {
      font-size: var(--t-2xl);
    }
    :host(.money--md) {
      font-size: var(--t-xl);
    }
    :host(.money--sm) {
      font-size: var(--t-lg);
    }
  `,
})
export class Money {
  readonly valor = input.required<number>();
  readonly tamanho = input<'hero' | 'xl' | 'lg' | 'md' | 'sm'>('lg');
  readonly animar = input(false);

  private readonly exibido = signal(0);
  protected readonly partes = computed(() => partesBrl(this.exibido()));
  protected readonly rotulo = computed(() => formatarBrl(this.valor()));

  constructor() {
    let quadro = 0;
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(quadro));
    const reduzido =
      typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

    effect(() => {
      const alvo = this.valor();
      const animar = this.animar();
      cancelAnimationFrame(quadro);
      if (!animar || reduzido || typeof requestAnimationFrame !== 'function') {
        this.exibido.set(alvo);
        return;
      }
      const inicio = untracked(this.exibido);
      const t0 = performance.now();
      const duracao = 900;
      const passo = (t: number) => {
        const p = Math.min(1, (t - t0) / duracao);
        const e = 1 - Math.pow(1 - p, 4);
        this.exibido.set(p === 1 ? alvo : Math.round((inicio + (alvo - inicio) * e) * 100) / 100);
        if (p < 1) quadro = requestAnimationFrame(passo);
      };
      quadro = requestAnimationFrame(passo);
    });
  }
}
