<#
  Adds the "Test deployment" GitHub Action and pushes the new test files.
  Run from the repo root:  powershell -ExecutionPolicy Bypass -File scripts\push-test-deployment.ps1
#>
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

New-Item -ItemType Directory -Force -Path '.github/workflows' | Out-Null
@'
name: Test deployment

# Builds the Docker images, starts the full system on the GitHub runner,
# runs the smoke test against it, then tears it down. No server needed.
on:
  push:
    branches: [main, dotnet-rebuild]
  pull_request:
  workflow_dispatch:

jobs:
  deploy-test:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    env:
      DOMAIN: localhost
      ADMIN_EMAIL: admin@ci.local
      ADMIN_PASSWORD: Ci2026AdminPass
    steps:
      - uses: actions/checkout@v4

      - name: Build images and start API + website
        working-directory: deploy
        run: docker compose -f docker-compose.yml -f docker-compose.test.yml up -d --build api web

      - name: Smoke test the running system
        run: bash scripts/smoke-test.sh

      - name: Show container logs
        if: always()
        working-directory: deploy
        run: docker compose -f docker-compose.yml -f docker-compose.test.yml logs --no-color api web

      - name: Tear down
        if: always()
        working-directory: deploy
        run: docker compose -f docker-compose.yml -f docker-compose.test.yml down -v

'@ | Set-Content -Path '.github/workflows/deploy-test.yml' -Encoding utf8
Write-Host 'wrote .github/workflows/deploy-test.yml'

git add -A
git commit -m "Add test deployment on GitHub Actions and Codespaces setup" `
           -m "Every push builds the Docker images, starts API + website on the runner and runs scripts/smoke-test.sh. .devcontainer lets you run the system in GitHub Codespaces (bash scripts/dev-run.sh)."
git push
$url = (git remote get-url origin) -replace '\.git$', ''
Write-Host "`nPushed. Watch it run: $url/actions" -ForegroundColor Green
