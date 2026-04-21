using ApostasApi.Data;
using ApostasApi.DTOs.Jogos;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace ApostasApi.Services;

public class JogoService : IJogoService
{
    private readonly IDbExecutor _db;
    private static readonly Regex CodigoRegex = new("^FUT-\\d{4}-\\d{4}$", RegexOptions.Compiled);

    public JogoService(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<ApiResult<object>> CriarAsync(CriarJogoDto dto)
    {
        if (!CodigoRegex.IsMatch(dto.CodigoJogo))
            return ApiResult<object>.Fail("Formato de código inválido. Use FUT-AAAA-JJNN.");

        var parameters = new List<IDataParameter>
    {
        new SqlParameter("@Codigo_Jogo", dto.CodigoJogo),
        new SqlParameter("@Data_Hora", dto.DataHora),
        new SqlParameter("@Equipa_Casa", dto.EquipaCasa),
        new SqlParameter("@Equipa_Fora", dto.EquipaFora),
        new SqlParameter("@Competicao", dto.Competicao),
        new SqlParameter("@Estado", dto.Estado)
    };

    var table = await _db.QueryAsync("SP_Inserir_Jogo", parameters);
    return ApiResult<object>.Ok(table, "Jogo criado com sucesso.");
    }

public async Task<ApiResult<object>> AtualizarAsync(string codigoJogo, AtualizarJogoDto dto)
{
    var parameters = new List<IDataParameter>
    {
        new SqlParameter("@Codigo_Jogo", codigoJogo),
        new SqlParameter("@Novo_Estado", dto.NovoEstado),
        new SqlParameter("@Golos_Casa", (object?)dto.GolosCasa ?? DBNull.Value),
        new SqlParameter("@Golos_Fora", (object?)dto.GolosFora ?? DBNull.Value)
    };

    await _db.ExecuteAsync("SP_Atualizar_Jogo", parameters);
    return ApiResult<object>.Ok(null, "Jogo atualizado com sucesso.");
}
    public async Task<ApiResult<object>> ListarAsync(DateOnly? data, int? estado, string? competicao)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@DataJogo", data.HasValue ? data.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value),
            new SqlParameter("@EstadoJogo", estado ?? (object)DBNull.Value),
            new SqlParameter("@Competicao", competicao ?? (object)DBNull.Value)
        };

        var table = await _db.QueryAsync("sp_Jogo_Listar", parameters);
        return ApiResult<object>.Ok(table, "Jogos obtidos com sucesso.");
    }

    public async Task<ApiResult<object>> ObterAsync(string codigoJogo)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@CodigoJogo", codigoJogo)
        };

        var table = await _db.QueryAsync("sp_Jogo_ObterPorCodigo", parameters);
        return ApiResult<object>.Ok(table, "Jogo obtido com sucesso.");
    }


    public async Task<ApiResult<object>> RemoverAsync(string codigoJogo)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@CodigoJogo", codigoJogo)
        };

        await _db.ExecuteAsync("sp_Jogo_Remover", parameters);
        return ApiResult<object>.Ok(null, "Jogo removido com sucesso.");
    }
}