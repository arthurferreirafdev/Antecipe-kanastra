// Contratos da API .NET (antecipe-kanastra-back/Domain/Models.cs e Controllers/NotasController.cs).
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
}

/** `ResultadoLote` — resposta 200 de POST /api/notas/analisar. */
export interface ResultadoLote {
  totalRecebiveis: number;
  quantidadeAprovada: number;
  quantidadeRecusada: number;
  quantidadePendente: number;
  valorTotalLiquido: number;
  resultados: ResultadoRecebivel[];
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
