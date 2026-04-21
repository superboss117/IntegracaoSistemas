using ApostasApi.DTOs.Apostas;
using ApostasApi.DTOs.Estatisticas;
using ApostasApi.DTOs.Jogos;
using ApostasApi.DTOs.Resultados;
using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;

namespace ApostasApi.Services;


public interface IEstatisticaService
{
    Task<ApiResult<EstatisticaJogoDto>> ObterPorJogoAsync(string codigoJogo);
    Task<ApiResult<EstatisticaCompeticaoDto>> ObterPorCompeticaoAsync(string competicao);
}