#!/usr/bin/env bash

set -u

BASE_URL="http://localhost:5001"
UNIQUE_ID=$(printf "%04d" $((RANDOM % 10000)))
CODIGO_JOGO="FUT-2026-${UNIQUE_ID}"

DATA_JOGO="2026-04-23T00:00:00"
HORA_INICIO="15:30:00"

echo "======================================"
echo "Teste da ResultadosApi"
echo "BASE_URL: $BASE_URL"
echo "CODIGO_JOGO: $CODIGO_JOGO"
echo "======================================"

request() {
  local method="$1"
  local url="$2"
  local body="${3:-}"

  echo
  echo "-> $method $url"

  if [ -n "$body" ]; then
    echo "Payload:"
    echo "$body"
    echo
    curl -i -s -X "$method" "$url" \
      -H "Content-Type: application/json" \
      -d "$body"
  else
    curl -i -s -X "$method" "$url"
  fi

  echo
  echo "--------------------------------------"
}

# 1) Inserir jogo
POST_BODY=$(cat <<EOF
{
  "codigo_Jogo": "$CODIGO_JOGO",
  "data_Jogo": "$DATA_JOGO",
  "hora_Inicio": "$HORA_INICIO",
  "equipa_Casa": "Benfica",
  "equipa_Fora": "Porto"
}
EOF
)

echo
echo "1) Inserir jogo"
request "POST" "$BASE_URL/api/Jogos" "$POST_BODY"

# 2) Listar jogos
echo
echo "2) Listar jogos"
request "GET" "$BASE_URL/api/Jogos"

# 3) Filtrar jogos por estado
echo
echo "3) Listar jogos por estado=1"
request "GET" "$BASE_URL/api/Jogos?estado=1"

# 4) Obter jogo por código
echo
echo "4) Obter jogo por código"
request "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

# 4.5) Atualizar jogo para Em Curso (Estado 2)
PUT_BODY_CURSO=$(cat <<EOF
{
  "novo_Estado": 2,
  "golos_Casa": 0,
  "golos_Fora": 0
}
EOF
)
echo
echo "4.5) Atualizar jogo para Em Curso"
request "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" "$PUT_BODY_CURSO"

# 5) Atualizar jogo
PUT_BODY=$(cat <<EOF
{
  "novo_Estado": 3,
  "golos_Casa": 2,
  "golos_Fora": 1
}
EOF
)

echo
echo "5) Atualizar jogo para Finalizado"
request "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" "$PUT_BODY"

# 6) Obter jogo atualizado
echo
echo "6) Obter jogo atualizado"
request "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

# 7) Apagar jogo (Falha esperada - Jogo não está Agendado)
echo
echo "7) Apagar jogo (deverá falhar com 400)"
request "DELETE" "$BASE_URL/api/Jogos/$CODIGO_JOGO"

# 7.5) Inserir jogo temporário para testar remoção
POST_BODY_TEMP=$(cat <<EOF
{
  "codigo_Jogo": "FUT-2026-9999",
  "data_Jogo": "$DATA_JOGO",
  "hora_Inicio": "$HORA_INICIO",
  "equipa_Casa": "TempCasa",
  "equipa_Fora": "TempFora"
}
EOF
)
echo
echo "7.5) Inserir jogo temporário para testar remoção"
request "POST" "$BASE_URL/api/Jogos" "$POST_BODY_TEMP"

# 8) Apagar jogo temporário
echo
echo "8) Apagar jogo temporário (deverá ter sucesso)"
request "DELETE" "$BASE_URL/api/Jogos/FUT-2026-9999"

echo
echo "Teste concluído."