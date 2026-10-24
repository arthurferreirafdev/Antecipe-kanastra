import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { ResultadoLote } from '../../models/api.models';
import { apiErrorInterceptor } from './api-error.interceptor';
import { API_BASE_URL } from './api-config';
import { ApiError } from './api-error';
import { NotasApiService } from './notas-api.service';

describe('NotasApiService', () => {
  let api: NotasApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.local' },
      ],
    });
    api = TestBed.inject(NotasApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('envia o arquivo em multipart no campo "arquivo" para POST /api/notas/analisar', async () => {
    const arquivo = new File(['conteudo'], 'notas.csv', { type: 'text/csv' });
    const resposta = firstValueFrom(api.analisar(arquivo));

    const req = http.expectOne('http://api.local/api/notas/analisar');
    expect(req.request.method).toBe('POST');
    const corpo = req.request.body as FormData;
    expect((corpo.get('arquivo') as File).name).toBe('notas.csv');

    const lote: ResultadoLote = {
      totalRecebiveis: 1,
      quantidadeAprovada: 1,
      quantidadeRecusada: 0,
      quantidadePendente: 0,
      valorTotalLiquido: 94.47,
      resultados: [
        {
          numeroDocumento: 'NF-1',
          status: 'APROVADA',
          valorNominal: 100,
          valorLiquido: 94.47,
          motivos: [],
        },
      ],
    };
    req.flush(lote);
    expect(await resposta).toEqual(lote);
  });

  it('converte 400 em ApiError com mensagem humana', async () => {
    const resposta = firstValueFrom(api.analisar(new File(['x'], 'n.csv')));
    http
      .expectOne('http://api.local/api/notas/analisar')
      .flush({ erro: 'O CSV não contém notas.' }, { status: 400, statusText: 'Bad Request' });
    const erro = await resposta.catch((e: unknown) => e);
    expect(erro).toBeInstanceOf(ApiError);
    expect((erro as ApiError).titulo).toBe('Nenhuma nota no arquivo');
  });

  it('converte 500 em erro de servidor', async () => {
    const resposta = firstValueFrom(api.analisar(new File(['x'], 'n.csv')));
    http
      .expectOne('http://api.local/api/notas/analisar')
      .flush(null, { status: 500, statusText: 'Server Error' });
    expect(((await resposta.catch((e: unknown) => e)) as ApiError).tipo).toBe('servidor');
  });
});
