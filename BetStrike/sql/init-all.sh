#!/usr/bin/env bash
set -e

echo "A aguardar SQL Server..."

for i in {1..60}; do
  /opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -Q "SELECT 1" >/dev/null 2>&1 && break
  echo "SQL Server ainda não está pronto... tentativa $i"
  sleep 2
done

echo "SQL Server pronto. A executar scripts SQL..."

echo "-> Pagamentos"
/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -i /sql/Pagamentos/Database_Pagamentos.sql

echo "-> Resultados"
/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -i /sql/Resultados/BD_Resultados.sql

echo "-> Apostas - base"
/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -i /sql/Apostas/DatabaseApostas.sql

echo "-> Apostas - stored procedures"
/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -i /sql/Apostas/StoredProcedures.sql

echo "-> Apostas - triggers"
/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P 'YourStrong@Pass123' -C -i /sql/Apostas/Trigger_mudanca_estado.sql

echo "Scripts executados com sucesso."