using ApostasApi.DTOs.Apostas;
using ApostasApi.DTOs.Estatisticas;
using ApostasApi.DTOs.Jogos;
using ApostasApi.DTOs.Resultados;
using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;

namespace ApostasApi.Services;



public interface IResultadoService
{
    Task<ApiResult<object>> CriarAsync(CriarResultadoDto dto);
    Task<ApiResult<ResultadoDto>> ObterPorJogoAsync(string codigoJogo);
}

