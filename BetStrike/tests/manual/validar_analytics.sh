#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

if ! command -v jq &> /dev/null; then
    print_err "jq is not instalado. Por favor, instala o jq para correr este script."
    exit 1
fi

print_step "1. Validando Estado dos Containers Analytics"
containers=("analyticsworker" "analyticsapi")
for c in "${containers[@]}"; do
    if [ "$(docker inspect -f '{{.State.Running}}' "$c" 2>/dev/null)" != "true" ]; then
        print_err "O container $c não está a correr!"
        echo "Comando útil para debug: docker logs $c"
        exit 1
    fi
done
print_ok "Containers de analytics estão a correr."

print_step "2. Lendo Resumo Atual (Antes do Teste)"
RESUMO_ANTES=$(curl -s http://localhost:5003/api/dashboard/resumo)
VOL_ANTES=$(echo "$RESUMO_ANTES" | jq -r '.totalApostado // 0')
echo "Volume apostado atual: $VOL_ANTES"

print_step "3. Obtendo Jogo Ativo"
JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r 'if type == "array" then map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id else .data | map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id end' | grep -v null || echo "1")
if [ -z "$JOGO_ID" ] || [ "$JOGO_ID" == "null" ]; then
    JOGO_ID=1
fi
UTILIZADOR_ID=1

print_step "4. Criando Apostas Grandes para Disparar Alerta (Volume > 1000)"
PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": $UTILIZADOR_ID,
  "tipoAposta": "1",
  "montante": 1050.0,
  "odd": 2.00
}
EOF
)

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST http://localhost:5002/api/Apostas \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" != "200" ]; then
    print_err "Falha ao criar a aposta. Código HTTP recebido: $HTTP_CODE"
    echo "Corpo da resposta: $BODY"
    exit 1
fi

APOSTA_ID=$(echo "$BODY" | jq -r '.data.apostaId // .data.ApostaId')
print_ok "Aposta gigante criada com sucesso (ApostaId: $APOSTA_ID)"

print_step "5. Aguardando Processamento do AnalyticsWorker"
sleep 5

print_step "6. Verificando Novo Resumo"
RESUMO_DEPOIS=$(curl -s http://localhost:5003/api/dashboard/resumo)
VOL_DEPOIS=$(echo "$RESUMO_DEPOIS" | jq -r '.totalApostado // 0')
echo "Volume apostado depois: $VOL_DEPOIS"

if [ $(awk "BEGIN {print ($VOL_DEPOIS <= $VOL_ANTES)}") -eq 1 ]; then
    print_err "Volume apostado não aumentou. O AnalyticsWorker pode não ter processado o evento."
    exit 1
fi
print_ok "Volume apostado foi atualizado no Dashboard"

print_step "7. Verificando Alertas na API"
ALERTAS=$(curl -s http://localhost:5003/api/alertas)
QTD_ALERTAS=$(echo "$ALERTAS" | jq 'length')

if [ "$QTD_ALERTAS" -eq 0 ]; then
    print_err "Nenhum alerta foi gerado mesmo com volume > 1000."
    exit 1
fi
print_ok "Alerta(s) gerado(s) e exposto(s) na API de Alertas."

print_step "8. Validando Fila de Alertas no WorkerRabbitMq"
WORKER_LOGS=$(docker logs workersrabbitmq 2>&1 | grep -i "Alert received! Level: CRITICO" | tail -n 1 || true)
if [ -z "$WORKER_LOGS" ]; then
    print_err "Mensagem de alerta não chegou ao WorkerAlertas do RabbitMQ."
    exit 1
fi
print_ok "Mensagem de alerta processada no RabbitMQ Worker"

print_step "Resultado Final"
echo -e "\033[1;32m[OK] AnalyticsWorker leu os tópicos Kafka."
echo -e "[OK] Dashboard atualizou métricas de negócio em tempo real."
echo -e "[OK] Alerta EXPOSICAO_ELEVADA foi ativado."
echo -e "[OK] Alerta foi reencaminhado via RabbitMQ."
echo -e "\nSISTEMA ANALYTICS VALIDADO COM SUCESSO!\033[0m"
