using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApostasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilizadoresController : ControllerBase
{
    private readonly IUtilizadorService _service;

    public UtilizadoresController(IUtilizadorService service)
    {
        _service = service;
    }

 [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarUtilizadorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.CriarAsync(dto);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}