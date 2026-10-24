# Integração com a API do Antecipe

Contratos levantados diretamente do código do backend em `../antecipe-kanastra-back` e conferidos com a API rodando (respostas reais via `curl` e teste E2E).

## Visão geral

| Item | Situação no backend |
|---|---|
| Controllers | **1** — `Controllers/NotasController.cs` (`[Route("api/notas")]`) |
| Endpoints | **1** — `POST /api/notas/analisar` |
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
  "totalRecebiveis": 9,
  "quantidadeAprovada": 1,
  "quantidadeRecusada": 8,
  "quantidadePendente": 0,
  "valorTotalLiquido": 94.47,
  "resultados": [
    { "numeroDocumento": "NF-1001", "status": "APROVADA", "valorNominal": 100.00, "valorLiquido": 94.47, "motivos": [] },
    {
      "numeroDocumento": "NF-1007", "status": "RECUSADA", "valorNominal": 6780.00, "valorLiquido": null,
      "motivos": [{ "codigo": "PRAZO_MAXIMO", "mensagem": "Prazo de 341 dias excede o máximo de 180 dias." }]
    }
  ]
}
```

| Campo | Tipo | Observação |
|---|---|---|
| `status` | `"APROVADA" \| "RECUSADA" \| "PENDENTE_VALIDACAO"` | `PENDENTE_VALIDACAO` nunca chega em 200 (vira 503) |
| `valorLiquido` | `number \| null` | Só para aprovadas |
| `motivos` | `{ codigo, mensagem }[]` | Todas as regras não atendidas, não só a primeira |
| `resultados` | array | **Mesma ordem das linhas do CSV** |

### 400 — arquivo inválido

```json
{ "erro": "Cabeçalho CSV inválido. Verifique nomes e ordem das colunas." }
```

Mensagens possíveis: arquivo ausente/vazio, `CSV vazio.`, cabeçalho inválido, `Linha N: quantidade de colunas inválida.`, `Linha N: valor, data ou número de documento inválido.`, `O CSV não contém notas.`

> O OpenAPI declara o 400 como `ProblemDetails`, mas o controller devolve `{ erro }`. O front aceita os dois formatos.
> O `N` das mensagens de linha vem de `TextFieldParser.LineNumber`, que aponta para a linha **seguinte**. Por isso o front valida o arquivo antes do envio e mostra a linha exata.

### 503 — verificação indisponível

Quando alguma regra fica inconclusiva (ex.: o CNPJ do sacado não existe na base de situação cadastral), **o lote inteiro** volta como 503 e nenhuma nota é classificada:

```json
{
  "erro": "Não foi possível concluir a análise de todas as notas. Tente novamente quando as verificações estiverem disponíveis.",
  "pendencias": [{ "numeroDocumento": "X1", "motivos": [{ "codigo": "SITUACAO_CADASTRAL_SACADO", "mensagem": "Situação cadastral do sacado indisponível para verificação." }] }]
}
```

### 500 — configuração de regras inválida

`ProblemDetails` com `detail: "Configuração de regras inválida: ..."`.

### Códigos de motivo

| Código | Mensagem do backend (padrão) | Título no front |
|---|---|---|
| `FUNDO_INCORRETO` | Recebível não pertence ao fundo configurado. | Nota de outro fundo |
| `RECEBIVEL_DUPLICADO` | Recebível já existe no estoque ou está repetido no lote. | Nota já enviada |
| `CNPJ_SACADO_VALIDO` | CNPJ do sacado inválido. | CNPJ do cliente inválido |
| `SITUACAO_CADASTRAL_SACADO` | Situação cadastral do sacado: X. Aceitas: A, B. | Cliente com cadastro irregular |
| `TIPO_RECEBIVEL` | Tipo de recebível não aceito. Tipos aceitos: … | Tipo de título não aceito |
| `VALOR_MINIMO` | … abaixo do mínimo de R$ N permitido pelo fundo. | Valor abaixo do mínimo |
| `NAO_VENCIDA` | A nota venceu em D1, antes da data da análise (D2). | Nota já vencida |
| `PRAZO_MAXIMO` | Prazo de N dias excede o máximo de M dias. | Prazo acima do permitido |
| `PRAZO_MINIMO` | Prazo de N dias abaixo do mínimo de M dias. | Prazo abaixo do mínimo |
| `CONCENTRACAO_MAXIMA_SACADO` | Concentração projetada do sacado: X% (limite Y%). | Limite por cliente atingido |
| `CONCENTRACAO_MAXIMA_CEDENTE` | Concentração projetada do cedente: X% (limite Y%). | Limite por empresa cedente atingido |
| `PRECIFICACAO_INVALIDA` | (exceção da precificação) | Não foi possível calcular o valor |

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
- Os dados que a API não devolve (sacado, cedente, tipo, vencimento) vêm do **próprio CSV enviado**, lido no navegador. A linha *i* do arquivo corresponde a `resultados[i]`, e o número do documento é conferido antes de associar.
- Agregações exibidas: soma do valor nominal (enviado e aprovado), desconto = valor nominal aprovado − `valorLiquido`, elegibilidade = aprovadas / total. São operações sobre valores já devolvidos, feitas em centavos.

## O que falta no backend (não implementado)

Itens que melhorariam a experiência e que **não existem hoje**. O front não simula nenhum deles.

1. **`GET /api/configuracao`** (regras ativas, parâmetros e taxa de precificação). Sem ele, o front não consegue listar "regras verificadas" de notas aprovadas nem mostrar a taxa mensal aplicada.
2. **Detalhe da precificação por nota** (`dias`, `taxa`, `dataBase`) em `ResultadoRecebivel`. Hoje só vem o valor líquido.
3. **Dados de identificação no resultado** (sacado, vencimento). Hoje o front depende do CSV local.
4. **Confirmação da antecipação** (`MotorRegras.Processar(..., confirmar: true)` existe, mas nenhuma rota a expõe). Sem ela, a jornada termina na simulação.
5. **Histórico de análises** (`GET`). A "última análise" fica só no navegador.
6. **CORS** para servir o front em outra origem sem proxy.
7. Corrigir o deslocamento de +1 no número da linha das mensagens de 400 e alinhar o OpenAPI do 400 com `{ erro }`.
