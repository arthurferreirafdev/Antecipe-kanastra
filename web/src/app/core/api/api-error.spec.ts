import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, explicarErroArquivo, paraApiError } from './api-error';

const http = (status: number, error: unknown) =>
  new HttpErrorResponse({ status, error, url: '/api/notas/analisar' });

describe('paraApiError', () => {
  it('status 0 vira erro de rede temporário', () => {
    const e = paraApiError(http(0, null));
    expect(e.tipo).toBe('rede');
    expect(e.temporario).toBe(true);
  });

  it('400 com { erro } vira arquivo inválido com mensagem humana e detalhe técnico', () => {
    const e = paraApiError(
      http(400, { erro: 'Cabeçalho CSV inválido. Verifique nomes e ordem das colunas.' }),
    );
    expect(e.tipo).toBe('arquivo-invalido');
    expect(e.titulo).toBe('Cabeçalho fora do padrão');
    expect(e.detalheTecnico).toContain('Cabeçalho CSV inválido');
    expect(e.temporario).toBe(false);
  });

  it('400 em formato ProblemDetails também é lido', () => {
    const e = paraApiError(http(400, { title: 'One or more validation errors occurred.' }));
    expect(e.descricao).toBe('One or more validation errors occurred.');
  });

  it('413 vira arquivo grande', () => {
    expect(paraApiError(http(413, null)).tipo).toBe('arquivo-grande');
  });

  it('503 traz as pendências da API', () => {
    const e = paraApiError(
      http(503, {
        erro: 'Não foi possível concluir a análise de todas as notas.',
        pendencias: [
          {
            numeroDocumento: 'X1',
            motivos: [{ codigo: 'SITUACAO_CADASTRAL_SACADO', mensagem: 'indisponível' }],
          },
        ],
      }),
    );
    expect(e.tipo).toBe('verificacao-indisponivel');
    expect(e.pendencias).toHaveLength(1);
    expect(e.temporario).toBe(true);
  });

  it('500 de configuração de regras é identificado', () => {
    const e = paraApiError(
      http(500, { detail: 'Configuração de regras inválida: Parâmetro x inválido.' }),
    );
    expect(e.tipo).toBe('configuracao');
  });

  it('outros 5xx viram erro de servidor', () => {
    expect(paraApiError(http(502, 'Bad gateway')).tipo).toBe('servidor');
  });

  it('mantém um ApiError recebido', () => {
    const original = new ApiError('tempo-esgotado', 0, 't', 'd');
    expect(paraApiError(original)).toBe(original);
  });
});

describe('explicarErroArquivo', () => {
  it('traduz erro de linha do backend', () => {
    expect(
      explicarErroArquivo('Linha 4: valor, data ou número de documento inválido.').descricao,
    ).toContain('linha 4');
  });

  it('mantém mensagens desconhecidas', () => {
    expect(explicarErroArquivo('Algo novo.').descricao).toBe('Algo novo.');
  });
});
