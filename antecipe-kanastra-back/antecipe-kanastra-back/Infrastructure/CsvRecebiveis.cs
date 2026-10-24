using System.Globalization;
using System.Text;
using antecipe_kanastra.Domain;
using Microsoft.VisualBasic.FileIO;

namespace antecipe_kanastra.Infrastructure;

public static class CsvRecebiveis
{
    private static readonly string[] Colunas = ["NOME_FUNDO", "DOC_FUNDO", "NOME_CEDENTE", "DOC_CEDENTE",
        "NOME_SACADO", "DOC_SACADO", "NU_DOCUMENTO", "TIPO_RECEBIVEL", "VALOR_NOMINAL",
        "DATA_VENCIMENTO_ORIGINAL", "DATA_AQUISICAO"];

    public static List<Recebivel> Ler(Stream stream)
    {
        using var parser = new TextFieldParser(stream, Encoding.UTF8, true);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(";");
        parser.HasFieldsEnclosedInQuotes = true;
        var cabecalho = parser.ReadFields() ?? throw new FormatException("CSV vazio.");
        if (!cabecalho.SequenceEqual(Colunas))
            throw new FormatException("Cabeçalho CSV inválido. Verifique nomes e ordem das colunas.");
        var recebiveis = new List<Recebivel>();
        while (!parser.EndOfData)
        {
            var f = parser.ReadFields();
            if (f is null) continue;
            if (f.Length != Colunas.Length) throw new FormatException($"Linha {parser.LineNumber}: quantidade de colunas inválida.");
            if (!decimal.TryParse(f[8], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.GetCultureInfo("pt-BR"), out var valor) || valor <= 0 ||
                !DateOnly.TryParseExact(f[9], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento) ||
                !DateOnly.TryParseExact(f[10], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var aquisicao) ||
                string.IsNullOrWhiteSpace(f[6]))
                throw new FormatException($"Linha {parser.LineNumber}: valor, data ou número de documento inválido.");
            recebiveis.Add(new Recebivel(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], valor, vencimento, aquisicao));
        }
        return recebiveis;
    }
}
