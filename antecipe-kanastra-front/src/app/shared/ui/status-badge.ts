import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { StatusRecebivel } from '../../models/api.models';

const ROTULOS: Record<StatusRecebivel, string> = {
  APROVADA: 'Aprovada',
  RECUSADA: 'Recusada',
  PENDENTE_VALIDACAO: 'Pendente',
};

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "'badge badge--' + status()" },
  template: `<span class="badge__dot" aria-hidden="true"></span>{{ rotulo() }}`,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      height: 24px;
      padding: 0 9px 0 8px;
      border-radius: var(--r-xs);
      font-size: var(--t-xs);
      font-weight: 600;
      letter-spacing: 0.01em;
      white-space: nowrap;
    }
    .badge__dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: currentColor;
    }
    :host(.badge--APROVADA) {
      background: var(--c-approved-bg);
      color: var(--c-approved);
    }
    :host(.badge--RECUSADA) {
      background: var(--c-refused-bg);
      color: var(--c-refused);
    }
    :host(.badge--PENDENTE_VALIDACAO) {
      background: var(--c-pending-bg);
      color: var(--c-pending);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<StatusRecebivel>();
  protected readonly rotulo = computed(() => ROTULOS[this.status()]);
}
