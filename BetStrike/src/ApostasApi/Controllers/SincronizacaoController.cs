using ApostasApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApostasApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SincronizacaoController : ControllerBase
    {
        private readonly SincronizacaoService _service;

        public SincronizacaoController(SincronizacaoService service)
        {
            _service = service;
        }

        [HttpPost("jogos/{codigoJogo}")]
        public async Task<IActionResult> SincronizarJogo(string codigoJogo)
        {
            try
            {
                await _service.SincronizarJogoAsync(codigoJogo);
                return Ok(new { mensagem = "Jogo sincronizado com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }
    }
}