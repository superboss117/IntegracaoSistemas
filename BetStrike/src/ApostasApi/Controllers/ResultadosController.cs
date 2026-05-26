using ApostasApi.DTOs.Resultados;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
        try
        {
            var result = await _service.CriarAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("{codigoJogo}")]
    public async Task<IActionResult> Obter(string codigoJogo)
    {
        try
        {
            var result = await _service.ObterPorJogoAsync(codigoJogo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}