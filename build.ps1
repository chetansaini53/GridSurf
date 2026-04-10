# GridSurf — WPF + WebView2 multi-pane browser. Requires: .NET 8 SDK + WebView2 runtime.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "=== GridSurf build (Release) ===" -ForegroundColor Cyan
Write-Host "Dir: $PSScriptRoot"
Write-Host ""

Get-Process -Name "GridSurf" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping GridSurf (PID $($_.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 400

dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\GridSurf.exe"
Write-Host ""
Write-Host "OK: $exe" -ForegroundColor Green
Write-Host ""
Write-Host "Run:" -ForegroundColor Yellow
Write-Host "  & `"$exe`""
Write-Host ""
Write-Host "Sessions: $($env:USERPROFILE)\.gridsurf\session1 .. session8" -ForegroundColor Gray
Write-Host ""
