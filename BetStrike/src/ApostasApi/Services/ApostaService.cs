using ApostasApi.Data;
using ApostasApi.DTOs.Apostas;
using ApostasApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Shared.Events.Models;
using Shared.Events.Services;
using Shared.Messaging.Services;
using Shared.Messaging.Models;

namespace ApostasApi.Services;

public class ApostaService : IApostaService
{
    private readonly IDbExecutor _db;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRabbitMqPublisher _rabbitPublisher;

    public ApostaService(IDbExecutor db, IEventPublisher eventPublisher, IRabbitMqPublisher rabbitPublisher)
    {
        _db = db;
        _eventPublisher = eventPublisher;
        _rabbitPublisher = rabbitPublisher;
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

        object? resultadoFinal = null;

        if (table.Rows.Count > 0)
        {
            var apostaId = table.Columns.Contains("Id_Aposta") 
                ? Convert.ToInt32(table.Rows[0]["Id_Aposta"]) 
                : Convert.ToInt32(table.Rows[0][0]); // fallback to first column

            resultadoFinal = new
            {
                ApostaId = apostaId,
                UtilizadorId = dto.UtilizadorId,
                JogoId = dto.JogoId,
                TipoAposta = dto.TipoAposta,
                Montante = dto.Montante,
                Odd = dto.Odd,
                Estado = 1
            };

            try
            {
                var apostaCriada = new ApostaCriadaEvent
                {
                    ApostaId = apostaId,
                    UtilizadorId = dto.UtilizadorId,
                    CodigoJogo = dto.JogoId.ToString(),
                    TipoAposta = dto.TipoAposta,
                    Montante = dto.Montante,
                    Odd = dto.Odd
                };

                await _eventPublisher.PublishAsync("apostas-events", new EventEnvelope<ApostaCriadaEvent>
                {
                    EventType = nameof(ApostaCriadaEvent),
                    Source = "ApostasApi",
                    Payload = apostaCriada
                });

                var auditoriaCmd = new AuditarAcaoCommand
                {
                    Acao = "CriarAposta",
                    Detalhes = $"Aposta {apostaId} criada para o jogo {dto.JogoId} no montante de {dto.Montante}",
                    Utilizador = dto.UtilizadorId.ToString()
                };

                _rabbitPublisher.Publish("fila-auditoria", new MessageEnvelope<AuditarAcaoCommand>
                {
                    MessageType = nameof(AuditarAcaoCommand),
                    Source = "ApostasApi",
                    Payload = auditoriaCmd
                });
            }
            catch (Exception ex)
            {
                // Registar erro, mas não quebrar a API pois a aposta já foi criada na BD
                Console.WriteLine($"Erro ao publicar eventos/comandos da aposta {apostaId}: {ex.Message}");
            }
        }

        return ApiResult<object>.Ok(resultadoFinal ?? new object(), "Aposta registada com sucesso.");
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
        
        var list = new List<Dictionary<string, object?>>();
        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in table.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            list.Add(dict);
        }

        return ApiResult<object>.Ok(list, "Apostas obtidas com sucesso.");
    }

    public async Task<ApiResult<object>> ObterAsync(int id)
    {
        var parameters = new List<IDataParameter>
        {
            new SqlParameter("@Aposta_Id", id)
        };

        var table = await _db.QueryAsync("SP_Obter_Aposta", parameters);
        
        if (table.Rows.Count == 0)
        {
            return ApiResult<object>.Fail("Aposta não encontrada.");
        }

        var dict = new Dictionary<string, object?>();
        foreach (DataColumn col in table.Columns)
        {
            dict[col.ColumnName] = table.Rows[0][col] == DBNull.Value ? null : table.Rows[0][col];
        }

        return ApiResult<object>.Ok(dict, "Aposta obtida com sucesso.");
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