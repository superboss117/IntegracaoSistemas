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
        new SqlParameter("@Codigo_Jogo", dto.CodigoJogo),
        new SqlParameter("@Golos_Casa", dto.GolosCasa),
        new SqlParameter("@Golos_Fora", dto.GolosFora)
    };

    await _db.ExecuteAsync("SP_Inserir_Resultado", parameters);
    return ApiResult<object>.Ok(null, "Resultado inserido com sucesso.");
    }

   public async Task<ApiResult<ResultadoDto>> ObterPorJogoAsync(string codigoJogo)
{
    var parameters = new List<IDataParameter>
    {
        new SqlParameter("@Codigo_Jogo", codigoJogo)
    };

    var table = await _db.QueryAsync("SP_Obter_Resultado", parameters);

    if (table.Rows.Count == 0)
        return ApiResult<ResultadoDto>.Fail("Resultado não encontrado.");

    var row = table.Rows[0];

    var dto = new ResultadoDto
    {
        CodigoJogo = row.Table.Columns.Contains("Codigo_Jogo")
            ? row["Codigo_Jogo"]?.ToString() ?? string.Empty
            : codigoJogo,
        GolosCasa = row.Table.Columns.Contains("Golos_Casa") && row["Golos_Casa"] != DBNull.Value
            ? Convert.ToInt32(row["Golos_Casa"])
            : 0,
        GolosFora = row.Table.Columns.Contains("Golos_Fora") && row["Golos_Fora"] != DBNull.Value
            ? Convert.ToInt32(row["Golos_Fora"])
            : 0
    };

    return ApiResult<ResultadoDto>.Ok(dto, "Resultado obtido com sucesso.");
}
}
