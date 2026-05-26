#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

print_step "Iniciando Teste de Carga de Alta Frequência"

# Encontrar jogo ativo
JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r 'if type == "array" then map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id else .data | map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id end' | grep -v null || echo "1")
if [ -z "$JOGO_ID" ] || [ "$JOGO_ID" == "null" ]; then
    JOGO_ID=1
fi

# Ensure user 1 has balance
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Pass123' -C -Q "UPDATE Pagamentos.dbo.Saldo_Utilizador SET Saldo = 5000.00 WHERE Utilizador_Id = 1" >/dev/null

START_TIME=$(date +%s.%N)

# Criar 5 apostas em loop rápido
for i in {1..5}; do
    echo "Submetendo aposta $i..."
    PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": 1,
  "tipoAposta": "1",
  "montante": 2.0,
  "odd": 1.50
}
EOF
)
    curl -s -X POST http://localhost:5002/api/Apostas \
      -H "Content-Type: application/json" \
      -d "$PAYLOAD" >/dev/null
done

END_TIME=$(date +%s.%N)
ELAPSED=$(awk "BEGIN {print $END_TIME - $START_TIME}")

print_ok "5 apostas de carga criadas num tempo total de: ${ELAPSED}s"

print_step "Aguardando propagação de mensagens..."
sleep 4

# Verificar no dashboard
RESUMO=$(curl -s http://localhost:5003/api/dashboard/resumo)
echo "Métricas de Carga no Dashboard: $RESUMO"
total_apostas=$(echo "$RESUMO" | jq -r '.numeroTotalApostas // 0')

if [ "$total_apostas" -lt 5 ]; then
    print_err "O número total de apostas no dashboard está menor que 5."
    exit 1
fi

print_ok "Teste de Carga executado e consolidado no motor analítico com sucesso!"
