using ApostasApi.Data;
using ApostasApi.DTOs.Estatisticas;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Services;

public class EstatisticaService : IEstatisticaService
{
    private readonly IDbExecutor _db;

    public EstatisticaService(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<ApiResult<EstatisticaJogoDto>> ObterPorJogoAsync(string codigoJogo)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@CodigoJogo", codigoJogo)
        };

        var table = await _db.QueryAsync("sp_Estatistica_ObterPorJogo", parameters);
        if (table.Rows.Count == 0)
            return ApiResult<EstatisticaJogoDto>.Fail("Sem dados para o jogo indicado.");

        var row = table.Rows[0];
        var dto = new EstatisticaJogoDto
        {
            CodigoJogo = row["CodigoJogo"].ToString() ?? string.Empty,
            TotalApostado = Convert.ToDecimal(row["TotalApostado"]),
            ApostasTipo1 = Convert.ToInt32(row["ApostasTipo1"]),
            ApostasTipoX = Convert.ToInt32(row["ApostasTipoX"]),
            ApostasTipo2 = Convert.ToInt32(row["ApostasTipo2"]),
            Pendentes = Convert.ToInt32(row["Pendentes"]),
            Ganhas = Convert.ToInt32(row["Ganhas"]),
            Perdidas = Convert.ToInt32(row["Perdidas"]),
            Anuladas = Convert.ToInt32(row["Anuladas"]),
            MargemPlataforma = Convert.ToDecimal(row["MargemPlataforma"])
        };

        return ApiResult<EstatisticaJogoDto>.Ok(dto, "Estatísticas do jogo obtidas com sucesso.");
    }

     public async Task<ApiResult<EstatisticaCompeticaoDto>> ObterPorCompeticaoAsync(string competicao)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Competicao", competicao)
        };

        var table = await _db.QueryAsync("sp_Estatistica_ObterPorCompeticao", parameters);
        if (table.Rows.Count == 0)
            return ApiResult<EstatisticaCompeticaoDto>.Fail("Sem dados para a competição indicada.");

        var row = table.Rows[0];
        var dto = new EstatisticaCompeticaoDto
        {
            Competicao = row["Competicao"].ToString() ?? string.Empty,
            MediaGolosPorJogo = Convert.ToDecimal(row["MediaGolosPorJogo"]),
            VolumeTotalApostado = Convert.ToDecimal(row["VolumeTotalApostado"]),
            TaxaVitoria1 = Convert.ToDecimal(row["TaxaVitoria1"]),
            TaxaVitoriaX = Convert.ToDecimal(row["TaxaVitoriaX"]),
            TaxaVitoria2 = Convert.ToDecimal(row["TaxaVitoria2"])
        };

        return ApiResult<EstatisticaCompeticaoDto>.Ok(dto, "Estatísticas da competição obtidas com sucesso.");
    }
}