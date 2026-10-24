// Contratos da API .NET (api/Antecipe.Api/Domain/Models.cs e Controllers/NotasController.cs).
// O ASP.NET serializa com camelCase; decimais chegam como number; o enum de status chega como string.

/** `StatusRecebivel` — JsonStringEnumConverter. */
export type StatusRecebivel = 'APROVADA' | 'RECUSADA' | 'PENDENTE_VALIDACAO';

/** `Motivo(Codigo, Mensagem)` — uma regra não atendida ou inconclusiva. */
export interface Motivo {
  codigo: string;
  mensagem: string;
}

/** `ResultadoRecebivel` — uma linha do CSV, na mesma ordem do arquivo enviado. */
export interface ResultadoRecebivel {
  numeroDocumento: string;
  status: StatusRecebivel;
  valorNominal: number;
  /** Preenchido somente para notas aprovadas. */
  valorLiquido: number | null;
  motivos: Motivo[];
  /** Número da linha no CSV (cabeçalho = 1). */
  linha?: number | null;
  /** Valores originais das colunas do CSV, preservados inclusive para linhas com erro de formato. */
  dados?: Record<string, string> | null;
}

/** `ResultadoLote` — resposta 200 de POST /api/notas/analisar. */
export interface ResultadoLote {
  totalRecebiveis: number;
  quantidadeAprovada: number;
  quantidadeRecusada: number;
  quantidadePendente: number;
  valorTotalLiquido: number;
  resultados: ResultadoRecebivel[];
  /** Relatório: subconjunto de `resultados` com status APROVADA. */
  aceitas?: ResultadoRecebivel[];
  /** Relatório: subconjunto de `resultados` com status RECUSADA (inclui linhas com erro de formato). */
  recusadas?: ResultadoRecebivel[];
  /** Repetições removidas do lote antes da análise (a primeira ocorrência é mantida). */
  duplicadas?: NotaDuplicada[];
  /** Identificador do lote; as recusadas ficam em GET /api/notas/lotes/{loteId}/recusadas. */
  loteId?: string | null;
  /** Pergunta ao usuário sobre o download; presente só quando houve recusadas ou duplicadas e alguma aprovada. */
  perguntaDownload?: string | null;
  /** O CSV enviado apenas com as notas aprovadas (sem recusadas e sem duplicadas). */
  csvSemRecusadas?: string | null;
}

/** `NotaDuplicada` — linha do CSV que repete fundo, cedente, número de documento e vencimento de outra. */
export interface NotaDuplicada {
  linha: number;
  /** Linha da primeira ocorrência, que foi mantida e analisada. */
  linhaOriginal: number;
  numeroDocumento: string;
  dados: Record<string, string>;
  /** Explicação em linguagem simples. */
  mensagem: string;
}

/** Corpo de 400 (arquivo ausente/vazio/CSV inválido) e base do 503. */
export interface ErroApiBody {
  erro: string;
}

/** Item de `pendencias` no 503. */
export interface PendenciaAnalise {
  numeroDocumento: string;
  motivos: Motivo[];
}

/** Corpo de 503: alguma verificação técnica não pôde ser concluída. */
export interface ErroPendenciasBody extends ErroApiBody {
  pendencias: PendenciaAnalise[];
}

/** Corpo de 500 (`Problem(...)`) e de falhas de model binding do [ApiController]. */
export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
}
