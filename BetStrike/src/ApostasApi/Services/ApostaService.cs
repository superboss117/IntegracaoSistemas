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
        int? jogoId = null;

        if (!string.IsNullOrWhiteSpace(codigoJogo))
        {
            var jogoParams = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo)
            };

            var jogoTable = await _db.QueryAsync("SP_Obter_Jogo", jogoParams);

            if (jogoTable.Rows.Count == 0)
                return ApiResult<object>.Fail("Jogo não encontrado.");

            jogoId = Convert.ToInt32(jogoTable.Rows[0]["Id"]);
        }

        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Utilizador_Id", (object?)idUtilizador ?? DBNull.Value),
            new SqlParameter("@Jogo_Id", (object?)jogoId ?? DBNull.Value),
            new SqlParameter("@Estado", (object?)estado ?? DBNull.Value),
            new SqlParameter("@Data_Inicio", (object?)inicio ?? DBNull.Value),
            new SqlParameter("@Data_Fim", (object?)fim ?? DBNull.Value)
        };

        var table = await _db.QueryAsync("SP_Listar_Apostas", parameters);
        return ApiResult<object>.Ok(table, "Apostas obtidas com sucesso.");
    }

    public async Task<ApiResult<object>> ObterAsync(int id)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Aposta_Id", id)
        };

        var table = await _db.QueryAsync("SP_Obter_Aposta", parameters);
        return ApiResult<object>.Ok(table, "Aposta obtida com sucesso.");
    }

    public async Task<ApiResult<object>> CancelarAsync(int id)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Aposta_Id", id)
        };

        await _db.ExecuteAsync("SP_Cancelar_Aposta", parameters);
        return ApiResult<object>.Ok(null, "Aposta cancelada com sucesso.");
    }
}