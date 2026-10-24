import { A11yModule } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  output,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';

/**
 * Painel lateral modal. Prende o foco, fecha com Esc ou clique fora e devolve o foco ao
 * elemento que o abriu. Renderize com @if no componente pai.
 */
@Component({
  selector: 'app-side-panel',
  imports: [A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'fechar.emit()' },
  template: `
    <div
      class="fundo"
      (click)="fechar.emit()"
      animate.enter="fundo-in"
      animate.leave="fundo-out"
    ></div>
    <aside
      class="painel"
      role="dialog"
      aria-modal="true"
      [attr.aria-labelledby]="rotuladoPor()"
      cdkTrapFocus
      [cdkTrapFocusAutoCapture]="true"
      animate.enter="painel-in"
      animate.leave="painel-out"
    >
      <ng-content />
    </aside>
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      z-index: 50;
      display: flex;
      justify-content: flex-end;
    }
    .fundo {
      position: absolute;
      inset: 0;
      background: rgba(1, 26, 20, 0.42);
    }
    .painel {
      position: relative;
      display: flex;
      flex-direction: column;
      width: min(560px, 100vw);
      height: 100%;
      background: var(--c-surface);
      box-shadow: var(--sh-panel);
      overflow: hidden;
      outline: none;
    }
    .fundo-in {
      animation: fade-in var(--dur) var(--ease-out) both;
    }
    .fundo-out {
      animation: fade-in var(--dur-fast) var(--ease-in-out) reverse both;
    }
    .painel-in {
      animation: entrar var(--dur-slow) var(--ease-out) both;
    }
    .painel-out {
      animation: entrar var(--dur) var(--ease-in-out) reverse both;
    }
    @keyframes entrar {
      from {
        transform: translateX(48px);
        opacity: 0;
      }
    }
  `,
})
export class SidePanel {
  readonly rotuladoPor = input.required<string>();
  readonly fechar = output<void>();

  constructor() {
    const doc = inject(DOCUMENT);
    const overflowAnterior = doc.body.style.overflow;
    doc.body.style.overflow = 'hidden';
    inject(DestroyRef).onDestroy(() => (doc.body.style.overflow = overflowAnterior));
  }
}
