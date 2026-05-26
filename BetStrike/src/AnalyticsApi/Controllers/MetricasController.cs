using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace AnalyticsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricasController : ControllerBase
{
    private readonly string _connectionString;

    public MetricasController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") 
            ?? throw new InvalidOperationException("Connection string 'AnalyticsConnection' not found.");
    }

    [HttpGet("apostas-por-minuto")]
    public async Task<IActionResult> GetApostasPorMinuto()
    {
        using var connection = new SqlConnection(_connectionString);
        var metricas = await connection.QueryAsync("SELECT Minuto, VolumeApostado, NumeroApostas FROM MetricasApostasPorMinuto ORDER BY Minuto DESC");
        return Ok(metricas);
    }
}
