using AnalyticsWorker;
using Shared.Messaging.Options;
using Shared.Messaging.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<KafkaAnalyticsWorker>();

var host = builder.Build();
host.Run();
