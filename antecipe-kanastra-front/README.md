# Antecipe — frontend

Portal de antecipação de recebíveis: a empresa envia um CSV com suas notas e vê **quais podem ser antecipadas, quanto vai receber e por que algumas foram recusadas**.

Angular 22 (standalone, signals, zoneless) · TypeScript strict · SCSS com design system próprio · Lucide · Angular CDK.

## Como rodar

### Com Docker (front + back de uma vez)

Na raiz do repositório (pasta acima desta):

```bash
docker compose up --build
# Front:  http://localhost:4200
# API:    http://localhost:8080/swagger
```

O `compose.yaml` builda dois serviços: `api` (.NET 10, porta 8080) e `web` (build Angular servido por nginx, que repassa `/api` ao serviço `api` — por isso não há problema de CORS). Para trocar as portas: `WEB_PORT=3000 API_PORT=9090 docker compose up --build`. O build baixa pacotes do npm e do NuGet; em redes que bloqueiam esses registros, rode o primeiro build em outra rede.

### Sem Docker

Pré-requisitos: Node 20+ (testado com 24), .NET SDK 10, Microsoft Edge ou Chrome (para o E2E).

### 1. Backend

```bash
cd ../antecipe-kanastra-back
dotnet run --project antecipe-kanastra-back/antecipe-kanastra-back.csproj --urls http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 2. Frontend

```bash
npm install
npm start            # http://localhost:4200
```

O backend não habilita CORS. O `ng serve` encaminha `/api` para `http://localhost:5000` (`proxy.conf.mjs`). Para usar outra porta:

```bash
# bash
ANTECIPE_API_URL=http://localhost:5080 npm start
# PowerShell
$env:ANTECIPE_API_URL="http://localhost:5080"; npm start
```

Em produção, configure `apiBaseUrl` em `src/environments/environment.ts` e sirva front e API na mesma origem (reverse proxy). Se forem origens diferentes, o backend precisará habilitar CORS.

### 3. Experimentar

Em **Nova antecipação**, clique em *Baixar CSV de exemplo* (`public/modelo/notas_exemplo.csv`) e envie o arquivo. Ele cobre todas as regras ativas do fundo: 1 nota aprovada e 8 recusadas, cada uma por um motivo diferente.

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
- **Erros humanos** para rede, 400, 413, 500 e 503 (este com a lista de notas pendentes), mantendo os detalhes técnicos recolhidos.
- **Resultado**: título-resposta ("1 de 9 notas pode ser antecipada"), valor líquido em destaque, composição *valor de face − desconto = líquido*, barra de composição do total enviado, aprovadas/recusadas/elegibilidade e motivos agrupados com quantidade e valor.
- **Tabela**: busca (documento, sacado, CNPJ), filtro por status e por motivo (sincronizados com a URL), ordenação por coluna, paginação, cartões no celular e exportação do resultado em CSV.
- **Painel de explicação**: todos os motivos da nota, com título simples, explicação, valor identificado × limite (quando a API informa), o que fazer e a mensagem técnica original. Notas aprovadas mostram valor de face, desconto e líquido. Tem navegação anterior/próxima e Esc para fechar.
- **Persistência**: a última análise fica salva no navegador, e o arquivo selecionado sobrevive à navegação.
- Responsivo (desktop, tablet, celular), navegável por teclado, `prefers-reduced-motion`, moeda e datas em pt-BR.

## Qualidade

- `npm run test:ci`: 55 testes (parser CSV, agregações, tradução de motivos, erros 4xx/5xx, service HTTP com interceptor, store com duplo envio, estados críticos das páginas de upload e resultado).
- `npm run build`: build de produção sem avisos.
- `npm run e2e`: upload → resultado → filtro → painel de recusa → nota aprovada → visão geral → celular, contra a API real, sem erros de console e sem rolagem horizontal em 390px.

## Limitações reais

- A API tem **um único endpoint de simulação**. Não há confirmação da antecipação, histórico nem consulta de configuração (ver [API_INTEGRATION.md](API_INTEGRATION.md#o-que-falta-no-backend-não-implementado)).
- Sacado, cedente, tipo e vencimento exibidos vêm do CSV lido no navegador, não da API. Se a leitura local divergir da do servidor, esses campos aparecem como indisponíveis.
- A taxa de desconto não é exposta pela API. O front mostra o desconto efetivo (desconto ÷ valor de face) e explica o método, sem citar a taxa.
- Para notas aprovadas, não é possível listar cada regra verificada: a API só informa as regras não atendidas.
- A "última análise" fica no `localStorage` deste navegador. Arquivos muito grandes podem ser salvos sem as linhas do CSV.
- Ambiente de desenvolvimento do backend: o estoque tem 1 nota e a base de situação cadastral conhece só 2 CNPJs. Qualquer outro sacado faz o lote inteiro voltar 503, e a regra de concentração de 10% recusa quase todas as notas. Isso é comportamento real do backend, não do front.
- O front não aplica nenhuma regra de elegibilidade. A validação local é só de formato e espelha o parser do backend.
