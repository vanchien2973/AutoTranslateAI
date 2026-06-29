#!/usr/bin/env bash
# Phase-0 acceptance smoke test: brings up the stack, checks the API is live, that it can reach
# Postgres + RabbitMQ + R2, and that the Worker's media CLI tools run.
# Run from the solution root:  ./scripts/smoke-test.sh
set -euo pipefail
cd "$(dirname "$0")/.."

API_PORT="${API_PORT:-8080}"
base="http://localhost:${API_PORT}"

echo "==> Building & starting the stack..."
docker compose up -d --build

echo "==> Waiting for the API liveness endpoint (${base}/health)..."
for i in $(seq 1 60); do
  if curl -fsS "${base}/health" >/dev/null 2>&1; then break; fi
  sleep 2
  [ "$i" = "60" ] && { echo "API did not become live in time"; docker compose logs --tail=50 api; exit 1; }
done
echo "    liveness OK (200 Healthy)"

echo "==> Readiness — DB + RabbitMQ + R2 (${base}/health/ready):"
code="$(curl -s -o /tmp/ata_ready.json -w '%{http_code}' "${base}/health/ready" || true)"
cat /tmp/ata_ready.json; echo
echo "    HTTP ${code}  (200 = all dependencies healthy, 503 = at least one down — see JSON above)"

echo "==> Worker CLI tools:"
docker compose run --rm --entrypoint verify-tools worker

echo
echo "Done. If readiness returned 200 and the tools verified, Phase-0 acceptance is met."
