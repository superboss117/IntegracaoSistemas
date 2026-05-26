#!/bin/bash
set -euo pipefail

# Helper function for printing
print_step() { echo -e "\n\033[1;35m>>>>>> $1 <<<<<<\033[0m"; }
print_ok() { echo -e "\033[1;32m[OK] $1\033[0m"; }
print_err() { echo -e "\033[1;31m[ERROR] $1\033[0m"; }

# Check dependencies
if ! command -v jq &> /dev/null; then
    print_err "jq is not instalado. Por favor, instala o jq para correr este script."
    exit 1
fi

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

print_step "INICIANDO PIPELINE COMPLETA DE VALIDAÇÃO (PARTE 1 & PARTE 2)"

echo "Executando: validar_healthchecks.sh"
"$DIR/validar_healthchecks.sh"

echo "Executando: validar_demo_completa.sh"
"$DIR/validar_demo_completa.sh"

echo "Executando: validar_carga.sh"
"$DIR/validar_carga.sh"

echo "Executando: validar_reprocessamento.sh"
"$DIR/validar_reprocessamento.sh"

echo "Executando: validar_alertas.sh"
"$DIR/validar_alertas.sh"

print_step "SUCESSO TOTAL - TODAS AS CAMADAS DO SISTEMA VALIDADA COM SUCESSO!"

