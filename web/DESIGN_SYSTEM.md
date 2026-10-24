# Design system do Antecipe

Direção inspirada na identidade da Kanastra: papel cru, verde floresta profundo, tipografia display estendida e raios discretos. O objetivo não é copiar o site, e sim criar um produto financeiro calmo e editorial, que responda a três perguntas: **quais notas posso antecipar, quanto vou receber e por que algumas foram recusadas**.

Tokens em `src/styles/_tokens.scss`, mixins em `src/styles/_mixins.scss`, primitivas globais em `src/styles.scss`.

## Princípios

1. **O número é o protagonista.** O valor a receber aparece grande, em fonte display leve, com símbolo e centavos menores.
2. **Editorial, não dashboard.** Não há sidebar nem grade de cards iguais. A hierarquia vem de tipografia, réguas finas (1px) e composição assimétrica (7/5, 5/7).
3. **Recusa sem alarme.** Estados usam tons terrosos (argila, ocre), não vermelho saturado. Toda recusa vem com explicação, números e o que fazer.
4. **Honestidade.** O que não vem da API não aparece. Os pontos em que a experiência depende de dados locais estão sinalizados.

## Cor

| Token | Valor | Uso |
|---|---|---|
| `--c-paper` | `#f4efe4` | Fundo da página |
| `--c-paper-deep` | `#ebe4d5` | Trilhos, chips, skeleton |
| `--c-surface` | `#fbf9f4` | Superfícies (zona de upload, painel) |
| `--c-line` / `--c-line-strong` | `#dcd5c5` / `#c3baa6` | Réguas e bordas |
| `--c-ink` · `-2` · `-3` | `#0f1d18` · `#384540` · `#5f6a64` | Texto principal, secundário, apoio |
| `--c-forest-900` | `#022c22` | Marca, painéis de destaque, botão primário |
| `--c-forest-800` | `#064e3b` | Links, hover do primário |
| `--c-emerald-600` | `#047857` | Aprovado, foco, progresso |
| `--c-mint-300` | `#8fdcb8` | Acentos sobre o verde (rótulos, valor líquido) |
| `--c-cream` | `#f8f1e0` | Texto e botão sobre o verde |
| `--c-refused` / `-bg` | `#9a3f1d` / `#f6e4d9` | Recusa |
| `--c-pending` / `-bg` | `#85580f` / `#f4e7c6` | Pendente, avisos |

Contraste: todos os pares texto/fundo usados atendem WCAG AA (`--c-ink-3` sobre `--c-paper` ≈ 5:1).

## Tipografia

Fontes self-hosted via `@fontsource-variable`, sem dependência de CDN.

| Papel | Família | Notas |
|---|---|---|
| Display | **Archivo** variável, `font-stretch: 125%`, peso 300 | Títulos e números. Substitui a Vinila Extended da Kanastra |
| Texto | **Inter** variável | UI e parágrafos |
| Dados | **JetBrains Mono** | Números de documento, CNPJ, códigos técnicos |

Escala: `--t-2xs` 11 · `xs` 12 · `sm` 13 · `base` 15 · `md` 17 · `lg` 22 · `xl` 30 · `2xl`/`3xl`/`hero` fluidas com `clamp()` (até 100px). Números usam `tabular-nums`.

## Espaço, raio e elevação

- Espaçamento base 4: `--s-1` 4 … `--s-9` 96.
- Raios discretos: `--r-xs` 2px (badges), `--r-sm` 4px (botões, campos), `--r-md` 6px (painéis).
- Elevação mínima: apenas `--sh-panel` no painel lateral. Fora isso, a separação vem de réguas e cor de fundo.

## Movimento

- `--ease-out: cubic-bezier(.2,.7,.2,1)`. Durações: 140ms (hover), 220ms (estado), 480ms (entrada).
- Entradas escalonadas (`.rise`, `.rise-1..4`), barras que crescem da esquerda, número que sobe até o valor final (`app-money [animar]`) e painel lateral que desliza.
- `prefers-reduced-motion` desliga todas as animações.

## Componentes

| Componente | Arquivo | Descrição |
|---|---|---|
| `app-money` | `shared/ui/money.ts` | Valor em R$ editorial; tamanhos `hero/xl/lg/md/sm`; `aria-label` com o valor completo |
| `app-status-badge` | `shared/ui/status-badge.ts` | Aprovada / Recusada / Pendente |
| `app-composicao-bar` | `shared/ui/composicao-bar.ts` | Barra fina de composição (líquido · desconto · fora) com legenda; variante inversa |
| `app-side-panel` | `shared/ui/side-panel.ts` | Painel modal lateral: foco preso (CDK), Esc, clique fora, devolve o foco |
| `app-skeleton` | `shared/ui/skeleton.ts` | Bloco de carregamento com shimmer |
| `app-brand-mark` | `shared/ui/brand-mark.ts` | Marca: três barras ascendentes |
| `.btn` (`--primary`, `--inverse`, `--ghost`, `--lg`, `--sm`) | `styles.scss` | Botões; a seta avança 3px no hover |
| `.eyebrow`, `.display`, `.mono`, `.link` | `styles.scss` | Utilitários tipográficos |

## Acessibilidade

- Link "Pular para o conteúdo", foco visível em verde em todos os controles.
- Tabela com `aria-sort`, `caption`, botões reais para ordenar e abrir detalhes.
- Painel com `role="dialog"`, `aria-modal`, `aria-labelledby` e foco preso.
- Mensagens de estado em `aria-live` / `role="status"` / `role="alert"`.
- Em telas estreitas a tabela vira lista de cartões, sem rolagem horizontal (verificado em 390px).
