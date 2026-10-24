import { NotaAnalisada } from '../../models/analise.models';
import { explicarMotivo } from './motivos-catalogo';

const nota: NotaAnalisada = {
  indice: 0,
  numeroDocumento: 'NF-1',
  status: 'RECUSADA',
  valorNominal: 45.9,
  valorLiquido: null,
  desconto: null,
  motivos: [],
  csv: null,
};

// As mensagens abaixo são exatamente as produzidas por Application/Validadores.cs.
describe('explicarMotivo', () => {
  it('extrai prazo identificado e limite de PRAZO_MAXIMO', () => {
    const m = explicarMotivo({
      codigo: 'PRAZO_MAXIMO',
      mensagem: 'Prazo de 341 dias excede o máximo de 180 dias.',
    });
    expect(m.titulo).toBe('Prazo acima do permitido');
    expect(m.detalhes).toEqual([
      { rotulo: 'Prazo identificado', valor: '341 dias', destaque: true },
      { rotulo: 'Limite do fundo', valor: 'até 180 dias' },
    ]);
  });

  it('extrai concentração projetada e limite', () => {
    const m = explicarMotivo({
      codigo: 'CONCENTRACAO_MAXIMA_SACADO',
      mensagem: 'Concentração projetada do sacado: 82,86% (limite 10,00%).',
    });
    expect(m.detalhes.map((d) => d.valor)).toEqual(['82,86%', 'até 10,00%']);
  });

  it('usa o valor da nota e o mínimo da mensagem em VALOR_MINIMO', () => {
    const m = explicarMotivo(
      {
        codigo: 'VALOR_MINIMO',
        mensagem: 'O valor da nota está abaixo do mínimo de R$ 100,00 permitido pelo fundo.',
      },
      nota,
    );
    expect(m.detalhes[0]!.valor).toContain('45,90');
    expect(m.detalhes[1]!.valor).toContain('100,00');
  });

  it('extrai situação cadastral encontrada e aceitas', () => {
    const m = explicarMotivo({
      codigo: 'SITUACAO_CADASTRAL_SACADO',
      mensagem: 'Situação cadastral do sacado: BAIXADA. Aceitas: ATIVA.',
    });
    expect(m.detalhes.map((d) => d.valor)).toEqual(['BAIXADA', 'ATIVA']);
  });

  it('extrai vencimento e data da análise de NAO_VENCIDA', () => {
    const m = explicarMotivo({
      codigo: 'NAO_VENCIDA',
      mensagem: 'A nota venceu em 12/08/2026, antes da data da análise (24/10/2026).',
    });
    expect(m.detalhes.map((d) => d.valor)).toEqual(['12/08/2026', '24/10/2026']);
  });

  it('não inventa detalhes quando a mensagem não segue o padrão', () => {
    expect(explicarMotivo({ codigo: 'PRAZO_MAXIMO', mensagem: 'outra coisa' }).detalhes).toEqual(
      [],
    );
  });

  it('mantém a mensagem original para códigos desconhecidos', () => {
    const m = explicarMotivo({ codigo: 'NOVA_REGRA', mensagem: 'Mensagem do servidor.' });
    expect(m.titulo).toBe('Regra do fundo não atendida');
    expect(m.explicacao).toBe('Mensagem do servidor.');
    expect(m.mensagemTecnica).toBe('Mensagem do servidor.');
  });

  it('trata motivos de notas pendentes como verificação não concluída', () => {
    const m = explicarMotivo(
      { codigo: 'SITUACAO_CADASTRAL_SACADO', mensagem: 'indisponível' },
      null,
      true,
    );
    expect(m.titulo).toBe('Verificação não concluída');
  });
});
