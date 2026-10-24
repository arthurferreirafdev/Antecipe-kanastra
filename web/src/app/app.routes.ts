import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Visão geral · Antecipe',
    loadComponent: () =>
      import('./features/visao-geral/visao-geral.page').then((m) => m.VisaoGeralPage),
  },
  {
    path: 'nova-antecipacao',
    title: 'Nova antecipação · Antecipe',
    loadComponent: () =>
      import('./features/nova-antecipacao/nova-antecipacao.page').then(
        (m) => m.NovaAntecipacaoPage,
      ),
  },
  {
    path: 'analise',
    title: 'Resultado da análise · Antecipe',
    loadComponent: () => import('./features/analise/analise.page').then((m) => m.AnalisePage),
  },
  { path: '**', redirectTo: '' },
];
