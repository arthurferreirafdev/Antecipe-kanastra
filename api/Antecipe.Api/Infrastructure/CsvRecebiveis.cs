using System.Globalization;
using System.Text;
using Antecipe.Api.Domain;
using Microsoft.VisualBasic.FileIO;

namespace Antecipe.Api.Infrastructure;

public static class CsvRecebiveis
{
    private static readonly string[] Colunas = ["NOME_FUNDO", "DOC_FUNDO", "NOME_CEDENTE", "DOC_CEDENTE",
        "NOME_SACADO", "DOC_SACADO", "NU_DOCUMENTO", "TIPO_RECEBIVEL", "VALOR_NOMINAL",
        "DATA_VENCIMENTO_ORIGINAL", "DATA_AQUISICAO"];

    private const string FormatoInvalido = "FORMATO_INVALIDO";

    // Leitura estrita (estoque): qualquer linha inválida interrompe a carga.
    public static List<Recebivel> Ler(Stream stream)
    {
        var linhas = LerLinhas(stream);
        var invalida = linhas.FirstOrDefault(l => l.Recebivel is null);
        if (invalida is not null)
            throw new FormatException($"Linha {invalida.Linha}: {string.Join(" ", invalida.Erros.Select(e => e.Mensagem))}");
        return linhas.Select(l => l.Recebivel!).ToList();
    }

    // Leitura tolerante (upload): cada linha com problema de formato volta com seus erros, sem interromper as demais.
    public static List<LinhaCsv> LerLinhas(Stream stream)
    {
        using var parser = new TextFieldParser(stream, Encoding.UTF8, true);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(";");
        parser.HasFieldsEnclosedInQuotes = true;
        var cabecalho = parser.ReadFields() ?? throw new FormatException("CSV vazio.");
        if (!cabecalho.SequenceEqual(Colunas))
            throw new FormatException("Cabeçalho CSV inválido. Verifique nomes e ordem das colunas.");

        var linhas = new List<LinhaCsv>();
        while (!parser.EndOfData)
        {
            var numero = (int)parser.LineNumber;
            string[]? campos;
            try { campos = parser.ReadFields(); }
            catch (MalformedLineException ex)
            {
                linhas.Add(new LinhaCsv((int)ex.LineNumber, new Dictionary<string, string>(), null,
                    [new Motivo(FormatoInvalido, "Não foi possível ler esta linha. Verifique se há aspas abertas sem fechamento.")]));
                continue;
            }
            if (campos is null) continue;
            linhas.Add(Interpretar(numero, campos));
        }
        return linhas;
    }

    private static LinhaCsv Interpretar(int numero, string[] f)
    {
        var dados = Colunas.Select((c, i) => (c, Valor: i < f.Length ? f[i] : ""))
            .ToDictionary(x => x.c, x => x.Valor, StringComparer.Ordinal);
        if (f.Length != Colunas.Length)
            return new LinhaCsv(numero, dados, null, [new Motivo(FormatoInvalido,
                $"A linha tem {f.Length} coluna(s), mas o arquivo deve ter {Colunas.Length}, separadas por ponto e vírgula (;).")]);

        var erros = new List<Motivo>();
        if (string.IsNullOrWhiteSpace(f[6]))
            erros.Add(new Motivo(FormatoInvalido, "O número do documento da nota (NU_DOCUMENTO) não foi informado."));
        if (!decimal.TryParse(f[8], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.GetCultureInfo("pt-BR"), out var valor))
            erros.Add(new Motivo(FormatoInvalido, $"O valor nominal \"{f[8]}\" não é um número válido. Use vírgula para os centavos, por exemplo 1234,56."));
        else if (valor <= 0)
            erros.Add(new Motivo(FormatoInvalido, "O valor nominal da nota deve ser maior que zero."));
        var vencimento = Data(f[9], "data de vencimento", erros);
        var aquisicao = Data(f[10], "data de aquisição", erros);

        return erros.Count > 0
            ? new LinhaCsv(numero, dados, null, erros)
            : new LinhaCsv(numero, dados,
                new Recebivel(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], valor, vencimento, aquisicao), []);
    }

    private static DateOnly Data(string valor, string nome, List<Motivo> erros)
    {
        if (DateOnly.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            return data;
        erros.Add(new Motivo(FormatoInvalido, $"A {nome} \"{valor}\" é inválida. Use o formato dd/mm/aaaa, por exemplo 31/12/2026."));
        return default;
    }

    // Mantém a primeira ocorrência de cada fundo + cedente + número de documento + vencimento; as repetições saem do lote.
    public static (List<LinhaCsv> Unicas, List<NotaDuplicada> Duplicadas) SepararDuplicadas(IEnumerable<LinhaCsv> linhas)
    {
        var primeiras = new Dictionary<string, int>(StringComparer.Ordinal);
        var unicas = new List<LinhaCsv>();
        var duplicadas = new List<NotaDuplicada>();
        foreach (var l in linhas)
        {
            var numeroDocumento = l.Dados.GetValueOrDefault("NU_DOCUMENTO", "").Trim();
            if (numeroDocumento.Length == 0) { unicas.Add(l); continue; }
            var chave = Documentos.Chave(l.Dados.GetValueOrDefault("DOC_FUNDO"), l.Dados.GetValueOrDefault("DOC_CEDENTE"),
                numeroDocumento, l.Dados.GetValueOrDefault("DATA_VENCIMENTO_ORIGINAL"));
            if (primeiras.TryGetValue(chave, out var original))
                duplicadas.Add(new NotaDuplicada(l.Linha, original, numeroDocumento, l.Dados,
                    $"A nota {numeroDocumento} aparece mais de uma vez no arquivo (mesmo fundo, cedente, número de documento e vencimento). " +
                    $"Analisamos apenas a primeira ocorrência, na linha {original}, e esta repetição foi desconsiderada."));
            else
            {
                primeiras[chave] = l.Linha;
                unicas.Add(l);
            }
        }
        return (unicas, duplicadas);
    }

    public static string Escrever(IEnumerable<LinhaCsv> linhas)
    {
        var sb = new StringBuilder();
        sb.AppendJoin(';', Colunas).Append("\r\n");
        foreach (var l in linhas)
            sb.AppendJoin(';', Colunas.Select(c => Escapar(l.Dados.GetValueOrDefault(c, "")))).Append("\r\n");
        return sb.ToString();
    }

    private static string Escapar(string valor) =>
        valor.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
}
