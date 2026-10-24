import { COLUNAS_CSV, LeituraCsv, LinhaCsv, ProblemaCsv } from '../../models/csv.models';

// Espelha o formato aceito por Infrastructure/CsvRecebiveis.cs (TextFieldParser):
// separador ';', campos opcionalmente entre aspas, espaços nas pontas ignorados, linhas em
// branco ignoradas, cabeçalho exato, valor pt-BR sem separador de milhar, datas dd/MM/aaaa.
// Só valida FORMATO — elegibilidade é decidida exclusivamente pela API.

export const TAMANHO_MAXIMO_BYTES = 30 * 1024 * 1024; // limite padrão de corpo do Kestrel
const MAX_ERROS = 50;

interface RegistroBruto {
  linha: number;
  campos: string[];
}

/** Divide o texto em registros, respeitando aspas (inclusive quebras de linha dentro delas). */
export function dividirRegistros(texto: string, separador = ';'): RegistroBruto[] {
  const registros: RegistroBruto[] = [];
  let campos: string[] = [];
  let campo = '';
  let entreAspas = false;
  let linhaAtual = 1;
  let linhaInicio = 1;

  const fecharRegistro = () => {
    campos.push(campo);
    if (campos.length > 1 || campos[0]!.trim() !== '') {
      registros.push({ linha: linhaInicio, campos });
    }
    campos = [];
    campo = '';
  };

  for (let i = 0; i < texto.length; i++) {
    const ch = texto[i]!;
    if (entreAspas) {
      if (ch === '"') {
        if (texto[i + 1] === '"') {
          campo += '"';
          i++;
        } else {
          entreAspas = false;
        }
      } else {
        if (ch === '\n') linhaAtual++;
        campo += ch;
      }
      continue;
    }
    if (ch === '"' && campo.trim() === '') {
      entreAspas = true;
      campo = '';
    } else if (ch === separador) {
      campos.push(campo);
      campo = '';
    } else if (ch === '\n') {
      fecharRegistro();
      linhaAtual++;
      linhaInicio = linhaAtual;
    } else if (ch !== '\r') {
      campo += ch;
    }
  }
  if (campo !== '' || campos.length > 0) fecharRegistro();
  return registros;
}

/** Converte "1234,56" no número aceito pela API (pt-BR, sem milhar). `null` se fora do formato. */
export function lerValorPtBr(texto: string): number | null {
  const t = texto.trim();
  if (!/^[+-]?(\d+(,\d*)?|,\d+)$/.test(t)) return null;
  const n = Number(t.replace(',', '.'));
  return Number.isFinite(n) ? n : null;
}

/** Converte "dd/MM/aaaa" em "aaaa-mm-dd". `null` se o formato ou a data forem inválidos. */
export function lerDataBr(texto: string): string | null {
  const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto.trim());
  if (!m) return null;
  const [dd, mm, aaaa] = [m[1]!, m[2]!, m[3]!];
  const d = new Date(Date.UTC(+aaaa, +mm - 1, +dd));
  if (d.getUTCFullYear() !== +aaaa || d.getUTCMonth() !== +mm - 1 || d.getUTCDate() !== +dd)
    return null;
  return `${aaaa}-${mm}-${dd}`;
}

function explicarValorInvalido(texto: string): string {
  const t = texto.trim();
  if (t === '') return 'o valor nominal está vazio';
  if (/^\d{1,3}(\.\d{3})+(,\d+)?$/.test(t))
    return `remova o ponto de milhar em "${t}" (use ${t.replace(/\./g, '')})`;
  if (/^\d+\.\d{1,2}$/.test(t))
    return `use vírgula para os centavos em "${t}" (use ${t.replace('.', ',')})`;
  if (t.includes('R$')) return `escreva apenas o número, sem "R$", em "${t}"`;
  return `"${t}" não é um valor válido (exemplo: 1250,90)`;
}

function separadorErrado(primeiraLinha: string): string | null {
  if (primeiraLinha.includes(';')) return null;
  if (primeiraLinha.split(',').length >= COLUNAS_CSV.length) return 'vírgula';
  if (primeiraLinha.split('\t').length >= COLUNAS_CSV.length) return 'tabulação';
  return null;
}

/** Lê o texto completo de um CSV de recebíveis. */
export function lerCsvRecebiveis(textoOriginal: string): LeituraCsv {
  const problemas: ProblemaCsv[] = [];
  let erros = 0;
  const erro = (p: Omit<ProblemaCsv, 'severidade'>) => {
    erros++;
    if (erros <= MAX_ERROS) problemas.push({ severidade: 'erro', ...p });
  };
  const texto = textoOriginal.replace(/^﻿/, '');

  if (texto.trim() === '') {
    return {
      linhas: [],
      valido: false,
      problemas: [{ severidade: 'erro', mensagem: 'O arquivo está vazio.' }],
    };
  }

  const registros = dividirRegistros(texto);
  const nomes = registros[0]!.campos.map((c) => c.trim());
  const cabecalhoOk =
    nomes.length === COLUNAS_CSV.length && nomes.every((n, i) => n === COLUNAS_CSV[i]);

  if (!cabecalhoOk) {
    const separador = separadorErrado(texto.split('\n')[0] ?? '');
    if (separador) {
      erro({
        linha: 1,
        mensagem: `O arquivo usa ${separador} para separar as colunas. Salve o CSV com ponto e vírgula (;).`,
      });
    } else {
      const conhecidas: readonly string[] = COLUNAS_CSV;
      const faltando = COLUNAS_CSV.filter((c) => !nomes.includes(c));
      const sobrando = nomes.filter((n) => !conhecidas.includes(n));
      let detalhe = 'As colunas estão fora da ordem esperada.';
      if (faltando.length) detalhe = `Faltam as colunas ${faltando.join(', ')}.`;
      else if (sobrando.length) detalhe = `Há colunas não reconhecidas: ${sobrando.join(', ')}.`;
      erro({ linha: 1, mensagem: `A primeira linha não tem o cabeçalho esperado. ${detalhe}` });
    }
    return { linhas: [], problemas, valido: false };
  }

  const linhas: LinhaCsv[] = [];
  for (const { linha, campos } of registros.slice(1)) {
    const f = campos.map((c) => c.trim());
    if (f.length !== COLUNAS_CSV.length) {
      erro({
        linha,
        mensagem: `Linha ${linha}: tem ${f.length} colunas, mas são esperadas ${COLUNAS_CSV.length}.`,
      });
      continue;
    }
    const [
      nomeFundo,
      docFundo,
      nomeCedente,
      docCedente,
      nomeSacado,
      docSacado,
      numeroDocumento,
      tipoRecebivel,
      valorTxt,
      vencTxt,
      aqTxt,
    ] = f as [
      string,
      string,
      string,
      string,
      string,
      string,
      string,
      string,
      string,
      string,
      string,
    ];

    const valor = lerValorPtBr(valorTxt);
    const dataVencimento = lerDataBr(vencTxt);
    const dataAquisicao = lerDataBr(aqTxt);

    if (valor === null) {
      erro({
        linha,
        coluna: 'VALOR_NOMINAL',
        mensagem: `Linha ${linha}: ${explicarValorInvalido(valorTxt)}.`,
      });
    } else if (valor <= 0) {
      erro({
        linha,
        coluna: 'VALOR_NOMINAL',
        mensagem: `Linha ${linha}: o valor nominal precisa ser maior que zero.`,
      });
    }
    if (dataVencimento === null) {
      erro({
        linha,
        coluna: 'DATA_VENCIMENTO_ORIGINAL',
        mensagem: `Linha ${linha}: a data de vencimento "${vencTxt}" deve estar no formato dd/mm/aaaa.`,
      });
    }
    if (dataAquisicao === null) {
      erro({
        linha,
        coluna: 'DATA_AQUISICAO',
        mensagem: `Linha ${linha}: a data de aquisição "${aqTxt}" deve estar no formato dd/mm/aaaa.`,
      });
    }
    if (numeroDocumento === '') {
      erro({
        linha,
        coluna: 'NU_DOCUMENTO',
        mensagem: `Linha ${linha}: o número do documento está vazio.`,
      });
    }

    linhas.push({
      linha,
      nomeFundo,
      docFundo,
      nomeCedente,
      docCedente,
      nomeSacado,
      docSacado,
      numeroDocumento,
      tipoRecebivel,
      valorNominal: valor !== null && valor > 0 ? valor : null,
      dataVencimento,
      dataAquisicao,
    });
  }

  if (erros > MAX_ERROS) {
    problemas.push({
      severidade: 'erro',
      mensagem: `E mais ${erros - MAX_ERROS} problemas semelhantes.`,
    });
  }
  if (linhas.length === 0 && erros === 0) {
    erro({ mensagem: 'O arquivo tem apenas o cabeçalho — nenhuma nota foi encontrada.' });
  }

  return { linhas, problemas, valido: erros === 0 };
}

/** Problemas que impedem até a leitura do arquivo (tipo, tamanho). */
export function verificarArquivo(arquivo: File): ProblemaCsv | null {
  const nomeOk = /\.csv$/i.test(arquivo.name);
  const tipoOk = arquivo.type === '' || /csv|text\/plain|excel/i.test(arquivo.type);
  if (!nomeOk || !tipoOk) {
    return {
      severidade: 'erro',
      mensagem: `"${arquivo.name}" não é um arquivo CSV. Exporte a planilha como CSV separado por ponto e vírgula.`,
    };
  }
  if (arquivo.size === 0) return { severidade: 'erro', mensagem: 'O arquivo está vazio.' };
  if (arquivo.size > TAMANHO_MAXIMO_BYTES) {
    return {
      severidade: 'erro',
      mensagem:
        'O arquivo passa de 30 MB, o limite aceito pelo servidor. Divida as notas em mais de um arquivo.',
    };
  }
  return null;
}

/** Decodifica como UTF-8; se o arquivo não for UTF-8, decodifica como Windows-1252 e avisa. */
export function decodificar(bytes: ArrayBuffer): { texto: string; aviso: ProblemaCsv | null } {
  try {
    return { texto: new TextDecoder('utf-8', { fatal: true }).decode(bytes), aviso: null };
  } catch {
    return {
      texto: new TextDecoder('windows-1252').decode(bytes),
      aviso: {
        severidade: 'aviso',
        mensagem:
          'O arquivo não está em UTF-8. Ele pode ser enviado, mas nomes com acento podem aparecer trocados. Para evitar, salve como "CSV UTF-8".',
      },
    };
  }
}
