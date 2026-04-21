using ApostasApi.Data;
using ApostasApi.DTOs.Resultados;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Services;

public class ResultadoService : IResultadoService
{
    private readonly IDbExecutor _db;

    public ResultadoService(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<ApiResult<object>> CriarAsync(CriarResultadoDto dto)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@CodigoJogo", dto.CodigoJogo),
            new SqlParameter("@GolosCasa", dto.GolosCasa),
            new SqlParameter("@GolosFora", dto.GolosFora)
        };

        await _db.ExecuteAsync("sp_Resultado_Inserir", parameters);
        return ApiResult<object>.Ok(null, "Resultado inserido com sucesso.");
    }

    public async Task<ApiResult<object>> ObterPorJogoAsync(string codigoJogo)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@CodigoJogo", codigoJogo)
        };

        var table = await _db.QueryAsync("sp_Resultado_ObterPorJogo", parameters);
        return ApiResult<object>.Ok(table, "Resultado obtido com sucesso.");
    }
}
