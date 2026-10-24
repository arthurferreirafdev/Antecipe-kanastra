// Linhas do CSV lidas no navegador. Servem para pré-visualizar o arquivo e para exibir dados
// que a API não devolve (sacado, cedente, vencimento...). Nenhuma regra de elegibilidade é aplicada aqui.

export const COLUNAS_CSV = [
  'NOME_FUNDO',
  'DOC_FUNDO',
  'NOME_CEDENTE',
  'DOC_CEDENTE',
  'NOME_SACADO',
  'DOC_SACADO',
  'NU_DOCUMENTO',
  'TIPO_RECEBIVEL',
  'VALOR_NOMINAL',
  'DATA_VENCIMENTO_ORIGINAL',
  'DATA_AQUISICAO',
] as const;

export type ColunaCsv = (typeof COLUNAS_CSV)[number];

export interface LinhaCsv {
  /** Número da linha física no arquivo (1 = cabeçalho). */
  linha: number;
  nomeFundo: string;
  docFundo: string;
  nomeCedente: string;
  docCedente: string;
  nomeSacado: string;
  docSacado: string;
  numeroDocumento: string;
  tipoRecebivel: string;
  /** `null` quando o texto não segue o formato aceito pela API. */
  valorNominal: number | null;
  /** Data ISO `aaaa-mm-dd`, ou `null` quando inválida. */
  dataVencimento: string | null;
  dataAquisicao: string | null;
}

export type SeveridadeProblema = 'erro' | 'aviso';

export interface ProblemaCsv {
  severidade: SeveridadeProblema;
  mensagem: string;
  /** Linha física do arquivo, quando o problema é de uma linha específica. */
  linha?: number;
  coluna?: ColunaCsv;
}

export interface LeituraCsv {
  linhas: LinhaCsv[];
  problemas: ProblemaCsv[];
  /** `true` quando não há problemas de severidade `erro`. */
  valido: boolean;
}
