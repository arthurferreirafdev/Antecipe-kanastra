# Integração com a API do Antecipe

Contratos levantados diretamente do código da API em `../api/Antecipe.Api` e conferidos com a API rodando (respostas reais via `curl` e teste E2E).

## Visão geral

| Item | Situação no backend |
|---|---|
| Controllers | **1** — `Controllers/NotasController.cs` (`[Route("api/notas")]`) |
| Endpoints | `POST /api/notas/analisar` e `GET /api/notas/lotes/{loteId}/recusadas` |
| Autenticação | Nenhuma |
| CORS | **Não configurado** — o front usa o proxy do `ng serve` (`proxy.conf.mjs`) |
| OpenAPI | `GET /openapi/v1.json` · Swagger UI em `/swagger` |
| Serialização | System.Text.Json, camelCase, enums como string, `decimal` como número |

## `POST /api/notas/analisar`

Simula a antecipação de um lote: valida cada nota contra as regras ativas do fundo (`validators.json`) e precifica as aprovadas. **Não grava nada no estoque**: o controller chama `MotorRegras.Processar(notas, confirmar: false)`.

### Requisição

`multipart/form-data` com um arquivo no campo **`arquivo`**.

```
NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO
ANTECIPE FIDC;92.234.400/0001-80;Atelier Ipê Móveis;11.111.111/0001-91;Mercado Serra Azul;58.206.002/0001-11;NF-1001;Duplicata;100,00;15/01/2027;24/10/2026
```

Regras de leitura (`Infrastructure/CsvRecebiveis.cs`):

- Cabeçalho exatamente igual ao acima, na mesma ordem.
- Separador `;`, campos podem vir entre aspas; espaços nas pontas são ignorados; linhas em branco são ignoradas.
- `VALOR_NOMINAL`: cultura pt-BR com `NumberStyles.AllowDecimalPoint | AllowLeadingSign` → vírgula decimal e **sem separador de milhar** (`4.250,00` é recusado). Precisa ser > 0.
- Datas `dd/MM/yyyy` exatas.
- `NU_DOCUMENTO` não pode ser vazio.
- Leitura em UTF-8.

### 200 — `ResultadoLote` (`Domain/Models.cs`)

```jsonc
{
  "totalRecebiveis": 19,
  "quantidadeAprovada": 12,
  "quantidadeRecusada": 7,
  "quantidadePendente": 0,
  "valorTotalLiquido": 15743.40,
  "resultados": [
    { "linha": 2, "numeroDocumento": "NF-1001", "status": "APROVADA", "valorNominal": 1250.00, "valorLiquido": 1180.83, "motivos": [], "dados": { "...": "..." } },
    {
      "linha": 18, "numeroDocumento": "NF-1017", "status": "RECUSADA", "valorNominal": 6780.00, "valorLiquido": null,
      "motivos": [{ "codigo": "PRAZO_MAXIMO", "mensagem": "A nota vence em 30/09/2027. Prazo de 341 dias excede o máximo de 180 dias aceito pelo fundo." }],
      "dados": { "...": "..." }
    }
  ],
  "aceitas": [], "recusadas": [], "duplicadas": [],
  "loteId": "…",
  "perguntaDownload": "7 notas foram recusadas. Deseja baixar o arquivo CSV apenas com as 12 notas aprovadas?",
  "csvSemRecusadas": "NOME_FUNDO;…"
}
```

| Campo | Tipo | Observação |
|---|---|---|
| `status` | `"APROVADA" \| "RECUSADA" \| "PENDENTE_VALIDACAO"` | `PENDENTE_VALIDACAO`: alguma verificação não pôde ser concluída e nenhuma regra recusou a nota |
| `valorLiquido` | `number \| null` | Só para aprovadas |
| `motivos` | `{ codigo, mensagem }[]` | Todas as regras não atendidas, não só a primeira |
| `resultados` | array | Ordenado por `linha` (linha do CSV, cabeçalho = 1); `dados` traz os valores originais das colunas |
| `duplicadas` | array | Repetições removidas do lote (fundo + cedente + documento + vencimento), com `linhaOriginal` e `mensagem` |
| `perguntaDownload`, `csvSemRecusadas` | `string \| null` | Presentes quando houve recusadas ou duplicadas e alguma aprovada |

### 400 — arquivo inválido

```json
{ "erro": "Cabeçalho CSV inválido. Verifique nomes e ordem das colunas." }
```

Mensagens possíveis: arquivo ausente/vazio, `CSV vazio.`, cabeçalho inválido, `O CSV não contém notas.` Linhas com valor, data, número de documento ou quantidade de colunas inválidos **não** geram 400: voltam como `RECUSADA` com o código `FORMATO_INVALIDO`.

> O OpenAPI declara o 400 como `ProblemDetails`, mas o controller devolve `{ erro }`. O front aceita os dois formatos.

### Verificação indisponível

Quando alguma regra fica inconclusiva (ex.: o CNPJ do sacado não existe na base de situação cadastral), a resposta continua sendo **200**: a nota volta como `PENDENTE_VALIDACAO` com o motivo, e as demais notas são classificadas normalmente. Se outra regra já recusou a nota, ela fica `RECUSADA`.

### 500 — configuração de regras inválida

`ProblemDetails` com `detail: "Configuração de regras inválida: ..."`.

### Códigos de motivo

| Código | Mensagem do backend (padrão) | Título no front |
|---|---|---|
| `FORMATO_INVALIDO` | O valor nominal "abc" não é um número válido… / A data de vencimento "…" é inválida… | Dados da nota em formato inválido |
| `RECEBIVEL_DUPLICADO` | Esta nota já faz parte da carteira do fundo… / Esta nota aparece mais de uma vez no lote enviado. | Nota já enviada |
| `CNPJ_SACADO_VALIDO` | O CNPJ do sacado (X) é inválido. Confira… | CNPJ do cliente inválido |
| `SITUACAO_CADASTRAL_SACADO` | A empresa sacada está com situação cadastral X na Receita Federal. O fundo só aceita… | Cliente com cadastro irregular |
| `TIPO_RECEBIVEL` | O tipo de recebível "X" não é aceito pelo fundo. Tipos aceitos: … | Tipo de título não aceito |
| `VALOR_MINIMO` | … abaixo do mínimo de R$ N permitido pelo fundo. | Valor abaixo do mínimo |
| `NAO_VENCIDA` | A nota venceu em D1, antes da data de aquisição (D2). | Nota já vencida |
| `PRAZO_MAXIMO` | A nota vence em D. Prazo de N dias excede o máximo de M dias aceito pelo fundo. | Prazo acima do permitido |
| `PRAZO_MINIMO` | A nota vence em D. Prazo de N dias abaixo do mínimo de M dias exigido pelo fundo. | Prazo abaixo do mínimo |
| `CONCENTRACAO_MAXIMA_SACADO` | Concentração projetada do sacado: X% (limite Y%). Aceitar esta nota… | Limite por cliente atingido |
| `CONCENTRACAO_MAXIMA_CEDENTE` | Concentração projetada do cedente: X% (limite Y%). Aceitar esta nota… | Limite por empresa cedente atingido |
| `PRECIFICACAO_INVALIDA` | O desconto da antecipação ficaria maior que o valor da nota… | Não foi possível calcular o valor |

Os números exibidos como "identificado" e "limite" são **extraídos da mensagem** (`shared/motivos/motivos-catalogo.ts`). Se a mensagem mudar de formato, o front mostra só a explicação e a mensagem original, sem números. Códigos desconhecidos aparecem com a mensagem do servidor.

## Mapeamento no front

| Backend | Front |
|---|---|
| `ResultadoLote`, `ResultadoRecebivel`, `Motivo`, `StatusRecebivel` | `src/app/models/api.models.ts` |
| Corpos de erro 400/503/500 | `ErroApiBody`, `ErroPendenciasBody`, `ProblemDetails` em `api.models.ts` |
| `NotasController.Analisar` | `NotasApiService.analisar(arquivo)` — `src/app/core/api/notas-api.service.ts` |
| Tradução de erros HTTP | `apiErrorInterceptor` + `paraApiError` — `src/app/core/api/` |
| Estado (loading / sucesso / erro, duplo envio) | `AnaliseStore` — `src/app/core/state/analise.store.ts` |
| URL base | `environment.apiBaseUrl` → `API_BASE_URL` (`src/app/core/api/api-config.ts`) |

### O que o front calcula (e o que não calcula)

- **Não recalcula** elegibilidade, prazo, concentração ou preço. Tudo vem da API.
- Sacado, cedente, tipo e vencimento exibidos vêm do **CSV lido no navegador**, associados a cada resultado pelo campo `linha`, com conferência do número do documento.
- Agregações exibidas: soma do valor nominal (enviado e aprovado), desconto = valor nominal aprovado − `valorLiquido`, elegibilidade = aprovadas / total. São operações sobre valores já devolvidos, feitas em centavos.

## O que falta no backend (não implementado)

Itens que melhorariam a experiência e que **não existem hoje**. O front não simula nenhum deles.

1. **`GET /api/configuracao`** (regras ativas, parâmetros e taxa de precificação). Sem ele, o front não consegue listar "regras verificadas" de notas aprovadas nem mostrar a taxa mensal aplicada.
2. **Detalhe da precificação por nota** (`dias`, `taxa`, `dataBase`) em `ResultadoRecebivel`. Hoje só vem o valor líquido.
3. **Confirmação da antecipação** (`MotorRegras.Processar(..., confirmar: true)` existe, mas nenhuma rota a expõe). Sem ela, a jornada termina na simulação.
4. **Histórico persistido de análises.** A "última análise" fica só no navegador, e as recusadas só na memória da API.
5. **CORS** para servir o front em outra origem sem proxy.
6. Alinhar o OpenAPI do 400 com `{ erro }`.
