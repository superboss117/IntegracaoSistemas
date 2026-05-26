using ApostasApi.Data;
using ApostasApi.Services;
using Shared.Events.Services;
using Shared.Messaging.Services;
using Shared.Messaging.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHttpClient<SincronizacaoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ResultadosApiUrl"] ?? "http://resultadosapi:8080");
});

builder.Services.AddScoped<IDbExecutor, SqlDbExecutor>();
builder.Services.AddScoped<IApostaService, ApostaService>();
builder.Services.AddScoped<IUtilizadorService, UtilizadorService>();
builder.Services.AddScoped<IJogoService, JogoService>();
builder.Services.AddScoped<IEstatisticaService, EstatisticaService>();
builder.Services.AddScoped<SincronizacaoService>();
builder.Services.AddScoped<IResultadoService, ResultadoService>();

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();