using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Messaging.Models;
using Shared.Messaging.Options;

namespace Shared.Messaging.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Direct, durable: true);

            // Configure DLQ
            var dlxName = "apostas.dlx";
            _channel.ExchangeDeclare(exchange: dlxName, type: ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(queue: "fila-dead-letter", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "fila-dead-letter", exchange: dlxName, routingKey: "dead-letter");

            var queueArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", "dead-letter" }
            };

            // Declare Queues with DLQ settings
            _channel.QueueDeclare(queue: "fila-auditoria", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueDeclare(queue: "fila-notificacoes", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueDeclare(queue: "fila-alertas", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

            // Bind queues
            _channel.QueueBind(queue: "fila-auditoria", exchange: _options.ExchangeName, routingKey: "fila-auditoria");
            _channel.QueueBind(queue: "fila-notificacoes", exchange: _options.ExchangeName, routingKey: "fila-notificacoes");
            _channel.QueueBind(queue: "fila-alertas", exchange: _options.ExchangeName, routingKey: "fila-alertas");

            _logger.LogInformation("RabbitMQ initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not initialize RabbitMQ connection.");
        }
    }

    public void Publish<T>(string routingKey, MessageEnvelope<T> message)
    {
        if (_channel == null || _channel.IsClosed)
        {
            InitializeRabbitMq();
        }

        if (_channel == null)
        {
            _logger.LogWarning("Cannot publish message, RabbitMQ channel is null.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"Message published to {routingKey}. MessageId: {message.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish message to {routingKey}.");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
