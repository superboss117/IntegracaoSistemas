using ApostasApi.DTOs.Apostas;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApostasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApostasController : ControllerBase
{
    private readonly IApostaService _service;

    public ApostasController(IApostaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarApostaDto dto)
    {
        var result = await _service.CriarAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idUtilizador,
        [FromQuery] string? codigoJogo,
        [FromQuery] int? estado,
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim)
    {
        var result = await _service.ListarAsync(idUtilizador, codigoJogo, estado, inicio, fim);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id)
    {
        var result = await _service.ObterAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var result = await _service.CancelarAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

