import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AnaliseStore } from './core/state/analise.store';
import { BrandMark } from './shared/ui/brand-mark';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, BrandMark],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './app.scss',
  template: `
    <a class="pular" href="#conteudo">Pular para o conteúdo</a>

    <header class="topo">
      <div class="page topo__linha">
        <a routerLink="/" class="topo__marca" aria-label="Antecipe — visão geral"
          ><app-brand-mark
        /></a>

        <nav class="topo__nav" aria-label="Principal">
          <a
            routerLink="/"
            routerLinkActive="ativo"
            [routerLinkActiveOptions]="{ exact: true }"
            ariaCurrentWhenActive="page"
            >Visão geral</a
          >
          <a routerLink="/nova-antecipacao" routerLinkActive="ativo" ariaCurrentWhenActive="page"
            >Nova antecipação</a
          >
          @if (store.analise()) {
            <a routerLink="/analise" routerLinkActive="ativo" ariaCurrentWhenActive="page"
              >Análise</a
            >
          }
        </nav>

        @if (store.nomeFundo(); as fundo) {
          <p class="topo__fundo"><span class="eyebrow">Fundo</span> {{ fundo }}</p>
        }
      </div>
    </header>

    <main id="conteudo" tabindex="-1">
      <router-outlet />
    </main>

    <footer class="rodape">
      <div class="page rodape__linha">
        <span>Antecipe — antecipação de recebíveis</span>
        <span class="muted"
          >Valores calculados pelo motor de regras do fundo. A análise é uma simulação e não cede
          notas.</span
        >
      </div>
    </footer>
  `,
})
export class App {
  protected readonly store = inject(AnaliseStore);
}
