import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowRight, LucideArrowUpRight, LucideDynamicIcon } from '@lucide/angular';
import { AnaliseStore } from '../../core/state/analise.store';
import { formatarDataHora, formatarDiaPorExtenso, saudacao } from '../../shared/format/formatters';
import { iconeMotivo, tituloMotivo } from '../../shared/motivos/motivos-catalogo';
import { ComposicaoBar, SegmentoComposicao } from '../../shared/ui/composicao-bar';
import { Money } from '../../shared/ui/money';
import { BrlPipe, InteiroPipe, PctPipe } from '../../shared/ui/pipes';

@Component({
  selector: 'app-visao-geral',
  imports: [
    RouterLink,
    Money,
    ComposicaoBar,
    BrlPipe,
    PctPipe,
    InteiroPipe,
    LucideArrowRight,
    LucideArrowUpRight,
    LucideDynamicIcon,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './visao-geral.page.html',
  styleUrl: './visao-geral.page.scss',
})
export class VisaoGeralPage {
  private readonly store = inject(AnaliseStore);

  protected readonly saudacao = saudacao();
  protected readonly dia = formatarDiaPorExtenso(new Date());
  protected readonly analise = this.store.analise;
  protected readonly resumo = this.store.resumo;

  protected readonly quando = computed(() => {
    const a = this.analise();
    return a ? formatarDataHora(a.analisadaEm) : '';
  });

  protected readonly segmentos = computed<SegmentoComposicao[]>(() => {
    const r = this.resumo();
    if (!r) return [];
    return [
      { rotulo: 'Você recebe', valor: r.valorLiquido, tom: 'liquido' },
      { rotulo: 'Desconto', valor: r.desconto, tom: 'desconto' },
      {
        rotulo: 'Fora da antecipação',
        valor: r.valorNominalEnviado - r.valorNominalElegivel,
        tom: 'recusado',
      },
    ];
  });

  protected readonly principaisMotivos = computed(() =>
    (this.resumo()?.motivos ?? [])
      .slice(0, 4)
      .map((m) => ({ ...m, titulo: tituloMotivo(m.codigo), icone: iconeMotivo(m.codigo) })),
  );
}
