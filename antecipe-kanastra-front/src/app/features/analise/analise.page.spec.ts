import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AnaliseSalva } from '../../models/analise.models';
import { AnalisePage } from './analise.page';

const analise: AnaliseSalva = {
  versao: 1,
  arquivoNome: 'notas.csv',
  arquivoTamanho: 100,
  analisadaEm: '2026-10-24T14:00:00.000Z',
  linhas: null,
  resultado: {
    totalRecebiveis: 2,
    quantidadeAprovada: 1,
    quantidadeRecusada: 1,
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
      {
        numeroDocumento: 'NF-2',
        status: 'RECUSADA',
        valorNominal: 6780,
        valorLiquido: null,
        motivos: [
          { codigo: 'PRAZO_MAXIMO', mensagem: 'Prazo de 341 dias excede o máximo de 180 dias.' },
        ],
      },
    ],
  },
};

describe('AnalisePage', () => {
  async function montar() {
    await TestBed.configureTestingModule({
      imports: [AnalisePage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(AnalisePage);
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(() => localStorage.clear());

  it('mostra estado vazio sem análise', async () => {
    const el = (await montar()).nativeElement as HTMLElement;
    expect(el.textContent).toContain('Ainda não há uma análise para mostrar');
    expect(el.querySelector('a[href="/nova-antecipacao"]')).not.toBeNull();
  });

  it('responde quantas notas, quanto recebe e por que houve recusa', async () => {
    localStorage.setItem('antecipe:ultima-analise:v1', JSON.stringify(analise));
    const el = (await montar()).nativeElement as HTMLElement;
    expect(el.querySelector('h1')?.textContent).toContain('1 de 2 notas pode ser antecipada');
    expect(el.querySelector('.hero app-money')?.getAttribute('aria-label')).toContain('94,47');
    expect(el.querySelector('.motivos__lista')?.textContent).toContain('Prazo acima do permitido');
    expect(el.querySelectorAll('app-notas-tabela tbody tr.linha').length).toBe(2);
  });

  it('abre o painel com a explicação da recusa via query param', async () => {
    localStorage.setItem('antecipe:ultima-analise:v1', JSON.stringify(analise));
    const fixture = await montar();
    fixture.componentRef.setInput('nota', '1');
    await fixture.whenStable();
    const painel = (fixture.nativeElement as HTMLElement).querySelector('[role=dialog]');
    expect(painel?.textContent).toContain('Nota não elegível');
    expect(painel?.textContent).toContain('341 dias');
    expect(painel?.textContent).toContain('até 180 dias');
  });
});
