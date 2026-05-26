using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AnalyticsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly string _connectionString;

    public DashboardController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") 
            ?? throw new InvalidOperationException("Connection string 'AnalyticsConnection' not found.");
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> GetResumo()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var totalVolume = await connection.ExecuteScalarAsync<decimal?>("SELECT SUM(VolumeApostado) FROM MetricasJogo") ?? 0m;
        var totalApostas = await connection.ExecuteScalarAsync<int?>("SELECT SUM(NumeroApostas) FROM MetricasJogo") ?? 0;
        
        var jogoMaiorExposicao = await connection.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 CodigoJogo FROM MetricasJogo ORDER BY Exposicao DESC");
        
        var alertasAtivos = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Alertas");

        return Ok(new
        {
            TotalApostado = totalVolume,
            NumeroTotalApostas = totalApostas,
            JogoMaiorExposicao = jogoMaiorExposicao,
            NumeroAlertasAtivos = alertasAtivos,
            TimestampUltimaAtualizacao = DateTime.UtcNow
        });
    }

    [HttpGet("jogos")]
    public async Task<IActionResult> GetJogos()
    {
        using var connection = new SqlConnection(_connectionString);
        var jogos = await connection.QueryAsync("SELECT CodigoJogo, VolumeApostado, NumeroApostas, Exposicao, 0 as Margem, NULL as Estado FROM MetricasJogo");
        return Ok(jogos);
    }

    [HttpGet("live")]
    public async Task<IActionResult> GetLive()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var totalVolume = await connection.ExecuteScalarAsync<decimal?>("SELECT SUM(VolumeApostado) FROM MetricasJogo") ?? 0m;
        var totalApostas = await connection.ExecuteScalarAsync<int?>("SELECT SUM(NumeroApostas) FROM MetricasJogo") ?? 0;
        var jogoMaiorExposicao = await connection.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 CodigoJogo FROM MetricasJogo ORDER BY Exposicao DESC") ?? "N/A";
        var alertasAtivos = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Alertas");

        var resumo = new
        {
            TotalApostado = totalVolume,
            NumeroTotalApostas = totalApostas,
            JogoMaiorExposicao = jogoMaiorExposicao,
            NumeroAlertasAtivos = alertasAtivos,
            TimestampUltimaAtualizacao = DateTime.UtcNow
        };

        var jogos = await connection.QueryAsync("SELECT CodigoJogo, VolumeApostado, NumeroApostas, Exposicao, 0 as Margem, NULL as Estado FROM MetricasJogo");
        var apostasPorMinuto = await connection.QueryAsync("SELECT Minuto, VolumeApostado, NumeroApostas FROM MetricasApostasPorMinuto ORDER BY Minuto DESC");
        var alertasRecentes = await connection.QueryAsync("SELECT Id, Tipo, Nivel, Detalhes, DataHora FROM Alertas ORDER BY DataHora DESC");

        return Ok(new
        {
            Resumo = resumo,
            Jogos = jogos,
            ApostasPorMinuto = apostasPorMinuto,
            AlertasRecentes = alertasRecentes
        });
    }
}
