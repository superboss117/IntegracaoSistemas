using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Events.Models;

namespace Shared.Events.Services;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var clientId = configuration["Kafka:ClientId"] ?? "default-client";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = clientId,
            MessageTimeoutMs = 3000,
            SocketTimeoutMs = 3000
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, EventEnvelope<T> evento, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageString = JsonSerializer.Serialize(evento);
            var message = new Message<Null, string> { Value = messageString };

            // Using Produce instead of ProduceAsync to avoid blocking the caller if the broker is down
            _producer.Produce(topic, message, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                {
                    _logger.LogError($"Delivery failed: {deliveryReport.Error.Reason}");
                }
                else
                {
                    _logger.LogInformation($"Event {evento.EventId} of type {evento.EventType} delivered to '{deliveryReport.TopicPartitionOffset}'");
                }
            });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while publishing the event.");
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
