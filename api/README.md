# Antecipe: API

API ASP.NET Core (.NET 10) que analisa lotes de recebíveis enviados em CSV. A documentação completa (regras, formato do CSV, contrato da API, variáveis de ambiente) está no [README da raiz](../README.md).

## Rodar

```bash
# a partir da raiz do repositório
dotnet run --project api/Antecipe.Api --urls http://localhost:5000
# Swagger: http://localhost:5000/swagger

dotnet test api/Antecipe.sln
```

Com Docker, na raiz: `docker compose up --build api`. A API fica em http://localhost:8080.

## Organização

| Pasta | Conteúdo |
|---|---|
| `Antecipe.Api/Controllers` | `NotasController`: `POST /api/notas/analisar` e `GET /api/notas/lotes/{loteId}/recusadas` |
| `Antecipe.Api/Application` | `MotorRegras` (orquestra a análise), `Validadores` (uma classe por regra), `ConfiguracaoService` (lê e valida `validators.json`), `PrecificacaoService` |
| `Antecipe.Api/Domain` | Modelos e chaves de documento (`Recebivel`, `ResultadoLote`, `Motivo`, `Documentos.Chave`...) |
| `Antecipe.Api/Infrastructure` | `CsvRecebiveis` (leitura tolerante, duplicadas, escrita), `EstoqueService`, `RecusasService` (recusadas em memória), transformador do OpenAPI para upload |
| `Antecipe.Api/Dados` | `validators.json`, `estoque_antecipe.csv`, `situacoes_cadastrais_dev.json` (copiados para a saída do build e do publish) |
| `Antecipe.Api.Tests` | Testes xUnit |

## Adicionar uma regra

1. Crie uma classe que implemente `IValidador` em `Application/Validadores.cs`, com um `Codigo` único, `ValidarParametros` (falha cedo se a configuração estiver errada) e `Validar`, que retorna `Aprovada`, `Recusada(mensagem)` ou `Indeterminada(mensagem)`. Escreva mensagens que o usuário final entenda.
2. Registre a classe em `Program.cs` (`AddSingleton<IValidador, NovaRegra>()`).
3. Inclua o código em `Dados/validators.json` com `"ativo": true` e os parâmetros.
4. Se a regra depende do conjunto da carteira (como a concentração), retorne `EhConcentracao => true`: ela será avaliada depois das demais, contra a carteira projetada.
5. No front, acrescente o código ao catálogo `web/src/app/shared/motivos/motivos-catalogo.ts` (título, explicação e orientação).
