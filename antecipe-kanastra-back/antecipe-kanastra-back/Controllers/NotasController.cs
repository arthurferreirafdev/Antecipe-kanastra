using antecipe_kanastra.Application;
using antecipe_kanastra.Domain;
using antecipe_kanastra.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;

namespace antecipe_kanastra.Controllers;

[ApiController]
[Route("api/notas")]
public sealed class NotasController(MotorRegras motor) : ControllerBase
{
    [HttpPost("analisar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultadoLote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Analisar([FromForm] IFormFile? arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { erro = "Envie um arquivo CSV não vazio no campo 'arquivo'." });

        List<Recebivel> notas;
        try
        {
            await using var stream = arquivo.OpenReadStream();
            notas = CsvRecebiveis.Ler(stream);
        }
        catch (Exception ex) when (ex is FormatException or MalformedLineException)
        {
            return BadRequest(new { erro = ex.Message });
        }

        if (notas.Count == 0)
            return BadRequest(new { erro = "O CSV não contém notas." });

        try
        {
            var resultado = await motor.Processar(notas, false, ct);
            if (resultado.QuantidadePendente > 0)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    erro = "Não foi possível concluir a análise de todas as notas. Tente novamente quando as verificações estiverem disponíveis.",
                    pendencias = resultado.Resultados.Where(x => x.Status == StatusRecebivel.PENDENTE_VALIDACAO)
                        .Select(x => new { x.NumeroDocumento, x.Motivos })
                });
            return Ok(resultado);
        }
        catch (ConfiguracaoException ex)
        {
            return Problem("Configuração de regras inválida: " + ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
