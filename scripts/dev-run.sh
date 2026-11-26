#!/usr/bin/env bash
# Runs the API and website together for testing (GitHub Codespaces or any Linux/macOS machine).
# Website: port 5181 (Codespaces opens it for you).  API: port 5180.
# Sign in as the platform admin below, then register a company from the login page.
set -euo pipefail
cd "$(dirname "$0")/.."

export ADMIN_EMAIL=${ADMIN_EMAIL:-admin@sustainmonitor.local}
export ADMIN_PASSWORD=${ADMIN_PASSWORD:-Admin2026Pass}

echo "Building…"
dotnet build SustainMonitor.slnx -v q

echo "Starting API on :5180"
ASPNETCORE_ENVIRONMENT=Development UseHttpsRedirection=false \
Seed__AdminEmail="$ADMIN_EMAIL" Seed__AdminPassword="$ADMIN_PASSWORD" \
dotnet run --project src/SustainMonitor.Api --no-build --no-launch-profile --urls http://0.0.0.0:5180 &
API_PID=$!

echo "Starting website on :5181"
ASPNETCORE_ENVIRONMENT=Development UseHttpsRedirection=false ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
Api__BaseUrl=http://localhost:5180/ DataProtection__KeysFolder="$PWD/.data/web-keys" \
dotnet run --project src/SustainMonitor.Web --no-build --no-launch-profile --urls http://0.0.0.0:5181 &
WEB_PID=$!

trap 'kill $API_PID $WEB_PID 2>/dev/null' EXIT INT TERM

echo
echo "  Website:  http://localhost:5181   (in Codespaces: Ports tab → 5181 → open in browser)"
echo "  API docs: http://localhost:5180/scalar"
echo "  Admin:    $ADMIN_EMAIL / $ADMIN_PASSWORD"
echo "  Press Ctrl+C to stop."
wait
