import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError, timeout } from 'rxjs';
import { ResultadoLote } from '../../models/api.models';
import { API_BASE_URL, TEMPO_LIMITE_ANALISE_MS } from './api-config';
import { ApiError } from './api-error';

/** Cliente de NotasController (`api/notas`). */
@Injectable({ providedIn: 'root' })
export class NotasApiService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL);

  /**
   * POST /api/notas/analisar — multipart/form-data, campo `arquivo`.
   * Simula a antecipação: o backend não adiciona as notas ao estoque.
   */
  analisar(arquivo: File): Observable<ResultadoLote> {
    const form = new FormData();
    form.append('arquivo', arquivo, arquivo.name);
    return this.http.post<ResultadoLote>(`${this.base}/api/notas/analisar`, form).pipe(
      timeout({
        first: TEMPO_LIMITE_ANALISE_MS,
        with: () =>
          throwError(
            () =>
              new ApiError(
                'tempo-esgotado',
                0,
                'A análise está demorando mais que o normal',
                'O servidor não respondeu a tempo. Tente novamente; se o arquivo for muito grande, divida-o em partes.',
              ),
          ),
      }),
    );
  }
}
