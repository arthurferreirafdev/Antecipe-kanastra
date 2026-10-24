import { LinhaCsv } from './csv.models';
import { Motivo, ResultadoLote, StatusRecebivel } from './api.models';

/** Uma análise concluída, guardada no navegador para a visão geral e para navegação. */
export interface AnaliseSalva {
  versao: 1;
  arquivoNome: string;
  arquivoTamanho: number;
  /** ISO 8601 do momento em que a API respondeu. */
  analisadaEm: string;
  resultado: ResultadoLote;
  /** Linhas do CSV enviado; podem faltar se o arquivo for grande demais para o armazenamento local. */
  linhas: LinhaCsv[] | null;
}

/** Resultado da API combinado com os dados do CSV da mesma linha. */
export interface NotaAnalisada {
  indice: number;
  numeroDocumento: string;
  status: StatusRecebivel;
  valorNominal: number;
  valorLiquido: number | null;
  /** Valor nominal − valor líquido devolvido pela API (somente aprovadas). */
  desconto: number | null;
  motivos: Motivo[];
  csv: LinhaCsv | null;
}

export interface MotivoAgrupado {
  codigo: string;
  quantidade: number;
  valorNominal: number;
}

export interface ResumoAnalise {
  total: number;
  aprovadas: number;
  recusadas: number;
  pendentes: number;
  /** Soma do valor nominal de todas as notas enviadas. */
  valorNominalEnviado: number;
  /** Soma do valor nominal das notas aprovadas. */
  valorNominalElegivel: number;
  /** `valorTotalLiquido` da API. */
  valorLiquido: number;
  /** valorNominalElegivel − valorLiquido. */
  desconto: number;
  /** aprovadas / total (0..1). */
  percentualElegibilidade: number;
  /** desconto / valorNominalElegivel (0..1). */
  descontoEfetivo: number;
  motivos: MotivoAgrupado[];
}
