using ApostasApi.DTOs.Apostas;
using ApostasApi.DTOs.Estatisticas;
using ApostasApi.DTOs.Jogos;
using ApostasApi.DTOs.Resultados;
using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;

namespace ApostasApi.Services;

public interface IJogoService
{
    Task<ApiResult<object>> CriarAsync(CriarJogoDto dto);
    Task<ApiResult<object>> AtualizarAsync(string codigoJogo, AtualizarJogoDto dto);
    Task<ApiResult<object>> ListarAsync(DateOnly? data, int? estado, string? competicao);
    Task<ApiResult<object>> ObterAsync(string codigoJogo);
    Task<ApiResult<object>> RemoverAsync(string codigoJogo);
}

