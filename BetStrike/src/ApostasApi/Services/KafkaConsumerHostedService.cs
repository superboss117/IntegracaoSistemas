using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Events.Models;

namespace ApostasApi.Services;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public KafkaConsumerHostedService(
        ILogger<KafkaConsumerHostedService> logger, 
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "redpanda:9092",
            GroupId = "apostas-api-sync-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("jogos-events");

        _logger.LogInformation("ApostasApi Background Sync: Subscribed to topic 'jogos-events'");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result == null) continue;

                    _logger.LogInformation($"[Sync] Event received from Kafka: {result.Topic}");

                    using var scope = _serviceProvider.CreateScope();
                    var jogoService = scope.ServiceProvider.GetRequiredService<IJogoService>();

                    using var doc = JsonDocument.Parse(result.Message.Value);
                    var root = doc.RootElement;
                    var eventType = root.GetProperty("EventType").GetString();

                    if (eventType == nameof(JogoCriadoEvent))
                    {
                        var envelope = JsonSerializer.Deserialize<EventEnvelope<JogoCriadoEvent>>(result.Message.Value);
                        if (envelope != null)
                        {
                            await ProcessarJogoCriado(envelope.Payload, jogoService);
                        }
                    }
                    else if (eventType == nameof(JogoAtualizadoEvent) || eventType == nameof(JogoFinalizadoEvent))
                    {
                        // Aqui poderíamos processar atualizações de estado ou golos
                        _logger.LogInformation($"[Sync] Event {eventType} received but specific sync logic for update is pending.");
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError($"[Sync] Consume error: {e.Error.Reason}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Sync] Error processing Kafka message.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }

    private async Task ProcessarJogoCriado(JogoCriadoEvent ev, IJogoService service)
    {
        try 
        {
            var existe = await service.ObterJogoAsync(ev.CodigoJogo);
            if (existe == null)
            {
                _logger.LogInformation($"[Sync] Creating new game from event: {ev.CodigoJogo}");
                await service.CriarJogoAsync(new DTOs.Jogos.CriarJogoDto
                {
                    CodigoJogo = ev.CodigoJogo,
                    EquipaCasa = ev.EquipaCasa,
                    EquipaFora = ev.EquipaFora,
                    DataHora = ev.DataHora,
                    Competicao = "Sincronizado via Kafka",
                    Estado = 1
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Sync] Failed to sync game {ev.CodigoJogo}: {ex.Message}");
        }
    }
}
