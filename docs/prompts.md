# Prompts principais do projeto Antecipe

Versões revisadas dos prompts usados para construir o projeto, em ordem cronológica. Cada prompt traz o contexto, o que deve ser feito e como verificar o resultado. O registro literal das mensagens está em [`historico-prompts.md`](historico-prompts.md).

---

## 1. Ambiente Docker para back e front

> Crie a configuração Docker para rodar o projeto inteiro com um único comando.
>
> **Contexto:** o back é uma API .NET 10 em `antecipe-kanastra-back/`. O front é Angular em `antecipe-kanastra-front/`. As regras do fundo ficam em `antecipe-kanastra/antecipe-kanastra/validators.json`.
>
> **Requisitos:**
> - Um `Dockerfile` para cada aplicação, com build em múltiplos estágios (SDK/Node para compilar, runtime enxuto para executar).
> - Um único `compose.yaml` na raiz que builde e suba os dois serviços.
> - O front servido por nginx, repassando `/api` para a API, para evitar CORS.
> - Portas configuráveis por variável (`API_PORT`, `WEB_PORT`) e healthcheck na API, para o front só subir depois dela.
>
> **Critério de aceite:** `docker compose up --build` deixa o front em `http://localhost:4200` e o Swagger em `http://localhost:8080/swagger`, com o upload funcionando de ponta a ponta.

---

## 2. Resultado por nota, motivos claros e download sem recusadas

> Evolua o endpoint `POST /api/notas/analisar` (upload do CSV de recebíveis).
>
> **Requisitos:**
> 1. **Processar todas as linhas.** Uma nota recusada ou com erro de formato (valor, data, colunas) não pode interromper o lote. Linhas inválidas voltam como recusadas, com o motivo.
> 2. **Resultado de cada nota:** status `APROVADA` ou `RECUSADA`, número da linha e dados originais. Uma nota aprovada recebe só o status, sem outros efeitos colaterais.
> 3. **Motivos em linguagem simples.** Cada recusa explica o problema e o valor que causou a recusa, de forma que alguém sem conhecimento técnico entenda. Ex.: "O tipo de recebível "Cheque" não é aceito pelo fundo. Tipos aceitos: Duplicata, Duplicata de Serviço."
> 4. **Duplicadas:** identificar as linhas repetidas no arquivo, manter a primeira ocorrência e devolver a lista das repetições com a linha original e uma explicação.
> 5. **Recusadas em memória:** guardar as notas recusadas de cada lote (arquivo, data, linha, dados e motivos), consultáveis por um `loteId`.
> 6. **Pergunta de download:** quando houver recusadas ou duplicadas e sobrar alguma aprovada, a resposta deve trazer a pergunta "Deseja baixar o arquivo CSV apenas com as notas aprovadas?" e o conteúdo desse CSV.
>
> **Restrições:** manter os códigos de motivo existentes (o front os usa) e os trechos das mensagens que o front lê por regex.
>
> **Critério de aceite:** um CSV com notas válidas, recusadas, com erro de formato e duplicadas retorna o resultado de todas as linhas, e o CSV oferecido contém só as aprovadas.

---

## 3. Tabela de notas com motivos detalhados

> Na tela de resultado da análise, exiba todas as notas analisadas em uma tabela estruturada.
>
> **Requisitos:**
> - Colunas: documento, sacado, vencimento, valor nominal, status (aprovada, recusada ou pendente) e valor líquido.
> - Em cada nota recusada ou pendente, um campo expansível na própria linha que lista **todos** os motivos: título, explicação em linguagem simples e "o que fazer" para resolver.
> - Manter busca, filtros por status e motivo, ordenação, paginação e o painel de detalhes.
> - Acessível (`aria-expanded`, foco por teclado) e funcional no celular.
>
> **Critério de aceite:** em um lote com recusas, cada nota recusada abre seus motivos sem sair da tabela.

---

## 4. Resultado nunca bloqueado por verificação indisponível

> A API está respondendo HTTP 503 quando uma verificação externa (ex.: situação cadastral do sacado) não pode ser concluída, e o cliente fica sem ver nenhum resultado.
>
> **Requisitos:**
> - Sempre responder 200 com o resultado de todas as notas.
> - Uma nota com verificação inconclusiva fica `PENDENTE_VALIDACAO`, com o motivo.
> - Se alguma regra já recusou a nota, ela fica `RECUSADA`: uma recusa definitiva prevalece sobre uma pendência.
> - Mensagem de indisponibilidade clara para o usuário, com orientação de tentar mais tarde.
>
> **Critério de aceite:** um lote com uma nota já existente na carteira e outra de sacado sem situação cadastral volta como 200, com uma recusada e uma pendente, e ambas aparecem na tela.

---

## 5. Correção do algoritmo de validação contra o resultado esperado

> O arquivo `estoque_antecipe.csv` (100.554 linhas, 18 fundos) tem o resultado oficial em `resultado_esperado_100k.json` (`linha`, `id`, `status` APROVADO/BARRADO e `barrado_por`). Hoje nenhuma nota bate com o esperado.
>
> **Tarefa:**
> 1. Comparar regra a regra o resultado da API com o esperado e descobrir a semântica correta de cada validador (`validators.json`).
> 2. Corrigir o motor de regras até o resultado ficar idêntico ao esperado em todas as linhas: status, `id` e códigos de `barrado_por`, na mesma ordem.
> 3. Verificar com testes automatizados e com o upload real do arquivo na API.
>
> **Pontos a investigar:** data de referência dos prazos (data da análise ou `DATA_AQUISICAO`), cálculo da concentração por sacado (sequencial ou sobre a carteira projetada do fundo), notas de outros fundos, critério de duplicidade (parcelas do mesmo documento) e a base de situação cadastral.
>
> **Documentação:** no `README.md` da raiz, listar as variáveis de ambiente necessárias para executar back, front e Docker Compose, com valores padrão e uso.
>
> **Critério de aceite:** 100.554 de 100.554 linhas iguais ao esperado (85.215 aprovadas e 15.339 barradas), testes passando e README atualizado.

---

## 6. Ajustes de UX na tela de resultado

> Dois problemas na tela de resultado:
>
> 1. **Valor "Você recebe" sobrepondo o layout.** Valores grandes (ex.: R$ 406.209.535,82) invadem o bloco "Como chegamos a esse valor". O valor deve se ajustar à largura da coluna em qualquer tamanho de tela, sem cortar dígitos, mantendo o tamanho grande para valores curtos.
> 2. **Pergunta de download no momento errado.** O diálogo "Algumas notas ficaram de fora", com ícone de alerta, aparece antes do resultado e passa a impressão de erro. Ir direto ao resultado e oferecer ali o download do CSV só com as aprovadas: um card de tom positivo, com a pergunta, a lista de duplicadas (se houver), os botões "Baixar CSV" e "Agora não", e um botão permanente no cabeçalho.
>
> **Critério de aceite:** com o arquivo de 100 mil linhas, o valor cabe na coluna e o download fica disponível na tela de resultado sem nenhum diálogo de alerta.

---

## 7. Erro ao iniciar o front (`piscina` não encontrado)

> Ao rodar `npm start`, o front falha com `Error: Cannot find module '.../node_modules/piscina/dist/main.js'` (`MODULE_NOT_FOUND`).
>
> **Tarefa:** diagnosticar a causa (código, configuração ou instalação de dependências), corrigir e confirmar que o Angular CLI sobe. Se a versão do Node não for compatível, informar a versão mínima exigida e como atualizar.
>
> **Critério de aceite:** `node_modules` íntegro, causa explicada e instrução clara para subir o front.

---

## 8. Histórico de prompts

> Crie um arquivo `.md` com o histórico dos prompts principais usados neste projeto, reescritos de forma mais completa: contexto, requisitos e critérios de aceite, para que possam ser reutilizados ou auditados.

---

## 9. Reorganização do repositório e documentação completa

> Reorganize o repositório para que qualquer pessoa consiga clonar, entender e rodar o projeto.
>
> **Requisitos:**
> - Renomear as pastas para `api/` (back-end .NET) e `web/` (front-end Angular), direto na raiz.
> - Renomear os projetos e namespaces (`Antecipe.Api`, `Antecipe.Api.Tests`, solução `Antecipe.sln`, projeto Angular `antecipe-web`) e corrigir todas as referências: `ProjectReference`, `using`, caminhos de arquivos de dados, saída do build.
> - Levar a configuração e as bases (`validators.json`, estoque, situações cadastrais) para dentro da API, em `Dados/`, e remover o protótipo de validadores que não era usado.
> - Reescrever os Dockerfiles (cada um com seu contexto) e o `compose.yaml`. Tirar do git os artefatos de build e IDE (`bin/`, `obj/`, `.idea/`).
> - Mover o material do hackathon e o histórico de prompts para `docs/`.
> - Escrever um README completo na raiz: o que é o projeto, estrutura, arquitetura, como executar (Docker e local), variáveis de ambiente, formato do CSV, regras de validação, precificação, API, telas do front, testes e solução de problemas.
>
> **Critério de aceite:** `docker compose up --build` e `dotnet run` + `npm start` funcionam a partir de um clone limpo; os testes passam; nenhuma referência aos nomes antigos.

