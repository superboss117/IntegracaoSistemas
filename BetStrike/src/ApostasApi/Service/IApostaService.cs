using ApostasApi.DTOs.Apostas;
using ApostasApi.DTOs.Estatisticas;
using ApostasApi.DTOs.Jogos;
using ApostasApi.DTOs.Resultados;
using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;

namespace ApostasApi.Services;



public interface IApostaService
{
    Task<ApiResult<object>> CriarAsync(CriarApostaDto dto);
    Task<ApiResult<object>> ListarAsync(int? idUtilizador, string? codigoJogo, int? estado, DateTime? inicio, DateTime? fim);
    Task<ApiResult<object>> ObterAsync(int id);
    Task<ApiResult<object>> CancelarAsync(int id);
}
