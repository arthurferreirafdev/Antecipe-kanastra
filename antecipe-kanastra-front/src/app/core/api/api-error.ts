import { HttpErrorResponse } from '@angular/common/http';
import {
  ErroApiBody,
  ErroPendenciasBody,
  PendenciaAnalise,
  ProblemDetails,
} from '../../models/api.models';

// Normaliza as respostas de erro de POST /api/notas/analisar em mensagens que uma pessoa sem
// conhecimento técnico entenda, mantendo a mensagem original como detalhe técnico.

export type TipoErroApi =
  | 'rede'
  | 'arquivo-invalido'
  | 'arquivo-grande'
  | 'verificacao-indisponivel'
  | 'configuracao'
  | 'tempo-esgotado'
  | 'servidor';

export class ApiError extends Error {
  constructor(
    readonly tipo: TipoErroApi,
    readonly status: number,
    readonly titulo: string,
    readonly descricao: string,
    readonly detalheTecnico: string | null = null,
    readonly pendencias: PendenciaAnalise[] = [],
  ) {
    super(titulo);
    this.name = 'ApiError';
  }

  /** Erros que podem ser resolvidos tentando de novo, sem mudar o arquivo. */
  get temporario(): boolean {
    return (
      this.tipo === 'rede' ||
      this.tipo === 'tempo-esgotado' ||
      this.tipo === 'verificacao-indisponivel' ||
      this.status >= 500
    );
  }
}

function lerErro(body: unknown): string | null {
  if (body && typeof body === 'object') {
    const b = body as Partial<ErroApiBody & ProblemDetails>;
    if (typeof b.erro === 'string') return b.erro;
    if (typeof b.detail === 'string') return b.detail;
    if (typeof b.title === 'string') return b.title;
  }
  return typeof body === 'string' && body.trim() !== '' ? body : null;
}

function lerPendencias(body: unknown): PendenciaAnalise[] {
  const p = (body as Partial<ErroPendenciasBody> | null)?.pendencias;
  return Array.isArray(p)
    ? p.filter((x) => x && typeof x.numeroDocumento === 'string' && Array.isArray(x.motivos))
    : [];
}

/** Traduz as mensagens de 400 do controller (NotasController / CsvRecebiveis). */
export function explicarErroArquivo(mensagem: string): { titulo: string; descricao: string } {
  let m: RegExpExecArray | null;
  if (/não vazio no campo/i.test(mensagem)) {
    return {
      titulo: 'Nenhum arquivo recebido',
      descricao: 'O servidor não recebeu o arquivo ou ele chegou vazio. Selecione o CSV de novo.',
    };
  }
  if (/Cabeçalho CSV inválido/i.test(mensagem)) {
    return {
      titulo: 'Cabeçalho fora do padrão',
      descricao:
        'A primeira linha do arquivo precisa ter exatamente as colunas do modelo, na mesma ordem, separadas por ponto e vírgula.',
    };
  }
  if ((m = /^Linha (\d+): quantidade de colunas/i.exec(mensagem))) {
    return {
      titulo: 'Linha com colunas a mais ou a menos',
      descricao: `Perto da linha ${m[1]} há uma nota com número de colunas diferente do cabeçalho. Verifique se algum campo contém ponto e vírgula.`,
    };
  }
  if ((m = /^Linha (\d+): valor, data ou número/i.exec(mensagem))) {
    return {
      titulo: 'Valor, data ou documento fora do formato',
      descricao: `Perto da linha ${m[1]} há um valor, uma data ou um número de documento inválido. Use valores como 1250,90 (sem ponto de milhar) e datas como 15/01/2027.`,
    };
  }
  if (/não contém notas|CSV vazio/i.test(mensagem)) {
    return {
      titulo: 'Nenhuma nota no arquivo',
      descricao: 'O arquivo tem apenas o cabeçalho. Inclua ao menos uma nota e envie de novo.',
    };
  }
  return { titulo: 'Não foi possível ler o arquivo', descricao: mensagem };
}

export function paraApiError(erro: unknown): ApiError {
  if (erro instanceof ApiError) return erro;
  if (!(erro instanceof HttpErrorResponse)) {
    return new ApiError(
      'servidor',
      0,
      'Algo deu errado',
      'Ocorreu um erro inesperado. Tente novamente.',
      String(erro),
    );
  }

  const mensagem = lerErro(erro.error);
  const tecnico = `HTTP ${erro.status}${mensagem ? ` — ${mensagem}` : ''}`;

  switch (true) {
    case erro.status === 0:
      return new ApiError(
        'rede',
        0,
        'Sem conexão com o servidor',
        'Não conseguimos falar com o serviço de análise. Verifique sua conexão ou se a API está no ar e tente novamente.',
        erro.message,
      );
    case erro.status === 400: {
      const { titulo, descricao } = explicarErroArquivo(mensagem ?? '');
      return new ApiError('arquivo-invalido', 400, titulo, descricao, tecnico);
    }
    case erro.status === 413:
      return new ApiError(
        'arquivo-grande',
        413,
        'Arquivo grande demais',
        'O servidor recusou o arquivo pelo tamanho. Divida as notas em arquivos menores.',
        tecnico,
      );
    case erro.status === 503:
      return new ApiError(
        'verificacao-indisponivel',
        503,
        'Algumas verificações não puderam ser concluídas',
        'Para não recusar notas por engano, o fundo interrompeu a análise. Nenhuma nota foi classificada. Tente novamente em alguns minutos.',
        tecnico,
        lerPendencias(erro.error),
      );
    case erro.status === 500 && /Configuração de regras inválida/i.test(mensagem ?? ''):
      return new ApiError(
        'configuracao',
        500,
        'As regras do fundo estão indisponíveis',
        'A configuração de regras do fundo está com problema, então nenhuma análise pode ser feita agora. A gestão do fundo precisa revisá-la.',
        tecnico,
      );
    case erro.status === 404:
      return new ApiError(
        'servidor',
        404,
        'Serviço de análise não encontrado',
        'O endereço da API parece incorreto. Verifique a configuração do front.',
        tecnico,
      );
    default:
      return new ApiError(
        'servidor',
        erro.status,
        'O servidor não conseguiu concluir a análise',
        'Tente novamente em instantes.',
        tecnico,
      );
  }
}
