using ApostasApi.Data;
using ApostasApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDbExecutor, SqlDbExecutor>();
builder.Services.AddScoped<IJogoService, JogoService>();
builder.Services.AddScoped<IApostaService, ApostaService>();
builder.Services.AddScoped<IResultadoService, ResultadoService>();
builder.Services.AddScoped<IUtilizadorService, UtilizadorService>();
builder.Services.AddScoped<IEstatisticaService, EstatisticaService>();

builder.Services.AddHttpClient<SincronizacaoService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();