using ApostasApi.DTOs.Jogos;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ApostasApi.Services
{
    public class SincronizacaoService
    {
        private readonly HttpClient _httpClient;
        private readonly IJogoService _jogoService;

        public SincronizacaoService(HttpClient httpClient, IJogoService jogoService)
        {
            _httpClient = httpClient;
            _jogoService = jogoService;
        }

        public async Task SincronizarJogoAsync(string codigoJogo)
        {
            var jogoExterno = await _httpClient.GetFromJsonAsync<JogoResultadosDto>($"api/jogos/{codigoJogo}");

            if (jogoExterno == null)
                throw new Exception("Jogo não encontrado na Plataforma de Resultados.");

            var jogoLocal = await _jogoService.ObterJogoAsync(codigoJogo);

            if (jogoLocal == null)
            {
                var dtoCriar = new CriarJogoDto
                {
                    CodigoJogo = jogoExterno.Codigo_Jogo,
                    DataHora = jogoExterno.Data_Jogo.Date + jogoExterno.Hora_Inicio,
                    EquipaCasa = jogoExterno.Equipa_Casa,
                    EquipaFora = jogoExterno.Equipa_Fora,
                    Competicao = "Liga Portugal",
                    Estado = jogoExterno.Estado
                };

                await _jogoService.CriarJogoAsync(dtoCriar);
            }
            else
            {
                var dtoAtualizar = new AtualizarJogoDto
                {
                    NovoEstado = jogoExterno.Estado,
                    GolosCasa = jogoExterno.Golos_Casa,
                    GolosFora = jogoExterno.Golos_Fora
                };

                await _jogoService.AtualizarJogoAsync(codigoJogo, dtoAtualizar);
            }
        }
    }
}