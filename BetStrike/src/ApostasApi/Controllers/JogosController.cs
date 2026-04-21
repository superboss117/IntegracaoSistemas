using ApostasApi.DTOs.Jogos;
using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApostasApi.Controllers
{
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
        public async Task<IActionResult> InserirJogo([FromBody] CriarJogoDto dto)
        {
            try
            {
                var result = await _service.CriarJogoAsync(dto);
                return CreatedAtAction(nameof(ObterJogo), new { codigoJogo = dto.CodigoJogo }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPut("{codigoJogo}")]
        public async Task<IActionResult> AtualizarJogo(string codigoJogo, [FromBody] AtualizarJogoDto dto)
        {
            try
            {
                await _service.AtualizarJogoAsync(codigoJogo, dto);
                return Ok(new { mensagem = "Jogo atualizado com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListarJogos([FromQuery] DateTime? data, [FromQuery] int? estado, [FromQuery] string? competicao)
        {
            try
            {
                var jogos = await _service.ListarJogosAsync(data, estado, competicao);
                return Ok(jogos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpGet("{codigoJogo}")]
        public async Task<IActionResult> ObterJogo(string codigoJogo)
        {
            try
            {
                var jogo = await _service.ObterJogoAsync(codigoJogo);
                if (jogo == null)
                    return NotFound(new { erro = "Jogo não encontrado." });

                return Ok(jogo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpDelete("{codigoJogo}")]
        public async Task<IActionResult> RemoverJogo(string codigoJogo)
        {
            try
            {
                await _service.RemoverJogoAsync(codigoJogo);
                return Ok(new { mensagem = "Jogo removido com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }
    }
}