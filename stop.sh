#!/usr/bin/env bash
# Detiene la arquitectura completa.
#
# Uso:
#   ./stop.sh           # baja todos los contenedores (preserva volúmenes)
#   ./stop.sh --clean   # también borra volúmenes (datos de Postgres, RabbitMQ, etc.)
set -euo pipefail

cd "$(dirname "$0")"

EXTRA_ARGS=()
for arg in "$@"; do
  case "$arg" in
    --clean) EXTRA_ARGS+=(-v) ;;
    -h|--help)
      sed -n '2,7p' "$0"
      exit 0
      ;;
    *) echo "Argumento no reconocido: $arg" >&2; exit 1 ;;
  esac
done

echo "==> docker compose down ${EXTRA_ARGS[*]-}"
# Incluimos el profile observability para que también caiga si estaba activo
docker compose --profile observability down "${EXTRA_ARGS[@]}"
