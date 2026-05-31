#!/usr/bin/env bash
# Detiene la arquitectura.
#
# Uso:
#   ./stop.sh           # baja todos los contenedores Docker (preserva volúmenes)
#   ./stop.sh --clean   # también borra volúmenes (datos de Postgres, RabbitMQ, etc.)
#   ./stop.sh --dev     # baja solo la infra de desarrollo (Postgres + RabbitMQ)
set -euo pipefail

cd "$(dirname "$0")"

EXTRA_ARGS=()
DEV_MODE=0

for arg in "$@"; do
  case "$arg" in
    --clean) EXTRA_ARGS+=(-v) ;;
    --dev)   DEV_MODE=1 ;;
    -h|--help)
      sed -n '2,8p' "$0"
      exit 0
      ;;
    *) echo "Argumento no reconocido: $arg" >&2; exit 1 ;;
  esac
done

if [ "$DEV_MODE" = "1" ]; then
  echo "==> Bajando infra de desarrollo (docker-compose.infra.yml)"
  docker compose -f docker-compose.infra.yml down "${EXTRA_ARGS[@]}"
else
  echo "==> docker compose down ${EXTRA_ARGS[*]-}"
  # Incluimos el profile observability para que también caiga si estaba activo
  docker compose --profile observability down "${EXTRA_ARGS[@]}"
fi
