#!/usr/bin/env bash
# Arranca la arquitectura completa vía docker compose.
#
# Uso:
#   ./start.sh              # app + Postgres + RabbitMQ
#   ./start.sh --obs        # + stack de observabilidad (Grafana/Loki/Tempo/Prometheus)
#   ./start.sh --build      # fuerza rebuild de las imágenes antes de levantar
set -euo pipefail

cd "$(dirname "$0")"

PROFILE_ARGS=()
SHOW_OBS=0
FORCE_BUILD=0

for arg in "$@"; do
  case "$arg" in
    --obs|--observability) PROFILE_ARGS+=(--profile observability); SHOW_OBS=1 ;;
    --build)               FORCE_BUILD=1 ;;
    -h|--help)
      sed -n '2,7p' "$0"
      exit 0
      ;;
    *) echo "Argumento no reconocido: $arg" >&2; exit 1 ;;
  esac
done

if [ ! -f .env ]; then
  echo "==> Creando .env desde .env.example"
  cp .env.example .env
fi

# Rebuild si se pidió, o si falta alguna imagen de la app
need_build=$FORCE_BUILD
for img in demo/api:latest demo/bff:latest demo/gateway:latest demo/worker:latest demo/frontend:latest; do
  if ! docker image inspect "$img" >/dev/null 2>&1; then
    need_build=1
    break
  fi
done

if [ "$need_build" = "1" ]; then
  echo "==> docker compose build"
  docker compose "${PROFILE_ARGS[@]}" build
fi

echo "==> docker compose up -d --wait"
docker compose "${PROFILE_ARGS[@]}" up -d --wait

echo
echo "Stack listo:"
echo "  Frontend     -> http://localhost:5173"
echo "  Gateway      -> http://localhost:5000"
echo "  BFF Swagger  -> http://localhost:5001/swagger"
echo "  API Swagger  -> http://localhost:5002/swagger"
echo "  Postgres     -> localhost:5432   (demo / demo123)"
echo "  RabbitMQ UI  -> http://localhost:15672  (admin / admin123)"
if [ "$SHOW_OBS" = "1" ]; then
  echo "  Grafana      -> http://localhost:3001  (admin / admin)"
  echo "  Prometheus   -> http://localhost:9090"
fi
echo
echo "Logs   :  docker compose logs -f <servicio>"
echo "Parar  :  ./stop.sh"
