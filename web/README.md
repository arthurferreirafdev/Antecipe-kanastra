# Antecipe — frontend

Portal de antecipação de recebíveis: a empresa envia um CSV com suas notas e vê **quais podem ser antecipadas, quanto vai receber e por que algumas foram recusadas**.

Angular 22 (standalone, signals, zoneless) · TypeScript strict · SCSS com design system próprio · Lucide · Angular CDK.

## Como rodar

Instruções completas (Docker, API local, variáveis de ambiente) no [README da raiz](../README.md#como-executar). Resumo para desenvolver só o front:

Pré-requisitos: **Node 24.15+** (ou 22.22.3+; o Angular CLI recusa versões anteriores) e a API rodando em `http://localhost:5000`:

```bash
# na raiz do repositório
dotnet run --project api/Antecipe.Api --urls http://localhost:5000

# nesta pasta
npm ci
npm start            # http://localhost:4200
```

A API não habilita CORS. O `ng serve` encaminha `/api` para `http://localhost:5000` (`proxy.conf.mjs`). Para usar outra porta:

```bash
# bash
ANTECIPE_API_URL=http://localhost:5080 npm start
# PowerShell
$env:ANTECIPE_API_URL="http://localhost:5080"; npm start
```

No Docker, o nginx (`nginx.conf`) serve o build e repassa `/api` para o serviço `api`.

### Experimentar

Em **Nova antecipação**, clique em *Baixar CSV de exemplo* (`public/modelo/notas_exemplo.csv`) e envie o arquivo. Ele cobre todas as regras ativas do fundo: 12 notas aprovadas (sacados diferentes, para respeitar a concentração máxima de 10%) e 7 recusadas, cada uma por um motivo diferente.

## Scripts

| Comando | O que faz |
|---|---|
| `npm start` | Servidor de desenvolvimento com proxy para a API |
| `npm run build` | Build de produção em `dist/` |
| `npm test` / `npm run test:ci` | Testes unitários e de componente (Vitest) |
| `npm run e2e` | Fluxo completo no navegador contra a API real (exige API e `npm start` no ar) |

## Estrutura

```
src/app
├── core/
│   ├── api/        API_BASE_URL, NotasApiService, interceptor e tradução de erros
│   └── state/      AnaliseStore (signals): envio, resultado, rascunho, persistência
├── models/         Contratos da API e do CSV
├── shared/
│   ├── csv/        Leitura e validação de formato do CSV (espelha o parser do backend)
│   ├── analise/    Combinação resultado + CSV e agregações
│   ├── motivos/    Tradução dos códigos de recusa para linguagem simples
│   ├── format/     Formatação pt-BR (moeda, datas, CNPJ)
│   └── ui/         Money, StatusBadge, ComposicaoBar, SidePanel, Skeleton, pipes
└── features/
    ├── visao-geral/        Visão geral
    ├── nova-antecipacao/   Upload do CSV
    └── analise/            Resultado, tabela de notas e painel de explicação
```

Documentação complementar: [API_INTEGRATION.md](API_INTEGRATION.md) · [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)

## Funcionalidades implementadas

- **Visão geral**: saudação, valor disponível, notas elegíveis/recusadas, elegibilidade e principais motivos da última análise, além de um estado vazio com chamada para nova antecipação.
- **Upload**: arrastar e soltar ou seleção manual, formato aceito e layout de exemplo, download do CSV modelo, leitura e pré-visualização no navegador (com totais de conferência), validação de formato com a linha exata e a correção sugerida, aviso de arquivo fora de UTF-8 e opção "enviar mesmo assim".
- **Envio real** para `POST /api/notas/analisar`, com estado de carregamento, bloqueio de duplo envio, timeout e "tentar novamente".
- **Erros humanos** para rede, 400, 413 e 500, mantendo os detalhes técnicos recolhidos.
- **Resultado**: título-resposta ("12 de 19 notas podem ser antecipadas"), valor líquido em destaque, composição *valor de face − desconto = líquido*, barra de composição do total enviado, aprovadas/recusadas/elegibilidade e motivos agrupados com quantidade e valor.
- **Tabela**: busca (documento, sacado, CNPJ), filtro por status e por motivo (sincronizados com a URL), ordenação por coluna, paginação, cartões no celular, campo expansível com todos os motivos de cada recusada e exportação do resultado em CSV.
- **Download só das aprovadas**: quando há recusadas ou duplicadas, a tela de resultado pergunta se o usuário quer baixar o CSV só com as aprovadas e mostra a lista de duplicadas.
- **Painel de explicação**: todos os motivos da nota, com título simples, explicação, valor identificado × limite (quando a API informa), o que fazer e a mensagem técnica original. Notas aprovadas mostram valor de face, desconto e líquido. Tem navegação anterior/próxima e Esc para fechar.
- **Persistência**: a última análise fica salva no navegador, e o arquivo selecionado sobrevive à navegação.
- Responsivo (desktop, tablet, celular), navegável por teclado, `prefers-reduced-motion`, moeda e datas em pt-BR.

## Qualidade

- `npm run test:ci`: 55 testes (parser CSV, agregações, tradução de motivos, erros 4xx/5xx, service HTTP com interceptor, store com duplo envio, estados críticos das páginas de upload e resultado).
- `npm run build`: build de produção sem avisos.
- `npm run e2e`: upload → resultado → filtro → painel de recusa → nota aprovada → visão geral → celular, contra a API real, sem erros de console e sem rolagem horizontal em 390px.

## Limitações reais

- A API só **simula** a antecipação. Não há confirmação nem histórico persistido (ver [API_INTEGRATION.md](API_INTEGRATION.md#o-que-falta-no-backend-não-implementado)).
- A taxa de desconto não é exposta pela API. O front mostra o desconto efetivo (desconto ÷ valor de face) e explica o método, sem citar a taxa.
- Para notas aprovadas, não é possível listar cada regra verificada: a API só informa as regras não atendidas.
- A "última análise" fica no `localStorage` deste navegador. Resultados muito grandes (por exemplo, 100 mil notas) não cabem e se perdem ao recarregar a página.
- O front não aplica nenhuma regra de elegibilidade. A validação local é só de formato e espelha o parser da API.
