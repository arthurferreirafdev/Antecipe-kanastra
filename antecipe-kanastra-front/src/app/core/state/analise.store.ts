import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AnaliseSalva } from '../../models/analise.models';
import { LeituraCsv, LinhaCsv, ProblemaCsv } from '../../models/csv.models';
import { combinarNotas, resumirAnalise } from '../../shared/analise/montar-analise';
import { ApiError, paraApiError } from '../api/api-error';
import { NotasApiService } from '../api/notas-api.service';

const CHAVE = 'antecipe:ultima-analise:v1';

export type EstadoEnvio =
  | { fase: 'ocioso' }
  | { fase: 'enviando'; arquivoNome: string; quantidade: number }
  | { fase: 'erro'; erro: ApiError };

/** Arquivo escolhido e ainda não analisado, mantido durante a navegação. */
export interface Rascunho {
  arquivo: File;
  leitura: LeituraCsv;
  avisoCodificacao: ProblemaCsv | null;
}

function carregar(): AnaliseSalva | null {
  try {
    const bruto = localStorage.getItem(CHAVE);
    if (!bruto) return null;
    const a = JSON.parse(bruto) as Partial<AnaliseSalva>;
    const ok =
      a.versao === 1 && typeof a.arquivoNome === 'string' && Array.isArray(a.resultado?.resultados);
    return ok ? (a as AnaliseSalva) : null;
  } catch {
    return null;
  }
}

/** Grava a análise; se não couber, tenta sem as linhas do CSV. Retorna se algo foi salvo. */
function salvar(a: AnaliseSalva | null): boolean {
  try {
    if (a === null) {
      localStorage.removeItem(CHAVE);
      return true;
    }
    try {
      localStorage.setItem(CHAVE, JSON.stringify(a));
    } catch {
      localStorage.setItem(CHAVE, JSON.stringify({ ...a, linhas: null }));
    }
    return true;
  } catch {
    return false;
  }
}

/**
 * Estado da antecipação. Vive no root para que a análise em andamento e o último resultado
 * sobrevivam à navegação; a última análise também é guardada no navegador.
 */
@Injectable({ providedIn: 'root' })
export class AnaliseStore {
  private readonly api = inject(NotasApiService);

  readonly analise = signal<AnaliseSalva | null>(carregar());
  readonly envio = signal<EstadoEnvio>({ fase: 'ocioso' });
  readonly rascunho = signal<Rascunho | null>(null);
  /** `false` quando o navegador não permitiu guardar a análise. */
  readonly guardadaNoNavegador = signal(true);

  readonly enviando = computed(() => this.envio().fase === 'enviando');
  readonly erro = computed(() => {
    const e = this.envio();
    return e.fase === 'erro' ? e.erro : null;
  });
  readonly notas = computed(() => {
    const a = this.analise();
    return a ? combinarNotas(a.resultado, a.linhas) : [];
  });
  readonly resumo = computed(() => {
    const a = this.analise();
    return a ? resumirAnalise(a.resultado, this.notas()) : null;
  });
  /** Nome do fundo informado no arquivo (a API não expõe o fundo configurado). */
  readonly nomeFundo = computed(() => this.analise()?.linhas?.[0]?.nomeFundo ?? null);

  /**
   * Envia o arquivo para análise. Ignora chamadas enquanto outra análise está em andamento,
   * evitando duplo envio. Resolve `true` em caso de sucesso.
   */
  async analisar(arquivo: File, linhas: LinhaCsv[] | null): Promise<boolean> {
    if (this.enviando()) return false;
    this.envio.set({
      fase: 'enviando',
      arquivoNome: arquivo.name,
      quantidade: linhas?.length ?? 0,
    });
    try {
      const resultado = await firstValueFrom(this.api.analisar(arquivo));
      const analise: AnaliseSalva = {
        versao: 1,
        arquivoNome: arquivo.name,
        arquivoTamanho: arquivo.size,
        analisadaEm: new Date().toISOString(),
        resultado,
        linhas,
      };
      this.analise.set(analise);
      this.guardadaNoNavegador.set(salvar(analise));
      this.rascunho.set(null);
      this.envio.set({ fase: 'ocioso' });
      return true;
    } catch (e) {
      this.envio.set({ fase: 'erro', erro: paraApiError(e) });
      return false;
    }
  }

  limparErro(): void {
    if (this.envio().fase === 'erro') this.envio.set({ fase: 'ocioso' });
  }

  descartarAnalise(): void {
    this.analise.set(null);
    salvar(null);
  }
}
