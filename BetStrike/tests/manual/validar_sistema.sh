#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

# Check dependencies
if ! command -v jq &> /dev/null; then
    print_err "jq is not instalado. Por favor, instala o jq para correr este script."
    exit 1
fi

print_step "1. Validando Estado dos Containers"
containers=("sqlserver" "resultadosapi" "apostasapi" "redpanda" "redpanda-console" "rabbitmq" "workersrabbitmq" "eventlogger")
for c in "${containers[@]}"; do
    if [ "$(docker inspect -f '{{.State.Running}}' "$c" 2>/dev/null)" != "true" ]; then
        print_err "O container $c não está a correr!"
        echo "Comando útil para debug: docker logs $c"
        exit 1
    fi
done
print_ok "Todos os containers obrigatórios estão a correr."

print_step "2. Validando APIs (Parte 1)"
endpoints=(
    "http://localhost:5001/swagger/v1/swagger.json"
    "http://localhost:5002/swagger/v1/swagger.json"
    "http://localhost:5002/api/Jogos"
    "http://localhost:5002/api/Apostas"
)

for ep in "${endpoints[@]}"; do
    status=$(curl -s -o /dev/null -w "%{http_code}" "$ep")
    if [ "$status" != "200" ]; then
        print_err "O endpoint $ep devolveu HTTP $status"
        echo "Comando útil para debug: curl -i $ep"
        exit 1
    fi
done
print_ok "APIs antigas estão ativas e acessíveis."

print_step "3. Preparando Dados (Jogo e Utilizador)"
# Tentamos extrair o ID do primeiro jogo disponível, usando fallback 1 caso a API não devolva os dados na estrutura esperada
JOGO_ID=$(curl -s http://localhost:5002/api/Jogos | jq -r 'if type == "array" then map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id else .data | map(select(.Estado == 1 or .estado == 1)) | .[0].id // .[0].Id end' | grep -v null || echo "1")
if [ -z "$JOGO_ID" ]; then
    JOGO_ID=1
fi
UTILIZADOR_ID=1

print_step "4. Criando Aposta"
PAYLOAD=$(cat <<EOF
{
  "jogoId": $JOGO_ID,
  "utilizadorId": $UTILIZADOR_ID,
  "tipoAposta": "1",
  "montante": 1.0,
  "odd": 1.50
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
    echo "Comando útil para debug: docker logs apostasapi"
    exit 1
fi

# Extrair apostaId tendo em conta PascalCase (ApostaId) e camelCase (apostaId)
APOSTA_ID=$(echo "$BODY" | jq -r '.data.apostaId // .data.ApostaId')
if [ "$APOSTA_ID" == "null" ] || [ -z "$APOSTA_ID" ]; then
    print_err "Não foi possível extrair o apostaId da resposta."
    echo "Corpo da resposta: $BODY"
    exit 1
fi
print_ok "Criação de aposta com sucesso (ApostaId: $APOSTA_ID)"

print_step "5. Validando Kafka / Redpanda (Parte 2)"
# Lemos do final do tópico até ao máximo de 10 segundos, parando automaticamente ou no fim do timeout
KAFKA_MSG=$(timeout 10 docker exec redpanda rpk topic consume apostas-events -f '%v\n' 2>/dev/null | grep -i "\"ApostaId\":$APOSTA_ID" || true)

if [ -z "$KAFKA_MSG" ]; then
    print_err "ApostaCriadaEvent referente à ApostaId $APOSTA_ID não foi encontrado no Redpanda."
    echo "Comando útil para debug: docker exec -it redpanda rpk topic consume apostas-events --num 5"
    exit 1
fi
print_ok "Evento Kafka publicado e validado (apostas-events)"

print_step "6. Validando RabbitMQ e Workers (Parte 2)"
# Verificar se a consola de gestão responde
if ! curl -s -o /dev/null -w "%{http_code}" http://localhost:15672 | grep -q "200"; then
    print_err "O Management Dashboard do RabbitMQ não está acessível no porto 15672."
    exit 1
fi

# Aguardar um pequeno intervalo para garantir a conclusão do log do worker
sleep 2

# Extrair logs do worker
WORKER_LOGS=$(docker logs workersrabbitmq 2>&1 | grep -i "Aposta $APOSTA_ID" || true)

if [ -z "$WORKER_LOGS" ]; then
    print_err "Mensagem de auditoria para a ApostaId $APOSTA_ID não apareceu nos logs do RabbitMQ Worker."
    echo "Comando útil para debug: docker logs workersrabbitmq"
    exit 1
fi
print_ok "Mensagem RabbitMQ processada no worker (fila-auditoria)"

print_step "7. Validando Estabilidade Final dos Containers"
CRASHED=0
for c in "${containers[@]}"; do
    if [ "$(docker inspect -f '{{.State.Running}}' "$c" 2>/dev/null)" != "true" ]; then
        print_err "O container $c crashou durante o teste!"
        CRASHED=1
    fi
done

if [ "$CRASHED" == "1" ]; then
    echo "Estado final dos containers:"
    docker compose ps
    exit 1
fi

print_step "Resultado Final"
echo -e "\033[1;32m[OK] APIs antigas validadas"
echo -e "[OK] Criação de aposta com montante baixo efetuada"
echo -e "[OK] Evento Kafka publicado e encontrado no tópico apostas-events"
echo -e "[OK] Mensagem RabbitMQ processada assincronamente pelo worker de auditoria"
echo -e "[OK] Estabilidade dos containers confirmada"
echo -e "\nSISTEMA VALIDADO COM SUCESSO!\033[0m"
