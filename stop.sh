#!/usr/bin/env bash

PORTS=(5002 5001 5173)
LABELS=("API" "BFF" "Frontend")

for i in "${!PORTS[@]}"; do
  port="${PORTS[$i]}"
  label="${LABELS[$i]}"
  pids=$(lsof -ti tcp:"$port" 2>/dev/null || true)
  if [ -z "$pids" ]; then
    echo "  - $label (port $port): not running"
    continue
  fi
  echo "  ✓ Stopping $label (port $port, pids: $pids)"
  kill $pids 2>/dev/null || true
  for _ in $(seq 1 10); do
    sleep 0.3
    still=$(lsof -ti tcp:"$port" 2>/dev/null || true)
    [ -z "$still" ] && break
  done
  still=$(lsof -ti tcp:"$port" 2>/dev/null || true)
  if [ -n "$still" ]; then
    echo "    (force-killing $still)"
    kill -9 $still 2>/dev/null || true
  fi
done

echo ""
echo "All services stopped."
