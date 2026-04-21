using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApostasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstatisticasController : ControllerBase
{
    private readonly IEstatisticaService _service;

    public EstatisticasController(IEstatisticaService service)
    {
        _service = service;
    }

    [HttpGet("jogo/{codigoJogo}")]
    public async Task<IActionResult> ObterPorJogo(string codigoJogo)
    {
        var result = await _service.ObterPorJogoAsync(codigoJogo);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("competicao/{competicao}")]
    public async Task<IActionResult> ObterPorCompeticao(string competicao)
    {
        var result = await _service.ObterPorCompeticaoAsync(competicao);
        return result.Success ? Ok(result) : NotFound(result);
    }
}