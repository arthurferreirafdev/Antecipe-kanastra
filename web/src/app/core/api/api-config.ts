import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/** URL base da API (sem barra final). Vazio = mesma origem do front, via proxy. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl.replace(/\/+$/, ''),
});

/** Tempo máximo de espera de uma análise antes de mostrar erro ao usuário. */
export const TEMPO_LIMITE_ANALISE_MS = 120_000;
