param(
    [string]$Configuration = "Release",
    # Path to dotnet.exe of a .NET 10 SDK. When omitted, 'dotnet' on PATH is used.
    [string]$Dotnet = ""
)

$ErrorActionPreference = "Continue"
Set-Location -Path $PSScriptRoot

# Process-scoped only: suppress the dotnet first-run banner and opt out of CLI telemetry.
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

# dotnet outputs UTF-8. Decode native process output as UTF-8
# so that non-ASCII (Japanese) messages are not garbled in the log.
$prevOutputEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$proj    = "LocalModAssistant\LocalModAssistant.csproj"
$publish = Join-Path $PSScriptRoot "publish"
$exe     = Join-Path $publish "LocalModAssistant.exe"
$log     = Join-Path $PSScriptRoot "build.log"

Write-Host "============================================================"
Write-Host " LocalModAssistant - Publish ($Configuration)"
Write-Host "============================================================"

# ---- Locate dotnet --------------------------------------------------
if ($Dotnet) {
    if (-not (Test-Path $Dotnet)) {
        [Console]::OutputEncoding = $prevOutputEncoding
        Write-Host "[ERROR] dotnet was not found: $Dotnet"
        exit 1
    }
}
else {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $cmd) {
        [Console]::OutputEncoding = $prevOutputEncoding
        Write-Host "[ERROR] dotnet was not found. Install the .NET 10 SDK or pass -Dotnet <path to dotnet.exe>."
        exit 1
    }
    $Dotnet = $cmd.Source
}
Write-Host "[INFO] dotnet: $Dotnet"

$output = @()
$rc = 0

try {
    $output += & $Dotnet --version 2>&1 | ForEach-Object { "dotnet SDK: $_" }

    # Remove the previous output so that only the current exe is left.
    if (Test-Path $publish) { Remove-Item -Path $publish -Recurse -Force }

    Write-Host "[INFO] Publishing..."
    $output += & $Dotnet publish $proj -c $Configuration -o $publish -v normal 2>&1
    $rc = $LASTEXITCODE
}
finally {
    [Console]::OutputEncoding = $prevOutputEncoding
}

# ---- Write log ------------------------------------------------------
$output | Out-File -FilePath $log -Encoding utf8

# ---- Summary --------------------------------------------------------
Write-Host ""
Write-Host "============================================================"
Write-Host " Summary"
Write-Host "============================================================"
if ($rc -eq 0 -and (Test-Path $exe)) {
    $size = "{0:N1} MB" -f ((Get-Item $exe).Length / 1MB)
    Write-Host "  OK      publish\LocalModAssistant.exe ($size)" -ForegroundColor Green

    $others = @(Get-ChildItem -Path $publish -File | Where-Object { $_.Name -ne "LocalModAssistant.exe" })
    if ($others.Count -gt 0) {
        Write-Host "  [WARN]  Other files in publish\: $($others.Name -join ', ')" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  FAILED  (exit code $rc)" -ForegroundColor Red
    if ($rc -eq 0) { $rc = 1 }
}

$warnings = $output | Select-String -Pattern "warning" -SimpleMatch
if ($warnings) {
    Write-Host ""
    Write-Host "---- Warnings ----"
    $warnings | ForEach-Object { Write-Host $_ }
}

if ($rc -ne 0) {
    Write-Host ""
    Write-Host "---- Errors ----"
    $output | Select-String -Pattern "error" -SimpleMatch | ForEach-Object { Write-Host $_ }
}

Write-Host ""
Write-Host "Full log: $log"

if ($rc -ne 0) { exit 1 }
exit 0
