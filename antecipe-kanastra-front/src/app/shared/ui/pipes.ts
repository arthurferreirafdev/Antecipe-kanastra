import { Pipe, PipeTransform } from '@angular/core';
import {
  formatarBrl,
  formatarCnpj,
  formatarData,
  formatarDataMedia,
  formatarInteiro,
  formatarPercentual,
} from '../format/formatters';

@Pipe({ name: 'brl' })
export class BrlPipe implements PipeTransform {
  transform(valor: number | null | undefined): string {
    return formatarBrl(valor);
  }
}

@Pipe({ name: 'dataBr' })
export class DataBrPipe implements PipeTransform {
  transform(iso: string | null | undefined, estilo: 'curta' | 'media' = 'curta'): string {
    return estilo === 'media' ? formatarDataMedia(iso) : formatarData(iso);
  }
}

@Pipe({ name: 'pct' })
export class PctPipe implements PipeTransform {
  transform(fracao: number, casas = 1): string {
    return formatarPercentual(fracao, casas);
  }
}

@Pipe({ name: 'cnpj' })
export class CnpjPipe implements PipeTransform {
  transform(doc: string | null | undefined): string {
    return formatarCnpj(doc);
  }
}

@Pipe({ name: 'inteiro' })
export class InteiroPipe implements PipeTransform {
  transform(n: number): string {
    return formatarInteiro(n);
  }
}
