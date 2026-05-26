using ResultadosAPI.Services;
using Shared.Events.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddScoped<JogoService>();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();