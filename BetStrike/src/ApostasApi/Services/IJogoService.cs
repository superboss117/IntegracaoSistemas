using ApostasApi.DTOs.Jogos;

namespace ApostasApi.Services
{
    public interface IJogoService
    {
        Task<object> CriarJogoAsync(CriarJogoDto dto);
        Task AtualizarJogoAsync(string codigoJogo, AtualizarJogoDto dto);
        Task<object> ListarJogosAsync(DateTime? data, int? estado, string? competicao);
        Task<object?> ObterJogoAsync(string codigoJogo);
        Task RemoverJogoAsync(string codigoJogo);
    }
}