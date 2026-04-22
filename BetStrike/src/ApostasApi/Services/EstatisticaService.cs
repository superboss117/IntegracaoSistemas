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
            new SqlParameter("@Codigo_Jogo", codigoJogo)
        };

        var table = await _db.QueryAsync("SP_Estatisticas_Jogo", parameters);

        if (table.Rows.Count == 0)
            return ApiResult<EstatisticaJogoDto>.Fail("Sem dados para o jogo indicado.");

        var row = table.Rows[0];

        var dto = new EstatisticaJogoDto
        {
            CodigoJogo = row["Codigo_Jogo"].ToString() ?? string.Empty,
            TotalApostado = Convert.ToDecimal(row["Volume_Total"]),
            ApostasTipo1 = Convert.ToInt32(row["Apostas_Tipo_1"]),
            ApostasTipoX = Convert.ToInt32(row["Apostas_Tipo_X"]),
            ApostasTipo2 = Convert.ToInt32(row["Apostas_Tipo_2"]),
            Pendentes = Convert.ToInt32(row["Apostas_Pendentes"]),
            Ganhas = Convert.ToInt32(row["Apostas_Ganhas"]),
            Perdidas = Convert.ToInt32(row["Apostas_Perdidas"]),
            Anuladas = Convert.ToInt32(row["Apostas_Anuladas"]),
            MargemPlataforma = Convert.ToDecimal(row["Margem_Plataforma"])
        };

        return ApiResult<EstatisticaJogoDto>.Ok(dto, "Estatísticas do jogo obtidas com sucesso.");
    }

    public async Task<ApiResult<EstatisticaCompeticaoDto>> ObterPorCompeticaoAsync(string competicao)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Competicao", competicao)
        };

        var table = await _db.QueryAsync("SP_Estatisticas_Competicao", parameters);

        if (table.Rows.Count == 0)
            return ApiResult<EstatisticaCompeticaoDto>.Fail("Sem dados para a competição indicada.");

        var row = table.Rows[0];

        var dto = new EstatisticaCompeticaoDto
        {
            Competicao = row["Competicao"].ToString() ?? string.Empty,
            MediaGolosPorJogo = Convert.ToDecimal(row["Media_Golos_Por_Jogo"]),
            VolumeTotalApostado = Convert.ToDecimal(row["Volume_Total_Apostado"]),
            TaxaVitoria1 = Convert.ToDecimal(row["Taxa_Vitoria_1"]),
            TaxaVitoriaX = Convert.ToDecimal(row["Taxa_Vitoria_X"]),
            TaxaVitoria2 = Convert.ToDecimal(row["Taxa_Vitoria_2"])
        };

        return ApiResult<EstatisticaCompeticaoDto>.Ok(dto, "Estatísticas da competição obtidas com sucesso.");
    }
}