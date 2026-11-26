#!/bin/sh
set -e

echo "==> Starting GoldWatch API..."
exec dotnet GoldWatch.Api.dll
