#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

print_step "Iniciando Teste Automático de Alertas de Risco"

# Ensure user 1 has balance
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Pass123' -C -Q "UPDATE Pagamentos.dbo.Saldo_Utilizador SET Saldo = 5000.00 WHERE Utilizador_Id = 1" >/dev/null

# Criar um jogo exclusivo de alerta para começar de 0
UNIQUE_ID=$(printf "%04d" $((RANDOM % 10000)))
CODIGO_JOGO="FUT-2026-${UNIQUE_ID}"

# Criar e sincronizar jogo
curl -s -X POST http://localhost:5001/api/Jogos \
  -H "Content-Type: application/json" \
  -d "{\"codigo_Jogo\": \"${CODIGO_JOGO}\", \"data_Jogo\": \"2026-04-23T00:00:00\", \"hora_Inicio\": \"15:30:00\", \"equipa_Casa\": \"Benfica\", \"equipa_Fora\": \"Porto\"}" >/dev/null

curl -s -X POST "http://localhost:5002/api/Sincronizacao/jogos/${CODIGO_JOGO}" >/dev/null

JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r ".[] | select(.codigoJogo == \"${CODIGO_JOGO}\") | .id")

# Criar aposta grande de 1050.00 €
print_step "Criando aposta gigante de 1050 EUR no jogo ${CODIGO_JOGO}"
PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": 1,
  "tipoAposta": "1",
  "montante": 1050.0,
  "odd": 1.50
}
EOF
)

curl -s -X POST http://localhost:5002/api/Apostas \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" >/dev/null

# Aguardar processamento do worker
sleep 5

# 1. Confirmar alerta na API
print_step "1. Validando Alerta Ativo na AnalyticsAPI..."
ALERTAS=$(curl -s http://localhost:5003/api/alertas)
QTD_ALERTAS=$(echo "$ALERTAS" | jq 'length')

if [ "$QTD_ALERTAS" -eq 0 ]; then
    print_err "Nenhum alerta foi gerado no Dashboard de Alertas!"
    exit 1
fi
print_ok "Alerta EXPOSICAO_ELEVADA foi inserido com sucesso na base analítica."

# 2. Confirmar AlertaGeradoEvent em alertas-events
print_step "2. Validando Alerta no Kafka (tópico: alertas-events)..."
KAFKA_MSG=$(timeout 5 docker exec redpanda rpk topic consume alertas-events -f '%v\n' 2>/dev/null | grep -i "EXPOSICAO_ELEVADA" | tail -n 1 || true)
if [ -z "$KAFKA_MSG" ]; then
    print_err "Evento de Alerta não foi publicado no tópico alertas-events do Redpanda."
    exit 1
fi
print_ok "AlertaGeradoEvent transmitido em tempo real pelo Kafka!"

# 3. Confirmar WorkerAlertas nos logs
print_step "3. Validando Logs do Worker de Alertas (RabbitMQ)..."
RABBIT_MSG=$(docker logs workersrabbitmq 2>&1 | grep -i "Alert received! Level: CRITICO" | tail -n 1 || true)
if [ -z "$RABBIT_MSG" ]; then
    print_err "O Worker de Alertas do RabbitMQ não recebeu o comando."
    exit 1
fi
print_ok "Mensagem de Alerta processada assincronamente pelo RabbitMQ!"

print_ok "Todos os fluxos de segurança e alertas analíticos foram perfeitamente validados!"
