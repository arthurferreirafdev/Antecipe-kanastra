import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { paraApiError } from './api-error';

/** Converte qualquer falha HTTP da API em `ApiError`, com mensagem pronta para a interface. */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const base = inject(API_BASE_URL);
  if (!req.url.startsWith(`${base}/api/`)) return next(req);
  return next(req).pipe(catchError((erro: unknown) => throwError(() => paraApiError(erro))));
};
