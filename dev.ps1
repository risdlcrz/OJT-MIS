# Starts both API (5198) and Vite (5173) in parallel.
# Press Ctrl+C to stop both cleanly.

$ErrorActionPreference = 'Continue'
$apiPort  = 5198
$webPort  = 5173
$apiProc  = $null
$webProc  = $null

function Start-Api {
    Write-Host "Starting OJTMISApi on http://localhost:$apiPort ..."
    $apiProc = Start-Process -FilePath 'dotnet' `
        -ArgumentList 'run --project .\OJTMISApi\OJTMISApi.csproj' `
        -WorkingDirectory (Get-Location).Path `
        -RedirectStandardOutput "$env:TEMP\ojt-api.log" `
        -RedirectStandardError "$env:TEMP\ojt-api.err.log" `
        -PassThru -NoNewWindow

    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        if (Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue) {
            Write-Host "  API ready on http://localhost:$apiPort"
            return $true
        }
        if ($apiProc.HasExited) { break }
    }
    Write-Host "  API failed to start. See $env:TEMP\ojt-api.err.log"
    Get-Content "$env:TEMP\ojt-api.err.log" -Tail 30 -ErrorAction SilentlyContinue
    return $false
}

function Start-Web {
    Write-Host "Starting Vite on http://localhost:$webPort ..."
    $webProc = Start-Process -FilePath 'npx.cmd' `
        -ArgumentList 'vite' `
        -WorkingDirectory (Get-Location).Path `
        -RedirectStandardOutput "$env:TEMP\ojt-web.log" `
        -RedirectStandardError "$env:TEMP\ojt-web.err.log" `
        -PassThru -NoNewWindow

    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 500
        if (Get-NetTCPConnection -State Listen -LocalPort $webPort -ErrorAction SilentlyContinue) {
            Write-Host "  Vite ready on http://localhost:$webPort"
            return $true
        }
        if ($webProc.HasExited) { break }
    }
    Write-Host "  Vite failed to start. See $env:TEMP\ojt-web.err.log"
    Get-Content "$env:TEMP\ojt-web.err.log" -Tail 30 -ErrorAction SilentlyContinue
    return $false
}

function Stop-All {
    Write-Host "`nStopping both servers..."
    if ($apiProc -and -not $apiProc.HasExited) { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue }
    if ($webProc -and -not $webProc.HasExited) { Stop-Process -Id $webProc.Id -Force -ErrorAction SilentlyContinue }
    # Also clean up any stragglers on our ports
    foreach ($port in @($apiPort, $webPort)) {
        Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object {
                $p = Get-Process -Id $_ -ErrorAction SilentlyContinue
                if ($p -and $p.ProcessName -in @('OJTMISApi','dotnet','node')) {
                    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
                }
            }
    }
    Write-Host "Done."
}

# Trap Ctrl+C
try { [System.Console]::CancelKeyPress += { Stop-All; exit 0 } } catch {}

Write-Host ""
Write-Host "=== Starting OJT-MIS Dev Environment ==="
Write-Host ""

$apiOk = Start-Api
$webOk = Start-Web

if (-not $apiOk -or -not $webOk) {
    Stop-All
    exit 1
}

Write-Host ""
Write-Host "  Frontend : http://localhost:$webPort"
Write-Host "  API      : http://localhost:$apiPort"
Write-Host "  Login    : hradmin@ojtmis.local / Admin@12345"
Write-Host ""
Write-Host "  Press Ctrl+C to stop both servers."
Write-Host ""

# Keep script alive until Ctrl+C
while (-not $apiProc.HasExited -and -not $webProc.HasExited) {
    Start-Sleep -Seconds 1
}

# If one dies unexpectedly, stop the other
Stop-All