// Fluxo completo contra o backend real: upload do CSV → resultado → explicação de recusa.
// Pré-requisitos: API rodando e `ng serve` com o proxy apontando para ela.
//   E2E_URL=http://localhost:4200 E2E_SAIDA=./e2e/capturas node e2e/fluxo-completo.mjs
// Usa o Microsoft Edge instalado (channel "msedge"); defina E2E_CHANNEL=chrome para o Chrome.

import { chromium } from 'playwright-core';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { tmpdir } from 'node:os';

const URL_BASE = process.env.E2E_URL ?? 'http://localhost:4200';
const SAIDA = resolve(process.env.E2E_SAIDA ?? 'e2e/capturas');
const CSV = resolve('public/modelo/notas_exemplo.csv');
mkdirSync(SAIDA, { recursive: true });

const falhas = [];
const verificar = (condicao, mensagem) => {
  console.log(`${condicao ? '✔' : '✘'} ${mensagem}`);
  if (!condicao) falhas.push(mensagem);
};

const browser = await chromium.launch({ channel: process.env.E2E_CHANNEL ?? 'msedge' });
const contexto = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'pt-BR' });
const page = await contexto.newPage();
const errosConsole = [];
page.on('console', (m) => m.type() === 'error' && errosConsole.push(m.text()));
page.on('pageerror', (e) => errosConsole.push(e.message));
const capturar = (nome, opcoes = {}) => page.screenshot({ path: join(SAIDA, `${nome}.png`), fullPage: true, ...opcoes });

// 1. Visão geral vazia
await page.goto(URL_BASE);
await page.evaluate(() => localStorage.clear());
await page.reload();
await page.getByRole('heading', { name: /Descubra hoje/ }).waitFor();
verificar(true, 'visão geral mostra estado vazio');
await capturar('01-visao-geral-vazia');

// 2. Arquivo inválido: mensagem humana antes de enviar
await page.goto(`${URL_BASE}/nova-antecipacao`);
const invalido = join(tmpdir(), 'antecipe_invalido.csv');
writeFileSync(
  invalido,
  'NOME_FUNDO;DOC_FUNDO;NOME_CEDENTE;DOC_CEDENTE;NOME_SACADO;DOC_SACADO;NU_DOCUMENTO;TIPO_RECEBIVEL;VALOR_NOMINAL;DATA_VENCIMENTO_ORIGINAL;DATA_AQUISICAO\n' +
    'ANTECIPE FIDC;92.234.400/0001-80;Ced;11.111.111/0001-91;Sac;58.206.002/0001-11;X1;Duplicata;4.250,00;15/01/2027;24/10/2026\n',
);
await page.locator('input[type=file]').setInputFiles(invalido);
await page.getByText(/remova o ponto de milhar/).waitFor();
verificar(await page.getByRole('button', { name: /Analisar/ }).isDisabled(), 'arquivo inválido bloqueia o envio');
await capturar('02-arquivo-invalido');

// 3. Arquivo válido: prévia e envio real
await page.locator('input[type=file]').setInputFiles(CSV);
await page.locator('.arquivo__nome', { hasText: 'notas_exemplo.csv' }).waitFor();
const linhasPrevia = await page.locator('.previa tbody tr').count();
verificar(linhasPrevia > 0, `prévia mostra ${linhasPrevia} linhas`);
await capturar('03-upload-previa');
await page.getByRole('button', { name: /Analisar \d+ notas/ }).click();
await page.waitForURL('**/analise');
await page.locator('.hero app-money').waitFor();
await page.waitForTimeout(1200); // fim da animação do valor

// 4. Resultado
const linhasTabela = await page.locator('app-notas-tabela tbody tr.linha').count();
verificar(linhasTabela === 19, `tabela mostra as 19 notas do arquivo (${linhasTabela})`);
const valor = await page.locator('.hero app-money').getAttribute('aria-label');
verificar(/R\$/.test(valor ?? ''), `valor líquido exibido: ${valor}`);
verificar((await page.locator('.motivos__lista li').count()) > 0, 'motivos de recusa agrupados');
await capturar('04-resultado');

// 5. Filtro por motivo + painel de explicação
await page.locator('.motivo').filter({ hasText: 'Prazo acima do permitido' }).click();
await page.waitForURL(/motivo=PRAZO_MAXIMO/);
verificar((await page.locator('app-notas-tabela tbody tr.linha').count()) === 1, 'filtro por motivo mostra 1 nota');
await page.getByRole('button', { name: /Ver detalhes da nota NF-1017/ }).click();
const painel = page.getByRole('dialog');
await painel.waitFor();
verificar(await painel.getByText('Nota não elegível').isVisible(), 'painel explica a recusa');
verificar(await painel.getByText('Prazo identificado').isVisible(), 'painel mostra prazo identificado');
verificar(await painel.getByText('Limite do fundo').isVisible(), 'painel mostra limite do fundo');
await page.waitForTimeout(500);
await capturar('05-painel-recusa', { fullPage: false });
await page.keyboard.press('Escape');
await painel.waitFor({ state: 'detached' });
verificar(true, 'Esc fecha o painel');

// 6. Nota aprovada
await page.goto(`${URL_BASE}/analise?status=APROVADA`);
await page.locator('app-notas-tabela tbody tr.linha').first().click();
await page.getByRole('dialog').getByText('Você recebe').waitFor();
await page.waitForTimeout(1100);
await capturar('06-painel-aprovada', { fullPage: false });
await page.keyboard.press('Escape');

// 7. Visão geral com a última análise
await page.goto(URL_BASE);
await page.getByText('Disponível para antecipação').waitFor();
await page.waitForTimeout(1100);
verificar(true, 'visão geral mostra a última análise');
await capturar('07-visao-geral');

// 8. Celular
await page.setViewportSize({ width: 390, height: 844 });
await page.goto(`${URL_BASE}/analise`);
await page.locator('.hero app-money').waitFor();
await page.waitForTimeout(1100);
const rolagemHorizontal = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
verificar(!rolagemHorizontal, 'sem rolagem horizontal em 390px');
await capturar('08-resultado-celular');
await page.goto(`${URL_BASE}/nova-antecipacao`);
await capturar('09-upload-celular');

verificar(errosConsole.length === 0, `sem erros no console${errosConsole.length ? ': ' + errosConsole.join(' | ') : ''}`);
await browser.close();

console.log(`\nCapturas em ${SAIDA}`);
if (falhas.length) {
  console.error(`\n${falhas.length} verificação(ões) falharam.`);
  process.exit(1);
}
