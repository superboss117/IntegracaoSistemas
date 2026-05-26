using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
        try
        {
            var result = await _service.ObterPorJogoAsync(codigoJogo);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("competicao/{competicao}")]
    public async Task<IActionResult> ObterPorCompeticao(string competicao)
    {
        try
        {
            var result = await _service.ObterPorCompeticaoAsync(competicao);
            return result.Success ? Ok(result) : NotFound(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}