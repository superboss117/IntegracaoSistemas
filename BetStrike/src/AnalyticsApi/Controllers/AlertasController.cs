using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace AnalyticsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertasController : ControllerBase
{
    private readonly string _connectionString;

    public AlertasController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") 
            ?? throw new InvalidOperationException("Connection string 'AnalyticsConnection' not found.");
    }

    [HttpGet]
    public async Task<IActionResult> GetAlertas()
    {
        using var connection = new SqlConnection(_connectionString);
        var alertas = await connection.QueryAsync("SELECT Id, Tipo, Nivel, Detalhes, DataHora FROM Alertas ORDER BY DataHora DESC");
        return Ok(alertas);
    }
}
