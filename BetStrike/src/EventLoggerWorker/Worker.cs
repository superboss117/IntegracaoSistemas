using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventLoggerWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "event-logger-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        int attempts = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            attempts++;
            int backoff = attempts == 1 ? 2 : (attempts == 2 ? 5 : 10);
            _logger.LogInformation($"Tentativa {attempts} de ligação ao Kafka/Redpanda (EventLoggerWorker)...");
            try
            {
                consumer.Subscribe(new[] { "jogos-events", "apostas-events" });
                _logger.LogInformation("Ligação ao Kafka/Redpanda (EventLoggerWorker) bem-sucedida! Subscrito nos tópicos: jogos-events, apostas-events");
                break;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning($"Falha temporária de ligação ao Kafka/Redpanda (EventLoggerWorker): {ex.Message}. Aplicando backoff de {backoff}s antes de re-tentar...");
                await Task.Delay(backoff * 1000, stoppingToken);
            }
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                _logger.LogInformation($"[Topic: {consumeResult.Topic}] Received message: {consumeResult.Message.Value}");
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}
