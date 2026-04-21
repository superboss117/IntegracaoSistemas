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

   public async Task<ApiResult<UtilizadorResponseDto>> CriarAsync(CriarUtilizadorDto dto)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Nome", dto.Nome),
            new SqlParameter("@Email", dto.Email)
        };

        var table = await _db.QueryAsync("SP_Criar_Utilizador", parameters);

        if (table.Rows.Count == 0)
            return ApiResult<UtilizadorResponseDto>.Fail("Não foi possível criar o utilizador.");

        var row = table.Rows[0];

        var response = new UtilizadorResponseDto
        {
            UtilizadorId = Convert.ToInt32(row["Utilizador_Id"]),
            Nome = row["Nome"]?.ToString() ?? string.Empty,
            Email = row["Email"]?.ToString() ?? string.Empty,
            SaldoInicial = Convert.ToDecimal(row["Saldo_Inicial"])
        };

        return ApiResult<UtilizadorResponseDto>.Ok(response, "Utilizador criado com sucesso.");
    }
}