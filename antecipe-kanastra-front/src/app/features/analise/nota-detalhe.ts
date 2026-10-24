import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import {
  LucideChevronLeft,
  LucideChevronRight,
  LucideCircleCheck,
  LucideDynamicIcon,
  LucideX,
} from '@lucide/angular';
import { NotaAnalisada } from '../../models/analise.models';
import { explicarMotivo } from '../../shared/motivos/motivos-catalogo';
import { Money } from '../../shared/ui/money';
import { BrlPipe, CnpjPipe, DataBrPipe, PctPipe } from '../../shared/ui/pipes';
import { StatusBadge } from '../../shared/ui/status-badge';

@Component({
  selector: 'app-nota-detalhe',
  imports: [
    Money,
    StatusBadge,
    BrlPipe,
    CnpjPipe,
    DataBrPipe,
    PctPipe,
    LucideX,
    LucideChevronLeft,
    LucideChevronRight,
    LucideCircleCheck,
    LucideDynamicIcon,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './nota-detalhe.html',
  styleUrl: './nota-detalhe.scss',
  host: {
    '(keydown.alt.arrowleft)': 'anterior.emit()',
    '(keydown.alt.arrowright)': 'proxima.emit()',
  },
})
export class NotaDetalhe {
  readonly nota = input.required<NotaAnalisada>();
  readonly posicao = input.required<number>();
  readonly total = input.required<number>();

  readonly fechar = output<void>();
  readonly anterior = output<void>();
  readonly proxima = output<void>();

  protected readonly motivos = computed(() => {
    const n = this.nota();
    return n.motivos.map((m) => explicarMotivo(m, n, n.status === 'PENDENTE_VALIDACAO'));
  });

  protected readonly descontoPercentual = computed(() => {
    const n = this.nota();
    return n.desconto !== null && n.valorNominal > 0 ? n.desconto / n.valorNominal : null;
  });
}
