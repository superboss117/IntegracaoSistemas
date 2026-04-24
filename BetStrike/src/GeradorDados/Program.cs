using System.Text;
using System.Text.Json;

var equipas = new List<string>
{
    "Benfica", "Porto", "Sporting CP", "Braga", "Guimarães",
    "Famalicão", "Estoril", "Casa Pia", "Moreirense", "Arouca",
    "Rio Ave", "Boavista", "Farense", "Gil Vicente", "Santa Clara",
    "Estrela Amadora", "Nacional", "AVS"
};

const string BASE_URL = "http://localhost:5001";
const int NUMERO_JORNADA = 1;
const int INTERVALO_SEGUNDOS = 10;

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};

var httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri(BASE_URL)
};

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
{
    Console.WriteLine($"  {j.codigo}: {j.casa} vs {j.fora}");
}

Console.WriteLine();

var jogosInseridos = new List<(string casa, string fora, string codigo)>();

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
    var responseBody = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"[OK] Jogo inserido: {jogo.codigo}");
        jogosInseridos.Add(jogo);
    }
    else
    {
        Console.WriteLine($"[ERRO INSERT] {jogo.codigo}: {responseBody}");
    }
}

if (jogosInseridos.Count == 0)
{
    Console.WriteLine("\nNenhum jogo foi inserido. A simulação foi cancelada.");
    return;
}

Console.WriteLine("\nA iniciar simulação dos jogos inseridos em paralelo...\n");

var tarefas = jogosInseridos
    .Select(jogo => SimularJogoAsync(jogo.codigo, jogo.casa, jogo.fora))
    .ToList();

await Task.WhenAll(tarefas);

Console.WriteLine("\n=== Simulação concluída! ===");

async Task SimularJogoAsync(string codigo, string casa, string fora)
{
    var rnd = new Random(Guid.NewGuid().GetHashCode());

    int golosCasa = 0;
    int golosFora = 0;
    int passosSimulacao = 9;

    var updateInicialOk = await AtualizarJogoAsync(codigo, 2, golosCasa, golosFora);

    if (!updateInicialOk)
    {
        Console.WriteLine($"[{codigo}] Simulação cancelada porque o jogo não entrou em curso.");
        return;
    }

    Console.WriteLine($"[{codigo}] A decorrer: {casa} {golosCasa}-{golosFora} {fora}");

    for (int passo = 0; passo < passosSimulacao; passo++)
    {
        await Task.Delay(INTERVALO_SEGUNDOS * 1000);

        if (rnd.NextDouble() < 0.15)
            golosCasa++;

        if (rnd.NextDouble() < 0.15)
            golosFora++;

        var updateOk = await AtualizarJogoAsync(codigo, 2, golosCasa, golosFora);

        if (!updateOk)
        {
            Console.WriteLine($"[{codigo}] Simulação interrompida por erro no update.");
            return;
        }

        Console.WriteLine($"[{codigo}] Min {(passo + 1) * 10}: {casa} {golosCasa}-{golosFora} {fora}");
    }

    var finalOk = await AtualizarJogoAsync(codigo, 3, golosCasa, golosFora);

    if (finalOk)
        Console.WriteLine($"[{codigo}] FINALIZADO: {casa} {golosCasa}-{golosFora} {fora}");
    else
        Console.WriteLine($"[{codigo}] Erro ao finalizar jogo.");
}

async Task<bool> AtualizarJogoAsync(string codigo, int estado, int golosCasa, int golosFora)
{
    var payload = new
    {
        novo_Estado = estado,
        golos_Casa = golosCasa,
        golos_Fora = golosFora
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PutAsync($"/api/Jogos/{codigo}", content);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
        return true;

    Console.WriteLine($"[ERRO UPDATE] {codigo}: {responseBody}");
    return false;
}