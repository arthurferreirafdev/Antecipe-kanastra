import { LinhaCsv } from '../../models/csv.models';
import { ResultadoLote } from '../../models/api.models';
import { combinarNotas, resumirAnalise } from './montar-analise';

const linha = (n: number, doc: string): LinhaCsv => ({
  linha: n,
  nomeFundo: 'F',
  docFundo: '1',
  nomeCedente: 'C',
  docCedente: '2',
  nomeSacado: `Sacado ${doc}`,
  docSacado: '3',
  numeroDocumento: doc,
  tipoRecebivel: 'Duplicata',
  valorNominal: 0,
  dataVencimento: '2027-01-15',
  dataAquisicao: '2026-10-24',
});

const resultado: ResultadoLote = {
  totalRecebiveis: 3,
  quantidadeAprovada: 2,
  quantidadeRecusada: 1,
  quantidadePendente: 0,
  valorTotalLiquido: 289.4,
  resultados: [
    {
      numeroDocumento: 'A',
      status: 'APROVADA',
      valorNominal: 100.1,
      valorLiquido: 94.47,
      motivos: [],
    },
    {
      numeroDocumento: 'B',
      status: 'RECUSADA',
      valorNominal: 50,
      valorLiquido: null,
      motivos: [{ codigo: 'VALOR_MINIMO', mensagem: 'x' }],
    },
    {
      numeroDocumento: 'C',
      status: 'APROVADA',
      valorNominal: 200.2,
      valorLiquido: 194.93,
      motivos: [],
    },
  ],
};

describe('combinarNotas', () => {
  it('associa cada resultado à linha do CSV na mesma posição', () => {
    const notas = combinarNotas(resultado, [linha(2, 'A'), linha(3, 'B'), linha(4, 'C')]);
    expect(notas.map((n) => n.csv?.nomeSacado)).toEqual(['Sacado A', 'Sacado B', 'Sacado C']);
  });

  it('não associa dados quando o número do documento diverge', () => {
    const notas = combinarNotas(resultado, [linha(2, 'A'), linha(3, 'X'), linha(4, 'C')]);
    expect(notas[1]!.csv).toBeNull();
    expect(notas[0]!.csv).not.toBeNull();
  });

  it('não associa nada quando a quantidade de linhas difere', () => {
    expect(combinarNotas(resultado, [linha(2, 'A')]).every((n) => n.csv === null)).toBe(true);
    expect(combinarNotas(resultado, null).every((n) => n.csv === null)).toBe(true);
  });

  it('calcula o desconto só das aprovadas, a partir dos valores da API', () => {
    const notas = combinarNotas(resultado, null);
    expect(notas[0]!.desconto).toBe(5.63);
    expect(notas[1]!.desconto).toBeNull();
  });
});

describe('resumirAnalise', () => {
  it('agrega totais sem erro de ponto flutuante', () => {
    const r = resumirAnalise(resultado, combinarNotas(resultado, null));
    expect(r.valorNominalEnviado).toBe(350.3);
    expect(r.valorNominalElegivel).toBe(300.3);
    expect(r.valorLiquido).toBe(289.4);
    expect(r.desconto).toBe(10.9);
    expect(r.percentualElegibilidade).toBeCloseTo(2 / 3);
    expect(r.motivos).toEqual([{ codigo: 'VALOR_MINIMO', quantidade: 1, valorNominal: 50 }]);
  });
});
