<#
  Publishes the API and web app for Windows Server + IIS.
  Run from the repo root:  powershell -ExecutionPolicy Bypass -File scripts\publish-iis.ps1
  Output: publish\api and publish\web — copy each to its IIS site folder. See DEPLOY.md, "Windows Server (IIS)".
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'publish'

Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
dotnet test "$root\SustainMonitor.slnx" -c Release
dotnet publish "$root\src\SustainMonitor.Api\SustainMonitor.Api.csproj" -c Release -o "$out\api"
dotnet publish "$root\src\SustainMonitor.Web\SustainMonitor.Web.csproj" -c Release -o "$out\web"

# Production settings templates (edit after copying to the server).
@'
{
  "ConnectionStrings": { "Default": "Data Source=D:\\SustainMonitor\\data\\sustainmonitor.db" },
  "Storage": { "EvidenceFolder": "D:\\SustainMonitor\\data\\evidence" },
  "DataProtection": { "KeysFolder": "D:\\SustainMonitor\\data\\keys" },
  "Seed": { "AdminEmail": "admin@example.com", "AdminPassword": "ChangeThis2026Now" },
  "Smtp": { "Host": "", "User": "", "Password": "", "From": "no-reply@example.com" }
}
'@ | Set-Content "$out\api\appsettings.Production.json"

@'
{
  "Api": { "BaseUrl": "http://localhost:8081/" },
  "DataProtection": { "KeysFolder": "D:\\SustainMonitor\\web-keys" }
}
'@ | Set-Content "$out\web\appsettings.Production.json"

Write-Host "`nPublished to $out" -ForegroundColor Green
Write-Host "Next: follow DEPLOY.md > Windows Server (IIS)."
