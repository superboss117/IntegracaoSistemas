using ApostasApi.DTOs.Jogos;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApostasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JogosController : ControllerBase
{
    private readonly IJogoService _service;

    public JogosController(IJogoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarJogoDto dto)
    {
        var result = await _service.CriarAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{codigoJogo}/estado-resultado")]
    public async Task<IActionResult> Atualizar(string codigoJogo, [FromBody] AtualizarEstadoResultadoJogoDto dto)
    {
        var result = await _service.AtualizarEstadoResultadoAsync(codigoJogo, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? data, [FromQuery] int? estado, [FromQuery] string? competicao)
    {
        var result = await _service.ListarAsync(data, estado, competicao);
        return Ok(result);
    }

    [HttpGet("{codigoJogo}")]
    public async Task<IActionResult> Obter(string codigoJogo)
    {
        var result = await _service.ObterAsync(codigoJogo);
        return Ok(result);
    }

    [HttpDelete("{codigoJogo}")]
    public async Task<IActionResult> Remover(string codigoJogo)
    {
        var result = await _service.RemoverAsync(codigoJogo);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}