var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "AnalyticsApi",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/dashboard", () => Results.Content(@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>BetStrike - Analytics Dashboard</title>
    <script src=""https://cdn.tailwindcss.com""></script>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
</head>
<body class=""bg-gray-900 text-gray-100 font-sans"">
    <header class=""bg-gray-800 border-b border-gray-700 py-4 px-6 flex justify-between items-center shadow-lg"">
        <h1 class=""text-2xl font-bold text-emerald-400 tracking-wider"">⚽ BetStrike Analytics</h1>
        <div id=""connection-badge"" class=""bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-2"">
            <span class=""w-2 h-2 rounded-full bg-emerald-400 animate-pulse""></span>
            LIVE
        </div>
    </header>

    <main class=""max-w-7xl mx-auto p-6 space-y-8"">
        <!-- Resumo Cards -->
        <div class=""grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6"">
            <div class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md"">
                <p class=""text-gray-400 text-sm font-semibold uppercase tracking-wider"">Total Apostado</p>
                <p id=""card-total-apostado"" class=""text-3xl font-bold text-white mt-2"">0.00 €</p>
            </div>
            <div class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md"">
                <p class=""text-gray-400 text-sm font-semibold uppercase tracking-wider"">Total de Apostas</p>
                <p id=""card-total-apostas"" class=""text-3xl font-bold text-white mt-2"">0</p>
            </div>
            <div class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md"">
                <p class=""text-gray-400 text-sm font-semibold uppercase tracking-wider"">Jogo Maior Exposição</p>
                <p id=""card-maior-exposicao"" class=""text-3xl font-bold text-emerald-400 mt-2"">N/A</p>
            </div>
            <div id=""card-alertas-container"" class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md transition-all duration-300"">
                <p class=""text-gray-400 text-sm font-semibold uppercase tracking-wider"">Alertas Ativos</p>
                <p id=""card-total-alertas"" class=""text-3xl font-bold text-white mt-2"">0</p>
            </div>
        </div>

        <div class=""grid grid-cols-1 lg:grid-cols-3 gap-8"">
            <!-- Gráfico de Volume por Jogo -->
            <div class=""lg:col-span-2 bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md"">
                <h2 class=""text-lg font-bold mb-4 text-emerald-400"">Volume Apostado por Jogo</h2>
                <div class=""h-64 relative"">
                    <canvas id=""chart-volume""></canvas>
                </div>
            </div>

            <!-- Alertas Recentes -->
            <div class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md flex flex-col h-full max-h-80 lg:max-h-none overflow-hidden"">
                <h2 class=""text-lg font-bold mb-4 text-rose-400"">Alertas Recentes</h2>
                <div id=""alertas-list"" class=""space-y-3 overflow-y-auto flex-1 pr-2"">
                    <p class=""text-gray-400 text-sm"">Sem alertas registados.</p>
                </div>
            </div>
        </div>

        <!-- Tabela por Jogo -->
        <div class=""bg-gray-800 p-6 rounded-xl border border-gray-700 shadow-md overflow-hidden"">
            <h2 class=""text-lg font-bold mb-4 text-emerald-400"">Distribuição Detalhada de Jogos</h2>
            <div class=""overflow-x-auto"">
                <table class=""w-full text-left border-collapse"">
                    <thead>
                        <tr class=""border-b border-gray-700 text-gray-400 text-xs font-semibold uppercase tracking-wider"">
                            <th class=""py-3 px-4"">Jogo</th>
                            <th class=""py-3 px-4"">Volume Apostado</th>
                            <th class=""py-3 px-4"">Nº Apostas</th>
                            <th class=""py-3 px-4"">Exposição Total</th>
                        </tr>
                    </thead>
                    <tbody id=""jogos-table"" class=""divide-y divide-gray-700/50 text-sm"">
                        <!-- Linhas geradas dinamicamente -->
                    </tbody>
                </table>
            </div>
        </div>
    </main>

    <footer class=""bg-gray-900 border-t border-gray-800 text-center py-6 text-gray-500 text-xs mt-12"">
        BetStrike Analytics System &bull; Atualização automática a cada 5s
    </footer>

    <script>
        let myChart = null;

        async function updateDashboard() {
            try {
                const res = await fetch('/api/dashboard/live');
                const data = await res.json();

                // Atualizar Cards
                document.getElementById('card-total-apostado').innerText = `${parseFloat(data.resumo.totalApostado).toFixed(2)} €`;
                document.getElementById('card-total-apostas').innerText = data.resumo.numeroTotalApostas;
                document.getElementById('card-maior-exposicao').innerText = data.resumo.jogoMaiorExposicao;
                document.getElementById('card-total-alertas').innerText = data.resumo.numeroAlertasAtivos;

                const alertaContainer = document.getElementById('card-alertas-container');
                if (data.resumo.numeroAlertasAtivos > 0) {
                    alertaContainer.classList.remove('bg-gray-800', 'border-gray-700');
                    alertaContainer.classList.add('bg-rose-950/20', 'border-rose-500/30', 'text-rose-400');
                } else {
                    alertaContainer.classList.remove('bg-rose-950/20', 'border-rose-500/30', 'text-rose-400');
                    alertaContainer.classList.add('bg-gray-800', 'border-gray-700');
                }

                // Atualizar Tabela de Jogos
                const tableBody = document.getElementById('jogos-table');
                tableBody.innerHTML = '';
                
                if (data.jogos.length === 0) {
                    tableBody.innerHTML = '<tr><td colspan=""4"" class=""py-4 px-4 text-center text-gray-500"">Sem atividade registada.</td></tr>';
                } else {
                    data.jogos.forEach(j => {
                        tableBody.innerHTML += `
                            <tr class=""hover:bg-gray-700/30 transition-colors"">
                                <td class=""py-4 px-4 font-semibold text-emerald-400"">${j.codigoJogo || j.CodigoJogo}</td>
                                <td class=""py-4 px-4"">${parseFloat(j.volumeApostado || j.VolumeApostado).toFixed(2)} €</td>
                                <td class=""py-4 px-4"">${j.numeroApostas || j.NumeroApostas}</td>
                                <td class=""py-4 px-4 font-semibold text-emerald-400"">${parseFloat(j.exposicao || j.Exposicao).toFixed(2)} €</td>
                            </tr>
                        `;
                    });
                }

                // Atualizar Lista de Alertas
                const alertList = document.getElementById('alertas-list');
                alertList.innerHTML = '';
                if (data.alertasRecentes.length === 0) {
                    alertList.innerHTML = '<p class=""text-gray-500 text-sm"">Sem alertas registados.</p>';
                } else {
                    data.alertasRecentes.slice(0, 5).forEach(a => {
                        const dateStr = new Date(a.dataHora || a.DataHora).toLocaleTimeString();
                        alertList.innerHTML += `
                            <div class=""bg-rose-950/20 border border-rose-500/20 rounded-lg p-3 text-xs space-y-1"">
                                <div class=""flex justify-between font-bold text-rose-400"">
                                    <span>${a.tipo || a.Tipo}</span>
                                    <span>${dateStr}</span>
                                </div>
                                <p class=""text-gray-300"">${a.detalhes || a.Detalhes}</p>
                            </div>
                        `;
                    });
                }

                // Atualizar Gráfico
                const labels = data.jogos.map(j => j.codigoJogo || j.CodigoJogo);
                const volumes = data.jogos.map(j => j.volumeApostado || j.VolumeApostado);

                if (!myChart) {
                    const ctx = document.getElementById('chart-volume').getContext('2d');
                    myChart = new Chart(ctx, {
                        type: 'bar',
                        data: {
                            labels: labels,
                            datasets: [{
                                label: 'Volume Apostado (€)',
                                data: volumes,
                                backgroundColor: 'rgba(52, 211, 153, 0.2)',
                                borderColor: 'rgba(52, 211, 153, 1)',
                                borderWidth: 2,
                                borderRadius: 6
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {
                                legend: { display: false }
                            },
                            scales: {
                                y: {
                                    grid: { color: 'rgba(255, 255, 255, 0.05)' },
                                    ticks: { color: 'rgba(255, 255, 255, 0.6)' }
                                },
                                x: {
                                    grid: { display: false },
                                    ticks: { color: 'rgba(255, 255, 255, 0.6)' }
                                }
                            }
                        }
                    });
                } else {
                    myChart.data.labels = labels;
                    myChart.data.datasets[0].data = volumes;
                    myChart.update();
                }

                // Badge status
                const badge = document.getElementById('connection-badge');
                badge.className = 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-2';
                badge.innerHTML = '<span class=\'w-2 h-2 rounded-full bg-emerald-400 animate-pulse\'></span>LIVE';

            } catch (err) {
                console.error('Erro a atualizar o dashboard:', err);
                const badge = document.getElementById('connection-badge');
                badge.className = 'bg-rose-500/20 text-rose-400 border border-rose-500/30 px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-2';
                badge.innerHTML = '<span class=\'w-2 h-2 rounded-full bg-rose-500\'></span>OFFLINE';
            }
        }

        updateDashboard();
        setInterval(updateDashboard, 5000);
    </script>
</body>
</html>", "text/html"));

app.MapControllers();

app.Run();
