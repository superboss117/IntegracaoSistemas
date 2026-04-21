using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;

namespace ApostasApi.Services;

public interface IUtilizadorService
{
    Task<ApiResult<UtilizadorResponseDto>> CriarAsync(CriarUtilizadorDto dto);
}