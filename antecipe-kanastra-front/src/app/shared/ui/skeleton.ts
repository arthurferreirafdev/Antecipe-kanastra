import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Bloco de carregamento. Use vários para desenhar a silhueta do conteúdo. */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
    '[style.width]': 'largura()',
    '[style.height]': 'altura()',
  },
  template: '',
  styles: `
    :host {
      display: block;
      border-radius: var(--r-xs);
      background: linear-gradient(
          90deg,
          var(--c-paper-deep) 0%,
          #f3ede1 40%,
          var(--c-paper-deep) 80%
        )
        0 0 / 200% 100%;
      animation: shimmer 1.4s linear infinite;
    }
  `,
})
export class Skeleton {
  readonly largura = input('100%');
  readonly altura = input('14px');
}
