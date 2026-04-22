#!/usr/bin/env bash

set -o pipefail

BASE_URL="${1:-http://localhost:5002}"

CODIGO_JOGO="FUT-2026-0001"
DATA_HORA="2026-04-23T15:30:00"
COMPETICAO="Liga Portugal"

UTILIZADOR_NOME="Pedro Melo"
UTILIZADOR_EMAIL="pedro.$(date +%s)@example.com"

TIPO_APOSTA="1"
MONTANTE="10.50"
ODD="1.85"

UTILIZADOR_ID=""
JOGO_ID=""
APOSTA_ID=""

RESPONSE=""
BODY=""
STATUS=""

hr() {
  echo "============================================================"
}

section() {
  echo
  hr
  echo "$1"
  hr
}

do_request() {
  local method="$1"
  local url="$2"
  local body="${3:-}"

  echo
  echo "-> $method $url"

  if [[ -n "$body" ]]; then
    echo "Payload:"
    echo "$body"
    echo
    RESPONSE=$(curl -sS -i -X "$method" "$url" \
      -H "Content-Type: application/json" \
      -d "$body")
  else
    RESPONSE=$(curl -sS -i -X "$method" "$url")
  fi

  echo "$RESPONSE"
  echo

  BODY=$(printf '%s\n' "$RESPONSE" | sed -n '/^\r\{0,1\}$/,$p' | sed '1d')
  STATUS=$(printf '%s\n' "$RESPONSE" | head -n 1 | awk '{print $2}')
}

extract_first_number_after_key() {
  local json="$1"
  local key="$2"

  printf '%s' "$json" \
    | grep -o "\"$key\"[[:space:]]*:[[:space:]]*[0-9]\+" \
    | head -n 1 \
    | grep -o "[0-9]\+"
}

section "Teste da ApostasApi"
echo "BASE_URL = $BASE_URL"

#
# 1) Criar utilizador
#
section "1) Criar utilizador"

CRIAR_UTILIZADOR=$(cat <<EOF
{
  "nome": "$UTILIZADOR_NOME",
  "email": "$UTILIZADOR_EMAIL"
}
EOF
)

do_request "POST" "$BASE_URL/api/Utilizadores" "$CRIAR_UTILIZADOR"

UTILIZADOR_ID=$(extract_first_number_after_key "$BODY" "utilizadorId")
echo "UTILIZADOR_ID detetado: ${UTILIZADOR_ID:-<não encontrado>}"

#
# 2) Criar jogo
#
section "2) Criar jogo"

CRIAR_JOGO=$(cat <<EOF
{
  "codigoJogo": "$CODIGO_JOGO",
  "dataHora": "$DATA_HORA",
  "equipaCasa": "Benfica",
  "equipaFora": "Porto",
  "competicao": "$COMPETICAO",
  "estado": 1
}
EOF
)

do_request "POST" "$BASE_URL/api/Jogos" "$CRIAR_JOGO"

#
# 3) Listar jogos
#
section "3) Listar jogos"
do_request "GET" "$BASE_URL/api/Jogos"

#
# 4) Filtrar jogos
#
section "4) Filtrar jogos por estado e competição"
do_request "GET" "$BASE_URL/api/Jogos?estado=1&competicao=Liga%20Portugal"

#
# 5) Obter jogo por código
#
section "5) Obter jogo por código"
do_request "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

JOGO_ID=$(extract_first_number_after_key "$BODY" "id")
echo "JOGO_ID detetado: ${JOGO_ID:-<não encontrado>}"

#
# 6) Atualizar jogo para Em Curso
#
section "6) Atualizar jogo para Em Curso"

ATUALIZAR_JOGO_1=$(cat <<EOF
{
  "novoEstado": 2
}
EOF
)

do_request "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" "$ATUALIZAR_JOGO_1"

#
# 7) Obter jogo atualizado
#
section "7) Obter jogo após Em Curso"
do_request "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

#
# 8) Atualizar jogo para Finalizado
#
section "8) Atualizar jogo para Finalizado"

ATUALIZAR_JOGO_2=$(cat <<EOF
{
  "novoEstado": 3,
  "golosCasa": 2,
  "golosFora": 1
}
EOF
)

do_request "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" "$ATUALIZAR_JOGO_2"

#
# 9) Obter jogo finalizado
#
section "9) Obter jogo finalizado"
do_request "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

#
# 10) Criar resultado
#
section "10) Criar resultado"

CRIAR_RESULTADO=$(cat <<EOF
{
  "codigoJogo": "$CODIGO_JOGO",
  "golosCasa": 2,
  "golosFora": 1
}
EOF
)

do_request "POST" "$BASE_URL/api/Resultados" "$CRIAR_RESULTADO"

#
# 11) Obter resultado por código
#
section "11) Obter resultado por código"
do_request "GET" "$BASE_URL/api/Resultados/$CODIGO_JOGO"

#
# 12) Estatísticas por jogo
#
section "12) Estatísticas por jogo"
do_request "GET" "$BASE_URL/api/Estatisticas/jogo/$CODIGO_JOGO"

#
# 13) Estatísticas por competição
#
section "13) Estatísticas por competição"
do_request "GET" "$BASE_URL/api/Estatisticas/competicao/Liga%20Portugal"

#
# 14) Sincronizar jogo
#
section "14) Sincronizar jogo"
do_request "POST" "$BASE_URL/api/Sincronizacao/jogos/$CODIGO_JOGO"

#
# 15) Criar aposta
#
section "15) Criar aposta"

if [[ -z "$UTILIZADOR_ID" ]]; then
  echo "AVISO: Não foi possível detetar UTILIZADOR_ID automaticamente."
fi

if [[ -z "$JOGO_ID" ]]; then
  echo "AVISO: Não foi possível detetar JOGO_ID automaticamente."
fi

CRIAR_APOSTA=$(cat <<EOF
{
  "jogoId": ${JOGO_ID:-0},
  "utilizadorId": ${UTILIZADOR_ID:-0},
  "tipoAposta": "$TIPO_APOSTA",
  "montante": $MONTANTE,
  "odd": $ODD
}
EOF
)

do_request "POST" "$BASE_URL/api/Apostas" "$CRIAR_APOSTA"

APOSTA_ID=$(extract_first_number_after_key "$BODY" "id")
echo "APOSTA_ID detetado: ${APOSTA_ID:-<não encontrado>}"

#
# 16) Listar apostas
#
section "16) Listar apostas"
do_request "GET" "$BASE_URL/api/Apostas"

#
# 17) Filtrar apostas
#
section "17) Filtrar apostas por utilizador, jogo e estado"
do_request "GET" "$BASE_URL/api/Apostas?idUtilizador=${UTILIZADOR_ID:-0}&codigoJogo=$CODIGO_JOGO&estado=1"

#
# 18) Obter aposta por ID
#
section "18) Obter aposta por ID"
do_request "GET" "$BASE_URL/api/Apostas/${APOSTA_ID:-1}"

#
# 19) Cancelar aposta
#
section "19) Cancelar aposta"
do_request "PUT" "$BASE_URL/api/Apostas/${APOSTA_ID:-1}/cancelar"

#
# 20) Confirmar aposta após cancelamento
#
section "20) Confirmar aposta após cancelamento"
do_request "GET" "$BASE_URL/api/Apostas/${APOSTA_ID:-1}"

#
# 21) Apagar jogo
#
section "21) Apagar jogo"
do_request "DELETE" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

section "Fim dos testes"
echo "Script concluído."