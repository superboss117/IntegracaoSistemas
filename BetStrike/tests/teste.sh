#!/usr/bin/env bash

BASE_URL="http://localhost:5290"

EMAIL="pedro4.teste@example.com"
CODIGO_JOGO="FUT-2026-0002"

call_api() {
  local method="$1"
  local url="$2"
  local data="$3"

  echo "-> $method $url"

  if [ -n "$data" ]; then
    curl -i -sS -X "$method" "$url" \
      -H "Content-Type: application/json" \
      -d "$data"
  else
    curl -i -sS -X "$method" "$url"
  fi

  echo
  echo "--------------------------------------------------"
  echo
}

echo "======================================"
echo "1) Criar utilizador novo"
echo "======================================"
call_api "POST" "$BASE_URL/api/Utilizadores" '{
  "nome": "Pedro Melo",
  "email": "'"$EMAIL"'"
}'

echo "======================================"
echo "2) Criar jogo novo"
echo "======================================"
call_api "POST" "$BASE_URL/api/Jogos" '{
  "codigoJogo": "'"$CODIGO_JOGO"'",
  "dataHora": "2026-04-22T20:30:00",
  "equipaCasa": "FC Porto",
  "equipaFora": "Benfica",
  "competicao": "Liga Portugal",
  "estado": 1
}'

echo "======================================"
echo "3) Obter jogo"
echo "======================================"
call_api "GET" "$BASE_URL/api/Jogos/$CODIGO_JOGO" ""

echo "======================================"
echo "4) Listar jogos"
echo "======================================"
call_api "GET" "$BASE_URL/api/Jogos" ""

echo "======================================"
echo "5) Atualizar para estado 2 (Em Curso)"
echo "======================================"
call_api "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" '{
  "novoEstado": 2
}'

echo "======================================"
echo "6) Atualizar para estado 3 (Finalizado)"
echo "======================================"
call_api "PUT" "$BASE_URL/api/Jogos/$CODIGO_JOGO" '{
  "novoEstado": 3,
  "golosCasa": 2,
  "golosFora": 1
}'

echo "======================================"
echo "7) Criar resultado"
echo "======================================"
call_api "POST" "$BASE_URL/api/Resultados" '{
  "codigoJogo": "'"$CODIGO_JOGO"'",
  "golosCasa": 2,
  "golosFora": 1
}'

echo "======================================"
echo "8) Consultar resultado"
echo "======================================"
call_api "GET" "$BASE_URL/api/Resultados/$CODIGO_JOGO" ""

echo "======================================"
echo "9) Criar aposta com montante baixo"
echo "======================================"
echo "ATENÇÃO: ajusta jogoId e utilizadorId para IDs reais da tua base de dados"
call_api "POST" "$BASE_URL/api/Apostas" '{
  "jogoId": 2,
  "utilizadorId": 2,
  "tipoAposta": "1X2-CASA",
  "montante": 5.0,
  "odd": 1.85
}'