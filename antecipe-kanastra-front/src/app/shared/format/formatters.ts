// Formatação pt-BR centralizada. Só apresentação — nenhum cálculo financeiro.

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const inteiro = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 });
const dataCurta = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  timeZone: 'UTC',
});
const dataMedia = new Intl.DateTimeFormat('pt-BR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
});
const dataHora = new Intl.DateTimeFormat('pt-BR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});
const diaPorExtenso = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
});

export function formatarBrl(valor: number | null | undefined): string {
  return valor === null || valor === undefined ? '—' : brl.format(valor);
}

export interface PartesMoeda {
  sinal: string;
  simbolo: string;
  inteiro: string;
  centavos: string;
}

/** Divide um valor em partes para tipografia editorial: R$ · 1.234 · ,56 */
export function partesBrl(valor: number): PartesMoeda {
  const partes = brl.formatToParts(Math.abs(valor));
  const juntar = (tipos: string[]) =>
    partes
      .filter((p) => tipos.includes(p.type))
      .map((p) => p.value)
      .join('');
  return {
    sinal: valor < 0 ? '−' : '',
    simbolo: juntar(['currency']),
    inteiro: juntar(['integer', 'group']),
    centavos: juntar(['decimal', 'fraction']),
  };
}

export function formatarInteiro(valor: number): string {
  return inteiro.format(valor);
}

/** `fracao` em 0..1 → "12,5%". */
export function formatarPercentual(fracao: number, casas = 1): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'percent',
    minimumFractionDigits: casas,
    maximumFractionDigits: casas,
  }).format(fracao);
}

function isoParaData(iso: string): Date {
  return new Date(`${iso}T00:00:00Z`);
}

/** "2027-01-15" → "15/01/2027". */
export function formatarData(iso: string | null | undefined): string {
  return iso ? dataCurta.format(isoParaData(iso)) : '—';
}

/** "2027-01-15" → "15 de jan. de 2027". */
export function formatarDataMedia(iso: string | null | undefined): string {
  return iso ? dataMedia.format(isoParaData(iso)) : '—';
}

export function formatarDataHora(iso: string): string {
  return dataHora.format(new Date(iso));
}

export function formatarDiaPorExtenso(data: Date): string {
  const t = diaPorExtenso.format(data);
  return t.charAt(0).toUpperCase() + t.slice(1);
}

/** Formata 14 dígitos como CNPJ; mantém o texto original em qualquer outro caso. */
export function formatarCnpj(doc: string | null | undefined): string {
  if (!doc) return '—';
  const d = doc.replace(/\D/g, '');
  if (d.length !== 14) return doc;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

export function formatarTamanho(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024)
    return `${(bytes / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} KB`;
  return `${(bytes / 1024 / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB`;
}

export function plural(n: number, singular: string, pluralForma: string): string {
  return `${formatarInteiro(n)} ${n === 1 ? singular : pluralForma}`;
}

export function saudacao(agora = new Date()): string {
  const h = agora.getHours();
  if (h < 5) return 'Boa noite';
  if (h < 12) return 'Bom dia';
  if (h < 18) return 'Boa tarde';
  return 'Boa noite';
}
