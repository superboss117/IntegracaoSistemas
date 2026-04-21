using ApostasApi.Data;
using ApostasApi.DTOs.Utilizadores;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Services;

public class UtilizadorService : IUtilizadorService
{
    private readonly IDbExecutor _db;

    public UtilizadorService(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<ApiResult<object>> CriarAsync(CriarUtilizadorDto dto)
    {
        var parameters = new List<IDataParameter>
    {
        new SqlParameter("@Utilizador_Id", dto.UtilizadorId),
        new SqlParameter("@Nome", dto.Nome)
    };

    await _db.ExecuteAsync("SP_Criar_Utilizador", parameters);
    return ApiResult<object>.Ok(null, "Utilizador criado com sucesso.");
    }
}