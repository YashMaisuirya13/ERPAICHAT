# PowerShell script to stop all running ERPAICHAT microservices and frontend

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Stopping ERPAICHAT Local Microservices        " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$ports = @(5000, 5045, 5046, 5047, 5048, 4200)

foreach ($port in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connections) {
        $pidsStopped = @{}
        foreach ($conn in $connections) {
            $processId = $conn.OwningProcess
            if ($processId -gt 4 -and -not $pidsStopped.ContainsKey($processId)) {
                $pidsStopped[$processId] = $true
                try {
                    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
                    if ($process) {
                        Write-Host "Stopping $($process.ProcessName) (PID: $processId) on port $port..." -ForegroundColor Yellow
                        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                        Write-Host "Stopped PID $processId on port $port." -ForegroundColor Green
                    }
                } catch {
                    # Ignore errors if process already exited
                }
            }
        }
    } else {
        Write-Host "No active process found listening on port $port." -ForegroundColor Gray
    }
}

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host " All ERPAICHAT services stopped successfully.   " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
