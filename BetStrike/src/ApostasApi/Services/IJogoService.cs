using ApostasApi.DTOs.Jogos;

namespace ApostasApi.Services
{
    public interface IJogoService
    {
       Task<JogoDto> CriarJogoAsync(CriarJogoDto dto);
        Task<List<JogoDto>> ListarJogosAsync(DateTime? data, int? estado, string? competicao);
        Task<JogoDto?> ObterJogoAsync(string codigoJogo);
        Task AtualizarJogoAsync(string codigoJogo, AtualizarJogoDto dto);
       
        Task RemoverJogoAsync(string codigoJogo);
    }
}