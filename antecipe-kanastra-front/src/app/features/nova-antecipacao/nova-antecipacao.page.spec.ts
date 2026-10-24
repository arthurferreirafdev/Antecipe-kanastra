import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NovaAntecipacaoPage } from './nova-antecipacao.page';

const CAB =
  'NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO';
const LINHA =
  'F;92.234.400/0001-80;C;11.111.111/0001-91;S;58.206.002/0001-11;NF-1;Duplicata;100,00;15/01/2027;24/10/2026';

describe('NovaAntecipacaoPage', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [NovaAntecipacaoPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  async function carregar(arquivo: File) {
    const fixture = TestBed.createComponent(NovaAntecipacaoPage);
    await fixture.whenStable();
    await fixture.componentInstance.selecionar(arquivo);
    await fixture.whenStable();
    return fixture;
  }

  it('recusa arquivo que não é CSV com mensagem humana', async () => {
    const el = (await carregar(new File(['x'], 'notas.xlsx'))).nativeElement as HTMLElement;
    expect(el.querySelector('[role=alert]')?.textContent).toContain('não é um arquivo CSV');
    expect(el.querySelector('.envio__botao')).toBeNull();
  });

  it('mostra os problemas do CSV e bloqueia o envio', async () => {
    const el = (
      await carregar(new File([`${CAB}\n${LINHA.replace('100,00', '1.100,00')}`], 'notas.csv'))
    ).nativeElement as HTMLElement;
    expect(el.querySelector('.problemas')?.textContent).toContain('remova o ponto de milhar');
    expect((el.querySelector('.envio__botao') as HTMLButtonElement).disabled).toBe(true);
  });

  it('mostra a prévia e libera o envio para um CSV válido', async () => {
    const el = (await carregar(new File([`${CAB}\n${LINHA}`], 'notas.csv')))
      .nativeElement as HTMLElement;
    expect(el.querySelectorAll('.previa tbody tr').length).toBe(1);
    const botao = el.querySelector('.envio__botao') as HTMLButtonElement;
    expect(botao.disabled).toBe(false);
    expect(botao.textContent).toContain('Analisar 1');
  });

  it('exibe o erro de rede com opção de tentar novamente', async () => {
    const fixture = await carregar(new File([`${CAB}\n${LINHA}`], 'notas.csv'));
    const el = fixture.nativeElement as HTMLElement;
    (el.querySelector('.envio__botao') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect((el.querySelector('.envio__botao') as HTMLButtonElement).disabled).toBe(true);

    TestBed.inject(HttpTestingController)
      .expectOne('/api/notas/analisar')
      .error(new ProgressEvent('error'), { status: 0 });
    await new Promise((r) => setTimeout(r));
    await fixture.whenStable();
    const texto = el.querySelector('.falha')?.textContent ?? '';
    expect(texto).toContain('Sem conexão com o servidor');
    expect(texto).toContain('Tentar novamente');
  });
});
