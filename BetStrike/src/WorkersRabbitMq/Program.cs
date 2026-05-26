using WorkersRabbitMq;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerAuditoria>();
builder.Services.AddHostedService<WorkerNotificacoes>();
builder.Services.AddHostedService<WorkerAlertas>();

var host = builder.Build();
host.Run();
