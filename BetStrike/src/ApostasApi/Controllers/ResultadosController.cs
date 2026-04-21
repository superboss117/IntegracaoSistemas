using ApostasApi.DTOs.Resultados;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApostasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultadosController : ControllerBase
{
    private readonly IResultadoService _service;

    public ResultadosController(IResultadoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarResultadoDto dto)
    {
        var result = await _service.CriarAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{codigoJogo}")]
    public async Task<IActionResult> Obter(string codigoJogo)
    {
        var result = await _service.ObterPorJogoAsync(codigoJogo);
        return Ok(result);
    }
}