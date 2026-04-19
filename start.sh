#!/usr/bin/env bash
set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"

echo "==> Starting API (port 5002)..."
dotnet run --project "$ROOT/src/Api/Api.WebApi/Api.WebApi.csproj" --launch-profile http \
  > /tmp/api.log 2>&1 &
API_PID=$!

echo "==> Starting BFF (port 5001)..."
dotnet run --project "$ROOT/src/BFF/BFF.Api/BFF.Api.csproj" --launch-profile http \
  > /tmp/bff.log 2>&1 &
BFF_PID=$!

echo "==> Starting Frontend (port 5173)..."
cd "$ROOT/frontend" && npm run dev > /tmp/frontend.log 2>&1 &
FE_PID=$!

echo ""
echo "PIDs  — API: $API_PID | BFF: $BFF_PID | Frontend: $FE_PID"
echo "Logs  — /tmp/api.log | /tmp/bff.log | /tmp/frontend.log"
echo ""
echo "Waiting for services..."

# Wait until all three ports are accepting connections (max 60s each)
for port in 5002 5001 5173; do
  for i in $(seq 1 30); do
    if curl -s "http://localhost:$port" > /dev/null 2>&1 || \
       curl -s "http://localhost:$port/health" > /dev/null 2>&1 || \
       nc -z localhost $port 2>/dev/null; then
      echo "  ✓ Port $port ready"
      break
    fi
    sleep 2
  done
done

echo ""
echo "All services up."
echo "  API Swagger  -> http://localhost:5002/swagger"
echo "  BFF Swagger  -> http://localhost:5001/swagger"
echo "  Frontend     -> http://localhost:5173"
echo ""
echo "Press Ctrl+C to stop all services."

trap "echo ''; echo 'Stopping...'; kill $API_PID $BFF_PID $FE_PID 2>/dev/null; exit 0" INT TERM
wait
