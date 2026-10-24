import { LinhaCsv } from '../../models/csv.models';
import { ResultadoLote } from '../../models/api.models';
import { MotivoAgrupado, NotaAnalisada, ResumoAnalise } from '../../models/analise.models';

// Combina a resposta da API com as linhas do CSV e agrega totais para exibição.
// O valor líquido de cada nota e o total líquido vêm prontos da API; aqui só somamos e
// subtraímos valores já calculados pelo backend (em centavos, para não acumular erro de float).

const centavos = (v: number) => Math.round(v * 100);
const reais = (c: number) => c / 100;

/**
 * Cada resultado traz a linha do CSV de origem; sem ela, recorremos à ordem (a linha i do arquivo
 * corresponde ao resultado i), o que só vale quando nenhuma linha foi removida, como duplicadas.
 * Conferimos o número do documento para não associar dados errados caso a leitura local divirja
 * da do servidor.
 */
export function combinarNotas(
  resultado: ResultadoLote,
  linhas: LinhaCsv[] | null,
): NotaAnalisada[] {
  const alinhado = linhas !== null && linhas.length === resultado.resultados.length;
  const porLinha = new Map((linhas ?? []).map((l) => [l.linha, l]));
  return resultado.resultados.map((r, indice) => {
    const linha = r.linha != null ? porLinha.get(r.linha) : alinhado ? linhas[indice] : undefined;
    const csv = linha && linha.numeroDocumento === r.numeroDocumento.trim() ? linha : null;
    const desconto =
      r.status === 'APROVADA' && r.valorLiquido !== null
        ? reais(centavos(r.valorNominal) - centavos(r.valorLiquido))
        : null;
    return {
      indice,
      numeroDocumento: r.numeroDocumento,
      status: r.status,
      valorNominal: r.valorNominal,
      valorLiquido: r.valorLiquido,
      desconto,
      motivos: r.motivos,
      csv,
    };
  });
}

export function agruparMotivos(notas: NotaAnalisada[]): MotivoAgrupado[] {
  const mapa = new Map<string, { quantidade: number; centavos: number }>();
  for (const nota of notas) {
    if (nota.status === 'APROVADA') continue;
    const codigos = new Set(nota.motivos.map((m) => m.codigo));
    for (const codigo of codigos) {
      const atual = mapa.get(codigo) ?? { quantidade: 0, centavos: 0 };
      atual.quantidade++;
      atual.centavos += centavos(nota.valorNominal);
      mapa.set(codigo, atual);
    }
  }
  return [...mapa.entries()]
    .map(([codigo, v]) => ({ codigo, quantidade: v.quantidade, valorNominal: reais(v.centavos) }))
    .sort((a, b) => b.quantidade - a.quantidade || b.valorNominal - a.valorNominal);
}

export function resumirAnalise(resultado: ResultadoLote, notas: NotaAnalisada[]): ResumoAnalise {
  let enviado = 0;
  let elegivel = 0;
  for (const n of notas) {
    enviado += centavos(n.valorNominal);
    if (n.status === 'APROVADA') elegivel += centavos(n.valorNominal);
  }
  const liquido = centavos(resultado.valorTotalLiquido);
  const desconto = elegivel - liquido;
  const total = resultado.totalRecebiveis;
  return {
    total,
    aprovadas: resultado.quantidadeAprovada,
    recusadas: resultado.quantidadeRecusada,
    pendentes: resultado.quantidadePendente,
    valorNominalEnviado: reais(enviado),
    valorNominalElegivel: reais(elegivel),
    valorLiquido: reais(liquido),
    desconto: reais(desconto),
    percentualElegibilidade: total > 0 ? resultado.quantidadeAprovada / total : 0,
    descontoEfetivo: elegivel > 0 ? desconto / elegivel : 0,
    motivos: agruparMotivos(notas),
  };
}
