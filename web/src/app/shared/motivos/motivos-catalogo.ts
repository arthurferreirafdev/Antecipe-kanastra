import {
  LucideBadgeCheck,
  LucideBan,
  LucideCalendarClock,
  LucideChartPie,
  LucideCircleAlert,
  LucideClock,
  LucideCoins,
  LucideCopy,
  LucideFileText,
  LucideHourglass,
  LucideIconInput,
  LucideLandmark,
  LucideScale,
} from '@lucide/angular';
import { Motivo } from '../../models/api.models';
import { NotaAnalisada } from '../../models/analise.models';
import { formatarBrl, formatarCnpj } from '../format/formatters';

// Tradução dos códigos de regra (validators.json / MotorRegras) para linguagem simples.
// Os números exibidos em "detalhes" são extraídos da própria mensagem da API ou dos dados da
// nota — nada aqui inventa limites ou recalcula regras.

export interface DetalheMotivo {
  rotulo: string;
  valor: string;
  /** Destaca o valor que não atendeu à regra. */
  destaque?: boolean;
}

export interface MotivoExplicado {
  codigo: string;
  titulo: string;
  explicacao: string;
  orientacao: string;
  icone: LucideIconInput;
  detalhes: DetalheMotivo[];
  /** Mensagem original devolvida pela API. */
  mensagemTecnica: string;
}

interface EntradaCatalogo {
  titulo: string;
  explicacao: string;
  orientacao: string;
  icone: LucideIconInput;
  detalhes?: (mensagem: string, nota: NotaAnalisada | null) => DetalheMotivo[];
}

const CATALOGO: Record<string, EntradaCatalogo> = {
  PRAZO_MAXIMO: {
    titulo: 'Prazo acima do permitido',
    explicacao:
      'O prazo desta nota ultrapassa o limite permitido pelo fundo. Quanto mais distante o vencimento, maior o risco — por isso o fundo só compra notas que vencem dentro de um período definido.',
    orientacao:
      'Esta nota poderá ser antecipada mais perto do vencimento, quando o prazo restante estiver dentro do limite.',
    icone: LucideCalendarClock,
    detalhes: (m) => {
      const r = /Prazo de (-?\d+) dias excede o máximo de (\d+) dias/.exec(m);
      return r
        ? [
            { rotulo: 'Prazo identificado', valor: `${r[1]} dias`, destaque: true },
            { rotulo: 'Limite do fundo', valor: `até ${r[2]} dias` },
          ]
        : [];
    },
  },
  PRAZO_MINIMO: {
    titulo: 'Prazo abaixo do mínimo',
    explicacao:
      'Esta nota vence cedo demais. O fundo só antecipa notas com um prazo mínimo até o vencimento.',
    orientacao:
      'Para notas tão próximas do vencimento, normalmente compensa aguardar o pagamento do cliente.',
    icone: LucideHourglass,
    detalhes: (m) => {
      const r = /Prazo de (-?\d+) dias abaixo do mínimo de (\d+) dias/.exec(m);
      return r
        ? [
            { rotulo: 'Prazo identificado', valor: `${r[1]} dias`, destaque: true },
            { rotulo: 'Mínimo do fundo', valor: `a partir de ${r[2]} dias` },
          ]
        : [];
    },
  },
  VALOR_MINIMO: {
    titulo: 'Valor abaixo do mínimo',
    explicacao:
      'O fundo só compra notas a partir de um valor mínimo, para que os custos da operação façam sentido para você.',
    orientacao:
      'Confira se o valor está correto no arquivo. Notas abaixo do mínimo não podem ser antecipadas.',
    icone: LucideCoins,
    detalhes: (m, nota) => {
      const r = /mínimo de (R\$\s?[\d.,]+)/.exec(m);
      const itens: DetalheMotivo[] = [];
      if (nota)
        itens.push({
          rotulo: 'Valor da nota',
          valor: formatarBrl(nota.valorNominal),
          destaque: true,
        });
      if (r) itens.push({ rotulo: 'Mínimo do fundo', valor: r[1]!.replace(/\s/, ' ') });
      return itens;
    },
  },
  NAO_VENCIDA: {
    titulo: 'Nota já vencida',
    explicacao:
      'A data de vencimento desta nota já passou. Uma nota vencida não pode ser antecipada — o valor deve ser cobrado diretamente do cliente.',
    orientacao:
      'Confira a data de vencimento. Se ela estiver errada, corrija o arquivo e envie novamente.',
    icone: LucideClock,
    detalhes: (m) => {
      const r =
        /venceu em (\d{2}\/\d{2}\/\d{4}), antes da data (?:da análise|de aquisição) \((\d{2}\/\d{2}\/\d{4})\)/.exec(
          m,
        );
      return r
        ? [
            { rotulo: 'Vencimento', valor: r[1]!, destaque: true },
            { rotulo: 'Data da análise', valor: r[2]! },
          ]
        : [];
    },
  },
  TIPO_RECEBIVEL: {
    titulo: 'Tipo de título não aceito',
    explicacao:
      'O fundo só compra alguns tipos de recebíveis. O tipo informado nesta nota não está entre eles.',
    orientacao:
      'Somente os tipos aceitos podem ser antecipados. Se o tipo foi escrito de outra forma no arquivo, ajuste e envie de novo.',
    icone: LucideFileText,
    detalhes: (m, nota) => {
      const r = /Tipos aceitos: (.+?)\.?$/.exec(m);
      const itens: DetalheMotivo[] = [];
      if (nota?.csv)
        itens.push({
          rotulo: 'Tipo informado',
          valor: nota.csv.tipoRecebivel || '—',
          destaque: true,
        });
      if (r) itens.push({ rotulo: 'Tipos aceitos', valor: r[1]! });
      return itens;
    },
  },
  SITUACAO_CADASTRAL_SACADO: {
    titulo: 'Cliente com cadastro irregular',
    explicacao:
      'A empresa que vai pagar esta nota (o sacado) não está com a situação cadastral regular. O fundo só compra notas de pagadores regulares.',
    orientacao:
      'Peça ao seu cliente que regularize o cadastro na Receita Federal. Depois disso, envie a nota novamente.',
    icone: LucideBadgeCheck,
    detalhes: (m) => {
      const r = /Situação cadastral do sacado: (.+?)\. Aceitas: (.+?)\.?$/.exec(m);
      return r
        ? [
            { rotulo: 'Situação encontrada', valor: r[1]!, destaque: true },
            { rotulo: 'Situações aceitas', valor: r[2]! },
          ]
        : [];
    },
  },
  CNPJ_SACADO_VALIDO: {
    titulo: 'CNPJ do cliente inválido',
    explicacao:
      'O CNPJ de quem vai pagar a nota não passou na verificação dos dígitos. Normalmente é um erro de digitação.',
    orientacao: 'Confira o CNPJ na coluna DOC_SACADO e envie o arquivo corrigido.',
    icone: LucideCircleAlert,
    detalhes: (_m, nota) =>
      nota?.csv
        ? [{ rotulo: 'CNPJ informado', valor: nota.csv.docSacado || '—', destaque: true }]
        : [],
  },
  CONCENTRACAO_MAXIMA_SACADO: {
    titulo: 'Limite por cliente atingido',
    explicacao:
      'Para equilibrar o risco, o fundo limita quanto da carteira pode depender de um mesmo pagador. Comprar esta nota deixaria esse cliente acima do limite.',
    orientacao:
      'Não é um erro na nota: depende da carteira do fundo. Outras notas do mesmo cliente neste arquivo podem ter sido aprovadas antes desta. Tente novamente mais tarde ou priorize notas de outros clientes.',
    icone: LucideChartPie,
    detalhes: (m) => detalhesConcentracao(m),
  },
  CONCENTRACAO_MAXIMA_CEDENTE: {
    titulo: 'Limite por empresa cedente atingido',
    explicacao:
      'O fundo limita quanto da carteira pode vir de uma mesma empresa vendedora. Comprar esta nota deixaria a sua empresa acima desse limite.',
    orientacao:
      'Não é um erro na nota: depende da carteira do fundo. Envie esta nota em outra oportunidade, quando houver espaço na carteira.',
    icone: LucideScale,
    detalhes: (m) => detalhesConcentracao(m),
  },
  FUNDO_INCORRETO: {
    titulo: 'Nota de outro fundo',
    explicacao: 'Esta nota está destinada a um fundo diferente do que fez a análise.',
    orientacao:
      'Confira as colunas NOME_FUNDO e DOC_FUNDO do arquivo e envie a nota para o fundo correto.',
    icone: LucideLandmark,
    detalhes: (_m, nota) =>
      nota?.csv
        ? [
            {
              rotulo: 'Fundo informado',
              valor: `${nota.csv.nomeFundo} · ${formatarCnpj(nota.csv.docFundo)}`,
              destaque: true,
            },
          ]
        : [],
  },
  RECEBIVEL_DUPLICADO: {
    titulo: 'Nota já enviada',
    explicacao:
      'Esta nota já faz parte da carteira do fundo ou aparece mais de uma vez neste arquivo. Uma mesma nota só pode ser antecipada uma vez.',
    orientacao:
      'Remova a linha repetida do arquivo. Se a nota já foi antecipada, não é preciso enviá-la de novo.',
    icone: LucideCopy,
    detalhes: (_m, nota) =>
      nota?.csv
        ? [
            { rotulo: 'Número do documento', valor: nota.csv.numeroDocumento, destaque: true },
            { rotulo: 'Identificação usada', valor: 'Fundo + cedente + número do documento + vencimento' },
          ]
        : [],
  },
  FORMATO_INVALIDO: {
    titulo: 'Dados da nota em formato inválido',
    explicacao:
      'Algumas informações desta linha do arquivo não puderam ser lidas, por isso a nota não passou pelas regras do fundo.',
    orientacao: 'Corrija o campo indicado nesta linha do arquivo e envie o CSV novamente.',
    icone: LucideCircleAlert,
  },
  PRECIFICACAO_INVALIDA: {
    titulo: 'Não foi possível calcular o valor',
    explicacao: 'O fundo não conseguiu precificar esta nota com os parâmetros atuais.',
    orientacao: 'Tente novamente mais tarde. Se o problema continuar, fale com a gestão do fundo.',
    icone: LucideBan,
  },
};

function detalhesConcentracao(m: string): DetalheMotivo[] {
  const r = /Concentração projetada do (?:sacado|cedente): ([\d.,]+)% \(limite ([\d.,]+)%\)/.exec(
    m,
  );
  return r
    ? [
        { rotulo: 'Participação após a compra', valor: `${r[1]}%`, destaque: true },
        { rotulo: 'Limite do fundo', valor: `até ${r[2]}%` },
      ]
    : [];
}

/** Explica um motivo devolvido pela API. `pendente` indica nota com status PENDENTE_VALIDACAO. */
export function explicarMotivo(
  motivo: Motivo,
  nota: NotaAnalisada | null = null,
  pendente = false,
): MotivoExplicado {
  const entrada = CATALOGO[motivo.codigo];
  if (pendente) {
    return {
      codigo: motivo.codigo,
      titulo: 'Verificação não concluída',
      explicacao:
        'Uma das verificações não pôde ser feita agora, por isso a nota não foi aprovada nem recusada.',
      orientacao: 'Tente analisar novamente em alguns minutos.',
      icone: LucideHourglass,
      detalhes: [{ rotulo: 'Verificação', valor: entrada?.titulo ?? motivo.codigo }],
      mensagemTecnica: motivo.mensagem,
    };
  }
  if (!entrada) {
    return {
      codigo: motivo.codigo,
      titulo: 'Regra do fundo não atendida',
      explicacao: motivo.mensagem,
      orientacao: 'Revise os dados desta nota. Se tiver dúvidas, fale com a gestão do fundo.',
      icone: LucideCircleAlert,
      detalhes: [],
      mensagemTecnica: motivo.mensagem,
    };
  }
  return {
    codigo: motivo.codigo,
    titulo: entrada.titulo,
    explicacao: entrada.explicacao,
    orientacao: entrada.orientacao,
    icone: entrada.icone,
    detalhes: entrada.detalhes?.(motivo.mensagem, nota) ?? [],
    mensagemTecnica: motivo.mensagem,
  };
}

export function tituloMotivo(codigo: string): string {
  return CATALOGO[codigo]?.titulo ?? 'Regra do fundo não atendida';
}

export function iconeMotivo(codigo: string): LucideIconInput {
  return CATALOGO[codigo]?.icone ?? LucideCircleAlert;
}
