import {
  dividirRegistros,
  lerCsvRecebiveis,
  lerDataBr,
  lerValorPtBr,
  verificarArquivo,
  decodificar,
} from './csv-recebiveis';

const CAB =
  'NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO';
const linha = (valor = '1250,90', venc = '15/01/2027', doc = 'NF-1') =>
  `FUNDO;92.234.400/0001-80;Cedente;11.111.111/0001-91;Sacado;58.206.002/0001-11;${doc};Duplicata;${valor};${venc};24/10/2026`;

describe('lerValorPtBr', () => {
  it('aceita o formato da API (vírgula decimal, sem milhar)', () => {
    expect(lerValorPtBr('1250,90')).toBe(1250.9);
    expect(lerValorPtBr('100')).toBe(100);
    expect(lerValorPtBr(' 45,9 ')).toBe(45.9);
  });

  it('rejeita separador de milhar e ponto decimal, como o backend', () => {
    expect(lerValorPtBr('1.250,90')).toBeNull();
    expect(lerValorPtBr('1250.90')).toBeNull();
    expect(lerValorPtBr('R$ 10,00')).toBeNull();
    expect(lerValorPtBr('')).toBeNull();
  });
});

describe('lerDataBr', () => {
  it('converte dd/MM/aaaa para ISO', () => {
    expect(lerDataBr('15/01/2027')).toBe('2027-01-15');
  });

  it('rejeita datas inexistentes ou em outro formato', () => {
    expect(lerDataBr('31/02/2027')).toBeNull();
    expect(lerDataBr('2027-01-15')).toBeNull();
    expect(lerDataBr('1/1/2027')).toBeNull();
  });
});

describe('dividirRegistros', () => {
  it('respeita aspas, aspas escapadas e ignora linhas em branco', () => {
    const r = dividirRegistros('a;"b;c";"d ""e"""\r\n\r\nf;g;h\n');
    expect(r).toEqual([
      { linha: 1, campos: ['a', 'b;c', 'd "e"'] },
      { linha: 3, campos: ['f', 'g', 'h'] },
    ]);
  });
});

describe('lerCsvRecebiveis', () => {
  it('lê um arquivo válido, ignorando BOM', () => {
    const r = lerCsvRecebiveis(`﻿${CAB}\n${linha()}\n${linha('100', '01/03/2027', 'NF-2')}\n`);
    expect(r.valido).toBe(true);
    expect(r.linhas).toHaveLength(2);
    expect(r.linhas[0]).toMatchObject({
      linha: 2,
      numeroDocumento: 'NF-1',
      valorNominal: 1250.9,
      dataVencimento: '2027-01-15',
    });
  });

  it('explica cabeçalho com colunas faltando', () => {
    const r = lerCsvRecebiveis('NOME_FUNDO;DOC_FUNDO\nx;y');
    expect(r.valido).toBe(false);
    expect(r.problemas[0]!.mensagem).toContain('Faltam as colunas');
  });

  it('detecta separador vírgula', () => {
    const r = lerCsvRecebiveis(CAB.replace(/;/g, ',') + '\n');
    expect(r.problemas[0]!.mensagem).toContain('vírgula');
  });

  it('aponta a linha exata do valor com ponto de milhar', () => {
    const r = lerCsvRecebiveis(`${CAB}\n${linha()}\n${linha('4.250,00')}`);
    expect(r.valido).toBe(false);
    expect(r.problemas[0]).toMatchObject({ linha: 3, coluna: 'VALOR_NOMINAL' });
    expect(r.problemas[0]!.mensagem).toContain('use 4250,00');
  });

  it('aponta colunas a menos e data inválida', () => {
    const r = lerCsvRecebiveis(`${CAB}\na;b;c\n${linha('10,00', '32/01/2027')}`);
    expect(r.problemas.map((p) => p.linha)).toEqual([2, 3]);
  });

  it('recusa arquivo só com cabeçalho', () => {
    expect(lerCsvRecebiveis(CAB).problemas[0]!.mensagem).toContain('apenas o cabeçalho');
  });

  it('recusa arquivo vazio', () => {
    expect(lerCsvRecebiveis('  \n').valido).toBe(false);
  });
});

describe('verificarArquivo', () => {
  it('recusa extensão diferente de .csv', () => {
    expect(verificarArquivo(new File(['x'], 'notas.xlsx'))?.mensagem).toContain(
      'não é um arquivo CSV',
    );
  });

  it('recusa arquivo vazio', () => {
    expect(verificarArquivo(new File([], 'notas.csv'))?.mensagem).toContain('vazio');
  });

  it('aceita .csv com conteúdo', () => {
    expect(verificarArquivo(new File(['a'], 'notas.csv', { type: 'text/csv' }))).toBeNull();
  });
});

describe('decodificar', () => {
  it('avisa quando o arquivo não está em UTF-8', () => {
    const latin1 = new Uint8Array([0x4d, 0xf3, 0x76, 0x65, 0x69, 0x73]); // "Móveis" em Windows-1252
    const r = decodificar(latin1.buffer);
    expect(r.texto).toBe('Móveis');
    expect(r.aviso?.severidade).toBe('aviso');
  });

  it('não avisa para UTF-8', () => {
    expect(decodificar(new TextEncoder().encode('Móveis').buffer as ArrayBuffer).aviso).toBeNull();
  });
});
