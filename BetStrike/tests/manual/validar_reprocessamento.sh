#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

print_step "Iniciando Teste de Resiliência e Reprocessamento"

# 1. Parar o container AnalyticsWorker
print_step "1. Desligando o AnalyticsWorker..."
docker compose stop analyticsworker

# Encontrar jogo ativo
JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r 'if type == "array" then map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id else .data | map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id end' | grep -v null || echo "1")
if [ -z "$JOGO_ID" ] || [ "$JOGO_ID" == "null" ]; then
    JOGO_ID=1
fi

# Obter volume apostado atual no dashboard
VOL_ANTES=$(curl -s http://localhost:5003/api/dashboard/resumo | jq -r '.totalApostado // 0')
echo "Volume apostado antes: $VOL_ANTES"

# 2. Criar duas apostas com o Worker parado
print_step "2. Submetendo apostas com o motor de Analytics offline (ficarão na fila/tópico)"
for i in {1..2}; do
    PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": 1,
  "tipoAposta": "1",
  "montante": 50.0,
  "odd": 2.00
}
EOF
)
    curl -s -X POST http://localhost:5002/api/Apostas \
      -H "Content-Type: application/json" \
      -d "$PAYLOAD" >/dev/null
done

# Confirmar que o dashboard ainda não atualizou as métricas (pois o worker está parado)
VOL_DURANTE=$(curl -s http://localhost:5003/api/dashboard/resumo | jq -r '.totalApostado // 0')
echo "Volume apostado com o Worker desligado: $VOL_DURANTE"

if [ $(awk "BEGIN {print ($VOL_DURANTE != $VOL_ANTES)}") -eq 1 ]; then
    print_err "Métricas foram atualizadas mesmo com o Worker desligado! Erro na arquitetura desacoplada."
    exit 1
fi
print_ok "Arquitetura desacoplada confirmada. Eventos acumulados no Kafka (Redpanda)."

# 3. Religando o AnalyticsWorker
print_step "3. Religando o AnalyticsWorker para reprocessamento..."
docker compose start analyticsworker

# Aguardar reprocessamento
sleep 5

# Confirmar que o volume agora inclui as apostas acumuladas (+$100.00)
VOL_DEPOIS=$(curl -s http://localhost:5003/api/dashboard/resumo | jq -r '.totalApostado // 0')
echo "Volume apostado após reprocessamento: $VOL_DEPOIS"

ESPERADO=$(awk "BEGIN {print $VOL_ANTES + 100.0}")
if [ $(awk "BEGIN {print ($VOL_DEPOIS < $ESPERADO)}") -eq 1 ]; then
    print_err "O reprocessamento de eventos falhou. Volume atual $VOL_DEPOIS menor do que o esperado de $ESPERADO."
    exit 1
fi

print_ok "Reprocessamento de eventos em Kafka validado com total resiliência!"
