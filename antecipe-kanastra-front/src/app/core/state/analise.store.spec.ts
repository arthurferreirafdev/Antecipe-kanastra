import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ResultadoLote } from '../../models/api.models';
import { ApiError } from '../api/api-error';
import { NotasApiService } from '../api/notas-api.service';
import { AnaliseStore } from './analise.store';

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

describe('AnaliseStore', () => {
  let resposta: Subject<ResultadoLote>;
  let chamadas: number;

  beforeEach(() => {
    localStorage.clear();
    chamadas = 0;
    resposta = new Subject<ResultadoLote>();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: NotasApiService,
          useValue: {
            analisar: () => {
              chamadas++;
              return resposta;
            },
          },
        },
      ],
    });
  });

  it('ignora um segundo envio enquanto o primeiro está em andamento', async () => {
    const store = TestBed.inject(AnaliseStore);
    const arquivo = new File(['x'], 'notas.csv');
    const primeiro = store.analisar(arquivo, null);
    const segundo = await store.analisar(arquivo, null);
    expect(segundo).toBe(false);
    expect(chamadas).toBe(1);
    expect(store.enviando()).toBe(true);

    resposta.next(lote);
    resposta.complete();
    expect(await primeiro).toBe(true);
    expect(store.enviando()).toBe(false);
  });

  it('guarda a análise no navegador', async () => {
    const store = TestBed.inject(AnaliseStore);
    const p = store.analisar(new File(['x'], 'notas.csv'), null);
    resposta.next(lote);
    resposta.complete();
    await p;
    expect(store.resumo()?.valorLiquido).toBe(94.47);
    expect(JSON.parse(localStorage.getItem('antecipe:ultima-analise:v1')!).arquivoNome).toBe(
      'notas.csv',
    );
  });

  it('expõe o erro da API e permite limpá-lo', async () => {
    const store = TestBed.inject(AnaliseStore);
    const p = store.analisar(new File(['x'], 'notas.csv'), null);
    resposta.error(new ApiError('rede', 0, 'Sem conexão', 'd'));
    expect(await p).toBe(false);
    expect(store.erro()?.tipo).toBe('rede');
    store.limparErro();
    expect(store.erro()).toBeNull();
  });

  it('ignora conteúdo inválido no armazenamento local', () => {
    localStorage.setItem('antecipe:ultima-analise:v1', '{"versao":99}');
    expect(TestBed.inject(AnaliseStore).analise()).toBeNull();
  });
});
