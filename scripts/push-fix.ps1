<#
  Pushes the smoke-test fix and the Codespaces setup to main.
  Run from the repo root:  powershell -ExecutionPolicy Bypass -File scripts\push-fix.ps1
#>
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

# Keep the fixed files, move to an up-to-date main.
git stash push --include-untracked -m "sm-fix" | Out-Null
git checkout main
git pull --ff-only
git stash pop

New-Item -ItemType Directory -Force -Path '.devcontainer' | Out-Null
@'
{
  "name": "Sustain Monitor",
  "image": "mcr.microsoft.com/devcontainers/base:ubuntu",
  "features": {
    "ghcr.io/devcontainers/features/dotnet:2": { "version": "10.0" }
  },
  "forwardPorts": [5181, 5180],
  "portsAttributes": {
    "5181": { "label": "Website", "onAutoForward": "openBrowser" },
    "5180": { "label": "API", "onAutoForward": "silent" }
  },
  "postCreateCommand": "dotnet restore SustainMonitor.slnx && dotnet dev-certs https",
  "postAttachCommand": "echo 'Run: bash scripts/dev-run.sh   (website on port 5181)'",
  "customizations": {
    "vscode": {
      "extensions": ["ms-dotnettools.csdevkit", "humao.rest-client"]
    }
  }
}

'@ | Set-Content -Path '.devcontainer/devcontainer.json' -Encoding utf8
Write-Host 'wrote .devcontainer/devcontainer.json'

git add -A
git commit -m "Smoke test follows fingerprinted asset links; add Codespaces setup"
git push origin main
$url = (git remote get-url origin) -replace '\.git$', ''
Write-Host "`nPushed to main. Watch: $url/actions" -ForegroundColor Green
