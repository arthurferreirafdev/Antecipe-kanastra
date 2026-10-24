# API de análise de notas

Execute com `dotnet run --project antecipe-kanastra-back/antecipe-kanastra-back` a partir da raiz do repositório, ou com `docker compose -f antecipe-kanastra/compose.yaml up --build`.

Abra `http://localhost:8080/swagger` para testar a API pelo navegador. Na operação `POST /api/notas/analisar`, clique em **Try it out**, selecione o CSV no campo `arquivo` e clique em **Execute**.

Envie um CSV em `multipart/form-data` no campo `arquivo`:

```bash
curl -F "arquivo=@notas.csv" http://localhost:8080/api/notas/analisar
```

O cabeçalho deve ser `NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO`. Use `;` como separador, vírgula decimal e datas `dd/MM/aaaa`.

A resposta contém o status e os motivos de cada nota, o valor líquido das aprovadas e `valorTotalLiquido`. A rota simula a análise; ela não acrescenta notas ao estoque. CSVs inválidos recebem HTTP 400. Se alguma verificação técnica estiver indisponível, a rota recebe HTTP 503 e informa as notas pendentes, sem classificá-las como recusadas.

As regras e a precificação vêm de `antecipe-kanastra/antecipe-kanastra/validators.json`. Em uma instalação separada, defina `ANTECIPE_CONFIG` para apontar a outro arquivo com o mesmo formato e `ANTECIPE_ESTOQUE` para apontar ao CSV do estoque.
