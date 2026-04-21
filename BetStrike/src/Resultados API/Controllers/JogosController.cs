using Microsoft.AspNetCore.Mvc;
using ResultadosAPI.DTOs;
using ResultadosAPI.Services;
using System;
using System.Threading.Tasks;

namespace ResultadosAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly JogoService _service;

        public JogosController(JogoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> InserirJogo([FromBody] InserirJogoDTO dto)
        {
            try
            {
                var (id, codigo) = await _service.InserirJogoAsync(dto);
                return CreatedAtAction(nameof(ObterJogo), new { codigoJogo = codigo },
                    new { id_interno = id, codigo_jogo = codigo });
            }
            catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
        }

        [HttpPut("{codigoJogo}")]
        public async Task<IActionResult> AtualizarJogo(string codigoJogo, [FromBody] AtualizarJogoDTO dto)
        {
            try
            {
                await _service.AtualizarJogoAsync(codigoJogo, dto);
                return Ok(new { mensagem = "Jogo atualizado com sucesso." });
            }
            catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> ListarJogos([FromQuery] DateTime? data, [FromQuery] int? estado)
        {
            try
            {
                var jogos = await _service.ListarJogosAsync(data, estado);
                return Ok(jogos);
            }
            catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
        }

        [HttpGet("{codigoJogo}")]
        public async Task<IActionResult> ObterJogo(string codigoJogo)
        {
            try
            {
                var jogo = await _service.ObterJogoAsync(codigoJogo);
                if (jogo == null) return NotFound(new { erro = "Jogo não encontrado." });
                return Ok(jogo);
            }
            catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
        }

        [HttpDelete("{codigoJogo}")]
        public async Task<IActionResult> RemoverJogo(string codigoJogo)
        {
            try
            {
                await _service.RemoverJogoAsync(codigoJogo);
                return Ok(new { mensagem = "Jogo removido com sucesso." });
            }
            catch (Exception ex) { return BadRequest(new { erro = ex.Message }); }
        }
    }
}