# Stops both dev servers (API on 5198, Vite on 5173).
$apiPort = 5198
$webPort = 5173

function Stop-Port([int]$port, [string]$label) {
    $owners = @()
    try {
        $owners = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue |
                  Select-Object -ExpandProperty OwningProcess -Unique
    } catch { return }

    foreach ($procId in $owners) {
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if (-not $proc) { continue }
        if ($proc.ProcessName -notin @('OJTMISApi', 'dotnet', 'node')) {
            Write-Host "  [$label] port $port held by $($proc.ProcessName) (PID $procId) - leaving it alone"
            continue
        }
        Write-Host "  [$label] stopping $($proc.ProcessName) (PID $procId)"
        try { Stop-Process -Id $procId -Force -ErrorAction Stop } catch {}
    }
}

function Test-Free([int]$port) {
    return -not (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
}

Write-Host ""
Write-Host "Stopping dev servers..."
Stop-Port $apiPort 'api'
Stop-Port $webPort 'web'
Start-Sleep -Seconds 2

# Report honestly - only claim a port is free if it actually is.
$status = @()
foreach ($port in @($apiPort, $webPort)) {
    if (Test-Free $port) { $status += "$port free" } else { $status += "$port STILL IN USE" }
}
Write-Host "Done. $($status -join ', ')."
Write-Host ""
