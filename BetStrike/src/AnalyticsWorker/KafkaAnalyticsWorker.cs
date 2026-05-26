using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Events.Models;
using Shared.Messaging.Models;
using Shared.Messaging.Services;

namespace AnalyticsWorker;

public class KafkaAnalyticsWorker : BackgroundService
{
    private readonly ILogger<KafkaAnalyticsWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqPublisher _rabbitPublisher;
    private readonly string _connectionString;

    public KafkaAnalyticsWorker(ILogger<KafkaAnalyticsWorker> logger, IConfiguration configuration, IRabbitMqPublisher rabbitPublisher)
    {
        _logger = logger;
        _configuration = configuration;
        _rabbitPublisher = rabbitPublisher;
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") 
            ?? throw new InvalidOperationException("Connection string 'AnalyticsConnection' not found.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "analytics-worker",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        // Wait until Kafka is ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                consumer.Subscribe(new[] { "apostas-events", "jogos-events" });
                _logger.LogInformation("AnalyticsWorker subscribed to topics: apostas-events, jogos-events");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka not ready yet. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var messageString = consumeResult.Message.Value;
                    
                    _logger.LogInformation($"[Analytics] Received message from {consumeResult.Topic}");
                    
                    using var doc = JsonDocument.Parse(messageString);
                    var root = doc.RootElement;
                    var eventType = root.GetProperty("EventType").GetString();
                    var eventIdStr = root.GetProperty("EventId").GetString();
                    
                    if (Guid.TryParse(eventIdStr, out var eventId))
                    {
                        if (await IsEventProcessedAsync(eventId))
                        {
                            _logger.LogInformation($"[Analytics] Event {eventId} already processed. Skipping.");
                            continue;
                        }

                        if (eventType == nameof(ApostaCriadaEvent))
                        {
                            var envelope = JsonSerializer.Deserialize<EventEnvelope<ApostaCriadaEvent>>(messageString);
                            if (envelope != null)
                                await ProcessarApostaCriadaAsync(envelope.Payload, eventId, eventType);
                        }
                        else if (eventType == nameof(JogoCriadoEvent) || eventType == nameof(JogoAtualizadoEvent) || eventType == nameof(JogoFinalizadoEvent))
                        {
                            // Could process if needed, but not strictly required for the requested dashboard other than tracking events
                            await MarkEventProcessedAsync(eventId, eventType);
                        }
                        else
                        {
                            await MarkEventProcessedAsync(eventId, eventType ?? "Unknown");
                        }
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError($"Consume error: {e.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }

    private async Task<bool> IsEventProcessedAsync(Guid eventId)
    {
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM EventosProcessados WHERE EventId = @EventId", new { EventId = eventId });
        return count > 0;
    }

    private async Task MarkEventProcessedAsync(Guid eventId, string eventType)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "INSERT INTO EventosProcessados (EventId, EventType, ConsumerName) VALUES (@EventId, @EventType, 'AnalyticsWorker')",
            new { EventId = eventId, EventType = eventType });
    }

    private async Task ProcessarApostaCriadaAsync(ApostaCriadaEvent payload, Guid eventId, string eventType)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Atualizar MetricasJogo
            var jogoQuery = "SELECT COUNT(1) FROM MetricasJogo WHERE CodigoJogo = @CodigoJogo";
            var exists = await connection.ExecuteScalarAsync<int>(jogoQuery, new { CodigoJogo = payload.CodigoJogo }, transaction);

            if (exists == 0)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO MetricasJogo (CodigoJogo, VolumeApostado, NumeroApostas, Exposicao) VALUES (@CodigoJogo, @Montante, 1, @Exposicao)",
                    new { CodigoJogo = payload.CodigoJogo, Montante = payload.Montante, Exposicao = payload.Montante * payload.Odd }, transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    "UPDATE MetricasJogo SET VolumeApostado = VolumeApostado + @Montante, NumeroApostas = NumeroApostas + 1, Exposicao = Exposicao + @Exposicao WHERE CodigoJogo = @CodigoJogo",
                    new { CodigoJogo = payload.CodigoJogo, Montante = payload.Montante, Exposicao = payload.Montante * payload.Odd }, transaction);
            }

            // 2. Atualizar MetricasApostasPorMinuto
            var minuto = DateTime.UtcNow;
            minuto = new DateTime(minuto.Year, minuto.Month, minuto.Day, minuto.Hour, minuto.Minute, 0);

            var minQuery = "SELECT COUNT(1) FROM MetricasApostasPorMinuto WHERE Minuto = @Minuto";
            var minExists = await connection.ExecuteScalarAsync<int>(minQuery, new { Minuto = minuto }, transaction);

            if (minExists == 0)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO MetricasApostasPorMinuto (Minuto, VolumeApostado, NumeroApostas) VALUES (@Minuto, @Montante, 1)",
                    new { Minuto = minuto, Montante = payload.Montante }, transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    "UPDATE MetricasApostasPorMinuto SET VolumeApostado = VolumeApostado + @Montante, NumeroApostas = NumeroApostas + 1 WHERE Minuto = @Minuto",
                    new { Minuto = minuto, Montante = payload.Montante }, transaction);
            }

            // 3. Mark event processed
            await connection.ExecuteAsync(
                "INSERT INTO EventosProcessados (EventId, EventType, ConsumerName) VALUES (@EventId, @EventType, 'AnalyticsWorker')",
                new { EventId = eventId, EventType = eventType }, transaction);

            transaction.Commit();

            // 4. Verificar Alerta
            var totalVolume = await connection.ExecuteScalarAsync<decimal>(
                "SELECT VolumeApostado FROM MetricasJogo WHERE CodigoJogo = @CodigoJogo", new { CodigoJogo = payload.CodigoJogo });

            if (totalVolume > 1000)
            {
                var alertaDetails = $"Volume apostado no jogo {payload.CodigoJogo} ultrapassou 1000 EUR. Volume Atual: {totalVolume}";
                
                var alertaId = await connection.ExecuteScalarAsync<int>(
                    "INSERT INTO Alertas (Tipo, Nivel, Detalhes) OUTPUT INSERTED.Id VALUES ('EXPOSICAO_ELEVADA', 'CRITICO', @Detalhes)",
                    new { Detalhes = alertaDetails });

                _logger.LogWarning($"[ALERTA GERADO] {alertaDetails}");

                var command = new ProcessarAlertaCommand
                {
                    Alerta = alertaDetails,
                    Nivel = "CRITICO"
                };

                _rabbitPublisher.Publish("fila-alertas", new MessageEnvelope<ProcessarAlertaCommand>
                {
                    MessageType = nameof(ProcessarAlertaCommand),
                    Source = "AnalyticsWorker",
                    Payload = command
                });
            }
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Error processing ApostaCriadaEvent.");
            throw; // Let it retry or fail
        }
    }
}
