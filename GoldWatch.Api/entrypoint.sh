#!/bin/sh
set -e

echo "==> Waiting for PostgreSQL to be ready..."
# Chờ postgres sẵn sàng (tối đa 30 giây)
RETRIES=30
until dotnet GoldWatch.Api.dll --check-db 2>/dev/null || \
      nc -z "${DB_HOST:-postgres}" "${DB_PORT:-5432}" 2>/dev/null; do
  RETRIES=$((RETRIES - 1))
  if [ "$RETRIES" -le 0 ]; then
    echo "ERROR: PostgreSQL not reachable after 30 seconds. Exiting."
    exit 1
  fi
  echo "  Postgres not ready yet, retrying in 1s... ($RETRIES left)"
  sleep 1
done

echo "==> PostgreSQL is ready. Starting API..."
exec dotnet GoldWatch.Api.dll
