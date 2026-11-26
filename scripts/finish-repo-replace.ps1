<#
  Finishes replacing the Laravel app with the .NET rebuild in this repo.
  Run from the repo root:  powershell -ExecutionPolicy Bypass -File scripts\finish-repo-replace.ps1

  1. Moves your current changes onto a new branch (main is not changed).
  2. Removes the Laravel + Filament files (they stay in git history).
  3. Commits and pushes the branch. Open the pull request link it prints; GitHub Actions builds and tests it.
#>
param([string]$Branch = 'dotnet-rebuild', [switch]$NoPush)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path '.git')) { throw 'Run this inside the sustain-monitor git repository.' }
if (-not (Test-Path 'SustainMonitor.slnx')) { throw 'SustainMonitor.slnx not found: the .NET files are missing.' }

$current = (git rev-parse --abbrev-ref HEAD).Trim()
if ($current -ne $Branch) { git checkout -b $Branch }

$laravel = @(
    '.editorconfig', '.env.example', '.gitattributes', 'artisan', 'composer.json', 'composer.lock',
    'package.json', 'package-lock.json', 'phpunit.xml', 'vite.config.js',
    'app', 'bootstrap', 'config', 'database', 'public', 'resources', 'routes', 'storage', 'vendor', 'node_modules',
    'tests/Feature', 'tests/Unit', 'tests/Pest.php', 'tests/TestCase.php'
)
foreach ($p in $laravel) {
    if (Test-Path $p) {
        git rm -r -q --ignore-unmatch -- $p 2>$null
        if (Test-Path $p) { Remove-Item $p -Recurse -Force }
        Write-Host "removed $p"
    }
}

# GitHub Actions workflow (written here because remote tools may not create files under .github).
New-Item -ItemType Directory -Force -Path '.github/workflows' | Out-Null
@'
name: CI

on:
  push:
    branches: [main]
  pull_request:

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore SustainMonitor.slnx
      - run: dotnet build SustainMonitor.slnx --no-restore -c Release
      - run: dotnet test SustainMonitor.slnx --no-build -c Release --logger "trx;LogFileName=results.trx"
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/TestResults/*.trx"

  images:
    # On main: build both Docker images and push them to GitHub Container Registry.
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    needs: test
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - name: Lower-case image prefix
        run: echo "PREFIX=ghcr.io/${GITHUB_REPOSITORY,,}" >> "$GITHUB_ENV"
      - uses: docker/build-push-action@v6
        with:
          context: .
          file: src/SustainMonitor.Api/Dockerfile
          push: true
          tags: ${{ env.PREFIX }}-api:latest,${{ env.PREFIX }}-api:${{ github.sha }}
      - uses: docker/build-push-action@v6
        with:
          context: .
          file: src/SustainMonitor.Web/Dockerfile
          push: true
          tags: ${{ env.PREFIX }}-web:latest,${{ env.PREFIX }}-web:${{ github.sha }}

'@ | Set-Content -Path '.github/workflows/ci.yml' -Encoding utf8
Write-Host 'wrote .github/workflows/ci.yml'

git add -A
git status --short | Select-Object -First 15
git commit -m "Rebuild Sustain Monitor in .NET 10 (API, website, tests, deployment)" `
           -m "Replaces the Laravel + Filament app: GBI-aligned scoring, CO-tickets, companies and roles, e-certificates, Docker/IIS deployment and CI. The Laravel version remains in git history."

if (-not $NoPush) {
    git push -u origin $Branch
    $url = (git remote get-url origin) -replace '\.git$', ''
    Write-Host "`nPushed. Open the pull request: $url/compare/main...$Branch" -ForegroundColor Green
}
