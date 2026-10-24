using Antecipe.Api.Application;
using Antecipe.Api.Domain;
using Antecipe.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;

namespace Antecipe.Api.Controllers;

[ApiController]
[Route("api/notas")]
public sealed class NotasController(MotorRegras motor, RecusasService recusas) : ControllerBase
{
    [HttpPost("analisar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultadoLote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Analisar([FromForm] IFormFile? arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { erro = "Envie um arquivo CSV não vazio no campo 'arquivo'." });

        List<LinhaCsv> notas;
        try
        {
            await using var stream = arquivo.OpenReadStream();
            notas = CsvRecebiveis.LerLinhas(stream);
        }
        catch (Exception ex) when (ex is FormatException or MalformedLineException)
        {
            return BadRequest(new { erro = ex.Message });
        }

        if (notas.Count == 0)
            return BadRequest(new { erro = "O CSV não contém notas." });

        var (unicas, duplicadas) = CsvRecebiveis.SepararDuplicadas(notas);

        try
        {
            // Todas as linhas são analisadas; uma recusa não interrompe as demais.
            var resultado = await motor.Processar(unicas, false, ct);
            var loteId = recusas.Registrar(arquivo.FileName, resultado.Recusadas);
            // Notas com verificação inconclusiva voltam como PENDENTE_VALIDACAO junto com as demais, sem bloquear o lote.
            resultado = resultado with { Duplicadas = duplicadas, LoteId = loteId };
            var descartadas = resultado.QuantidadeRecusada + duplicadas.Count;
            if (descartadas == 0 || resultado.QuantidadeAprovada == 0) return Ok(resultado);

            var aprovadas = resultado.Aceitas.Select(x => x.Linha).ToHashSet();
            return Ok(resultado with
            {
                PerguntaDownload = PerguntaDownload(resultado.QuantidadeRecusada, duplicadas.Count, resultado.QuantidadeAprovada),
                CsvSemRecusadas = CsvRecebiveis.Escrever(unicas.Where(l => aprovadas.Contains(l.Linha)))
            });
        }
        catch (ConfiguracaoException ex)
        {
            return Problem("Configuração de regras inválida: " + ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("lotes/{loteId:guid}/recusadas")]
    [ProducesResponseType(typeof(IReadOnlyList<NotaRecusada>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Recusadas(Guid loteId) => recusas.Obter(loteId) is { } notas
        ? Ok(notas)
        : NotFound(new { erro = "Lote não encontrado. Ele pode ter expirado; envie o arquivo novamente." });

    private static string PerguntaDownload(int recusadas, int duplicadas, int aprovadas)
    {
        var partes = new List<string>();
        if (recusadas > 0) partes.Add(recusadas == 1 ? "1 nota foi recusada" : $"{recusadas} notas foram recusadas");
        if (duplicadas > 0) partes.Add(duplicadas == 1 ? "1 nota duplicada foi desconsiderada" : $"{duplicadas} notas duplicadas foram desconsideradas");
        var apenas = aprovadas == 1 ? "a nota aprovada" : $"as {aprovadas} notas aprovadas";
        return $"{string.Join(" e ", partes)}. Deseja baixar o arquivo CSV apenas com {apenas}?";
    }
}
