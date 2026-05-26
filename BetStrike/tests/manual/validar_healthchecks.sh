#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;34m>>> $1\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

print_step "Validando Healthchecks do Sistema"

apis=(
    "http://localhost:5001/health"
    "http://localhost:5002/health"
    "http://localhost:5003/health"
)

for api in "${apis[@]}"; do
    status=$(curl -s -o /dev/null -w "%{http_code}" "$api")
    if [ "$status" != "200" ]; then
        print_err "O endpoint $api devolveu HTTP $status"
        exit 1
    fi
    res=$(curl -s "$api")
    echo "Resposta de $api: $res"
done

print_ok "Todos os Healthchecks estão operacionais e saudáveis!"
