# Frees the dev ports (API 5198, Vite 5173) then starts both apps.
# Prevents the "address already in use" crash that happens when an
# earlier run is still alive in another terminal.

$ErrorActionPreference = 'Continue'

$apiPort = 5198
$webPort = 5173

function Release-Port([int]$port, [string]$label) {
    $owners = @()
    try {
        $owners = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue |
                  Select-Object -ExpandProperty OwningProcess -Unique
    } catch {
        Write-Host "  [$label] could not inspect port $port (continuing)"
        return
    }

    foreach ($procId in $owners) {
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if (-not $proc) { continue }

        # Never touch processes that aren't ours (e.g. Visual Studio tooling).
        if ($proc.ProcessName -notin @('OJTMISApi', 'dotnet')) {
            Write-Host "  [$label] port $port is held by $($proc.ProcessName) (PID $procId) - leaving it alone"
            continue
        }

        Write-Host "  [$label] stopping $($proc.ProcessName) (PID $procId) on port $port"
        try { Stop-Process -Id $procId -Force -ErrorAction Stop } catch { Write-Host "  [$label] could not stop PID $procId" }
    }
}

Write-Host ""
Write-Host "Freeing dev ports..."
Release-Port $apiPort 'api'
Release-Port $webPort 'web'
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "Starting OJTMISApi on http://localhost:$apiPort ..."
$api = Start-Process -FilePath 'dotnet' `
    -ArgumentList 'run --project .\OJTMISApi\OJTMISApi.csproj' `
    -WorkingDirectory (Get-Location).Path `
    -RedirectStandardOutput "$env:TEMP\ojt-api.log" `
    -RedirectStandardError "$env:TEMP\ojt-api.err.log" `
    -PassThru -NoNewWindow

# Wait for the API to actually bind before handing over to Vite.
$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 750
    if (Get-NetTCPConnection -State Listen -LocalPort $apiPort -ErrorAction SilentlyContinue) { $ready = $true; break }
    if ($api.HasExited) { break }
}

if ($ready) {
    Write-Host "  API is up on http://localhost:$apiPort"
} else {
    Write-Host "  API did not start. See $env:TEMP\ojt-api.err.log"
    Get-Content "$env:TEMP\ojt-api.err.log" -Tail 20 -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Starting Vite on http://localhost:$webPort ..."
Write-Host ""
Write-Host "  Frontend : http://localhost:$webPort"
Write-Host "  API      : http://localhost:$apiPort"
Write-Host "  Login    : hradmin@ojtmis.local / Admin@12345"
Write-Host ""
Write-Host "  Press Ctrl+C here, then run 'npm run stop' to shut everything down."
Write-Host ""

Set-Location (Get-Location).Path
& npx vite
