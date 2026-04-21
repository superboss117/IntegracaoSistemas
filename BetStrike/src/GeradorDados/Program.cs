using System.Text;
using System.Text.Json;

var equipas = new List<string>
{
    "Benfica", "Porto", "Sporting CP", "Braga", "Guimarães",
    "Famalicão", "Estoril", "Casa Pia", "Moreirense", "Arouca",
    "Rio Ave", "Boavista", "Farense", "Gil Vicente", "Santa Clara",
    "Estrela Amadora", "Nacional", "AVS"
};

const string BASE_URL = "https://localhost:7159";
const int NUMERO_JORNADA = 1;
const int INTERVALO_SEGUNDOS = 10;

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};
var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BASE_URL) };
var random = new Random();

Console.WriteLine("=== BetStrike — Gerador de Dados ===");
Console.WriteLine($"A gerar jornada {NUMERO_JORNADA}...\n");

var equipasBaralhadas = equipas.OrderBy(_ => random.Next()).ToList();
var jogos = new List<(string casa, string fora, string codigo)>();

for (int i = 0; i < 9; i++)
{
    var casa = equipasBaralhadas[i * 2];
    var fora = equipasBaralhadas[i * 2 + 1];
    var codigo = $"FUT-{DateTime.Now.Year}-{NUMERO_JORNADA:D2}{(i + 1):D2}";
    jogos.Add((casa, fora, codigo));
}

Console.WriteLine("Jogos desta jornada:");
foreach (var j in jogos)
    Console.WriteLine($"  {j.codigo}: {j.casa} vs {j.fora}");
Console.WriteLine();

foreach (var jogo in jogos)
{
    var payload = new
    {
        codigo_Jogo = jogo.codigo,
        data_Jogo = DateTime.Now.ToString("yyyy-MM-dd"),
        hora_Inicio = DateTime.Now.ToString("HH:mm:ss"),
        equipa_Casa = jogo.casa,
        equipa_Fora = jogo.fora
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync("/api/Jogos", content);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($"[OK] Jogo inserido: {jogo.codigo}");
    else
        Console.WriteLine($"[ERRO] {jogo.codigo}: {await response.Content.ReadAsStringAsync()}");
}

Console.WriteLine("\nA iniciar simulação dos jogos em paralelo...\n");

var tarefas = jogos.Select(jogo => SimularJogoAsync(jogo.codigo, jogo.casa, jogo.fora)).ToList();
await Task.WhenAll(tarefas);

Console.WriteLine("\n=== Simulação concluída! ===");

async Task SimularJogoAsync(string codigo, string casa, string fora)
{
    var random = new Random();
    int golosCasa = 0, golosFora = 0;
    int passosSimulacao = 9;

    await AtualizarJogoAsync(codigo, 2, golosCasa, golosFora);
    Console.WriteLine($"[{codigo}] A decorrer: {casa} {golosCasa}-{golosFora} {fora}");

    for (int passo = 0; passo < passosSimulacao; passo++)
    {
        await Task.Delay(INTERVALO_SEGUNDOS * 1000);
        if (random.NextDouble() < 0.15) golosCasa++;
        if (random.NextDouble() < 0.15) golosFora++;
        await AtualizarJogoAsync(codigo, 2, golosCasa, golosFora);
        Console.WriteLine($"[{codigo}] Min {(passo + 1) * 10}: {casa} {golosCasa}-{golosFora} {fora}");
    }

    await AtualizarJogoAsync(codigo, 3, golosCasa, golosFora);
    Console.WriteLine($"[{codigo}] FINALIZADO: {casa} {golosCasa}-{golosFora} {fora}");
}

async Task AtualizarJogoAsync(string codigo, int estado, int golosCasa, int golosFora)
{
    var payload = new
    {
        novo_Estado = estado,
        golos_Casa = golosCasa,
        golos_Fora = golosFora
    };
    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    await httpClient.PutAsync($"/api/Jogos/{codigo}", content);
}