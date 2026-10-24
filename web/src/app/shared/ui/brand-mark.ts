import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Marca do Antecipe: três barras ascendentes — o recebível do futuro trazido para hoje. */
@Component({
  selector: 'app-brand-mark',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden="true">
      <rect x="0.5" y="0.5" width="27" height="27" rx="3" fill="var(--c-forest-900)" />
      <rect x="7" y="15" width="3" height="6" rx="0.5" fill="var(--c-mint-300)" />
      <rect
        x="12.5"
        y="11"
        width="3"
        height="10"
        rx="0.5"
        fill="var(--c-mint-300)"
        opacity="0.85"
      />
      <rect x="18" y="7" width="3" height="14" rx="0.5" fill="var(--c-cream)" />
    </svg>
    <span class="nome">antecipe</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      color: var(--c-forest-900);
    }
    .nome {
      font-family: var(--f-display);
      font-stretch: 125%;
      font-size: 1.125rem;
      font-weight: 500;
      letter-spacing: -0.02em;
    }
  `,
})
export class BrandMark {}
