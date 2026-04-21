using ApostasApi.Data;
using ApostasApi.DTOs.Apostas;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Services;

public class ApostaService : IApostaService
{
    private readonly IDbExecutor _db;

    public ApostaService(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<ApiResult<object>> CriarAsync(CriarApostaDto dto)
    {
        var parameters = new List<IDataParameter>
        {
           new SqlParameter("@Jogo_Id", dto.JogoId),
        new SqlParameter("@Utilizador_Id", dto.UtilizadorId),
        new SqlParameter("@Tipo_Aposta", dto.TipoAposta),
        new SqlParameter("@Montante", dto.Montante),
        new SqlParameter("@Odd", dto.Odd)
    };

    var table = await _db.QueryAsync("SP_Inserir_Aposta", parameters);
    return ApiResult<object>.Ok(table, "Aposta registada com sucesso.");
    }

    public async Task<ApiResult<object>> ListarAsync(int? idUtilizador, string? codigoJogo, int? estado, DateTime? inicio, DateTime? fim)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@IdUtilizador", idUtilizador ?? (object)DBNull.Value),
            new SqlParameter("@CodigoJogo", codigoJogo ?? (object)DBNull.Value),
            new SqlParameter("@EstadoAposta", estado ?? (object)DBNull.Value),
            new SqlParameter("@DataInicio", inicio ?? (object)DBNull.Value),
            new SqlParameter("@DataFim", fim ?? (object)DBNull.Value)
        };

        var table = await _db.QueryAsync("sp_Aposta_Listar", parameters);
        return ApiResult<object>.Ok(table, "Apostas obtidas com sucesso.");
    }

    public async Task<ApiResult<object>> ObterAsync(int id)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@IdAposta", id)
        };

        var table = await _db.QueryAsync("sp_Aposta_ObterPorId", parameters);
        return ApiResult<object>.Ok(table, "Aposta obtida com sucesso.");
    }

    public async Task<ApiResult<object>> CancelarAsync(int id)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@IdAposta", id)
        };

        await _db.ExecuteAsync("SP_Cancelar_Aposta", parameters);
    return ApiResult<object>.Ok(null, "Aposta cancelada com sucesso.");
    }
}