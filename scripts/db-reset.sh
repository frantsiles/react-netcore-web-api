#!/usr/bin/env bash
# db-reset.sh — Elimina la base de datos PostgreSQL y la recrea con los datos semilla.
#
# Uso:
#   ./scripts/db-reset.sh              # usa la connection string de appsettings.Development.json
#   DATABASE_URL="Host=..." ./scripts/db-reset.sh   # override explícito
#
# Requisitos: PostgreSQL corriendo (docker compose -f docker-compose.infra.yml up -d postgres)
#             dotnet-ef instalado globalmente (dotnet tool install -g dotnet-ef)

set -euo pipefail

PROJECT="src/Api/Api.WebApi"
INFRASTRUCTURE="src/Api/Api.Infrastructure"

echo "⏳  Eliminando base de datos..."
dotnet ef database drop --force \
  --project "$INFRASTRUCTURE" \
  --startup-project "$PROJECT"

echo "⏳  Aplicando migraciones..."
dotnet ef database update \
  --project "$INFRASTRUCTURE" \
  --startup-project "$PROJECT"

echo ""
echo "✅  Base de datos recreada. Los datos semilla se aplicarán en el próximo arranque del API."
echo "    Ejecuta: dotnet run --project $PROJECT"
