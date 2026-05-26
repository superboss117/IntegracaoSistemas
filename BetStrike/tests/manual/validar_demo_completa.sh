#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

print_step "Iniciando Demonstração Completa do Sistema"

# Ensure user 1 has balance
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Pass123' -C -Q "UPDATE Pagamentos.dbo.Saldo_Utilizador SET Saldo = 5000.00 WHERE Utilizador_Id = 1" >/dev/null

UNIQUE_ID=$(printf "%04d" $((RANDOM % 10000)))
CODIGO_JOGO="FUT-2026-${UNIQUE_ID}"

# 1. Criar jogo na ResultadosApi
print_step "1. Criando novo jogo na ResultadosApi (${CODIGO_JOGO})"
POST_BODY=$(cat <<EOF
{
  "codigo_Jogo": "${CODIGO_JOGO}",
  "data_Jogo": "2026-04-23T00:00:00",
  "hora_Inicio": "15:30:00",
  "equipa_Casa": "Benfica",
  "equipa_Fora": "Porto"
}
EOF
)
curl -s -i -X POST http://localhost:5001/api/Jogos \
  -H "Content-Type: application/json" \
  -d "$POST_BODY" >/dev/null

# 2. Sincronizar na ApostasApi
print_step "2. Sincronizando jogo na ApostasApi"
curl -s -X POST "http://localhost:5002/api/Sincronizacao/jogos/${CODIGO_JOGO}" >/dev/null

# Obter jogoId local
JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r ".[] | select(.codigoJogo == \"${CODIGO_JOGO}\") | .id")
echo "Jogo ID local detetado: ${JOGO_ID}"

# 3. Colocar aposta pequena
print_step "3. Criando aposta de demonstração"
APOSTA_PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": 1,
  "tipoAposta": "1",
  "montante": 10.0,
  "odd": 1.85
}
EOF
)
RESPONSE=$(curl -s -X POST http://localhost:5002/api/Apostas \
  -H "Content-Type: application/json" \
  -d "$APOSTA_PAYLOAD")

APOSTA_ID=$(echo "$RESPONSE" | jq -r '.data.apostaId // .data.ApostaId')
print_ok "Aposta criada com sucesso! ID: $APOSTA_ID"

# 4. Validar Kafka
print_step "4. Validando transmissão de eventos em tempo real (Kafka)"
KAFKA_MSG=$(timeout 5 docker exec redpanda rpk topic consume apostas-events -f '%v\n' 2>/dev/null | grep -i "\"ApostaId\":$APOSTA_ID" || true)
if [ -z "$KAFKA_MSG" ]; then
    print_err "Evento da aposta não foi encontrado no Redpanda."
    exit 1
fi
print_ok "Evento Kafka validado com sucesso!"

# 5. Validar RabbitMQ
print_step "5. Validando processamento em background (RabbitMQ Auditoria)"
sleep 2
AUDIT_LOG=$(docker logs workersrabbitmq 2>&1 | grep -i "Aposta ${APOSTA_ID}" || true)
if [ -z "$AUDIT_LOG" ]; then
    print_err "Mensagem de auditoria não foi encontrada nos logs do RabbitMQ."
    exit 1
fi
print_ok "RabbitMQ Auditoria validado com sucesso!"

# 6. Validar Analytics Dashboard
print_step "6. Validando métricas no Dashboard de Analytics"
RESUMO=$(curl -s http://localhost:5003/api/dashboard/resumo)
echo "Resumo de Analytics: $RESUMO"
total_apostado=$(echo "$RESUMO" | jq -r '.totalApostado // 0')

if (( $(echo "$total_apostado < 10.0" | bc -l 2>/dev/null || awk "BEGIN {print ($total_apostado < 10.0)}") )); then
    print_err "O volume do dashboard não reflete a aposta de 10 EUR."
    exit 1
fi
print_ok "Camada de Analytics validada com sucesso!"
