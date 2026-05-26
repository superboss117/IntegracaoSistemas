using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Models;

namespace WorkersRabbitMq;

public class WorkerNotificacoes : BackgroundService
{
    private readonly ILogger<WorkerNotificacoes> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public WorkerNotificacoes(ILogger<WorkerNotificacoes> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            var dlxName = "apostas.dlx";
            _channel.ExchangeDeclare(exchange: dlxName, type: ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(queue: "fila-dead-letter", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "fila-dead-letter", exchange: dlxName, routingKey: "dead-letter");
            
            var queueArgs = new System.Collections.Generic.Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", "dead-letter" }
            };
            _channel.QueueDeclare(queue: "fila-notificacoes", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

            _logger.LogInformation("WorkerNotificacoes connected to RabbitMQ.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not initialize RabbitMQ connection for WorkerNotificacoes.");
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return Task.CompletedTask;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageString = Encoding.UTF8.GetString(body);
            bool success = false;
            int retries = 0;

            while (!success && retries <= 3)
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<MessageEnvelope<EnviarNotificacaoCommand>>(messageString);
                    
                    if (envelope != null)
                    {
                        _logger.LogInformation($"[Notificacoes] Notifying user {envelope.Payload.UtilizadorId}: {envelope.Payload.Mensagem}");
                    }

                    success = true;
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    retries++;
                    _logger.LogWarning(ex, $"[Notificacoes] Error processing message. Retry {retries}/3.");
                    if (retries <= 3)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            if (!success)
            {
                _logger.LogError("[Notificacoes] Max retries reached. Sending to Dead Letter Queue.");
                _channel.BasicReject(ea.DeliveryTag, false);
            }
        };

        _channel.BasicConsume(queue: "fila-notificacoes", autoAck: false, consumer: consumer);
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
