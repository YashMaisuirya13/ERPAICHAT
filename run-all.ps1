# PowerShell script to run all ERPAICHAT microservices and frontend locally

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Launching ERPAICHAT Local Microservices & App   " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$scriptPath = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }

# Helper function to launch service in a new PowerShell window
function Start-ServiceWindow {
    param (
        [string]$ServiceName,
        [string]$FolderPath,
        [string]$Command,
        [int]$Port
    )
    Write-Host "-> Launching $ServiceName on port $Port..." -ForegroundColor Yellow
    $fullPath = Join-Path $scriptPath $FolderPath
    $psCommand = "Set-Location '$fullPath'; try { `$host.UI.RawUI.WindowTitle = '$ServiceName (Port $Port)' } catch {}; $Command"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $psCommand
}

# Pre-build solution once to avoid simultaneous file locking on Shared.Events.dll
Write-Host "-> Building solution once to prevent multi-process file locking..." -ForegroundColor Cyan
dotnet build "$scriptPath\ERPAICHAT.slnx" -v q --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Please resolve the errors above." -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "Build ready! Starting services with --no-build for instant startup..." -ForegroundColor Green

# 1. Start InventoryService
Start-ServiceWindow -ServiceName "InventoryService" -FolderPath "ERPAICHAT" -Command "dotnet run --no-build --launch-profile http" -Port 5045

# 2. Start SupplierService
Start-ServiceWindow -ServiceName "SupplierService" -FolderPath "SupplierService" -Command "dotnet run --no-build --launch-profile http" -Port 5046

# 3. Start ProcurementService
Start-ServiceWindow -ServiceName "ProcurementService" -FolderPath "ProcurementService" -Command "dotnet run --no-build --launch-profile http" -Port 5047

# 4. Start AIAgentService
Start-ServiceWindow -ServiceName "AIAgentService" -FolderPath "AIAgentService" -Command "dotnet run --no-build --launch-profile http" -Port 5048

# 5. Start ApiGateway
Start-ServiceWindow -ServiceName "ApiGateway" -FolderPath "ApiGateway" -Command "dotnet run --no-build --launch-profile http" -Port 5000

# 6. Start Angular Frontend
Start-ServiceWindow -ServiceName "Angular Frontend" -FolderPath "erp-frontend" -Command "npm start" -Port 4200

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host " All 6 Services launched in separate windows!   " -ForegroundColor Green
Write-Host "-------------------------------------------------" -ForegroundColor Green
Write-Host " Api Gateway:      http://localhost:5000" -ForegroundColor White
Write-Host " Inventory API:    http://localhost:5045/swagger" -ForegroundColor White
Write-Host " Supplier API:     http://localhost:5046/swagger" -ForegroundColor White
Write-Host " Procurement API:  http://localhost:5047/swagger" -ForegroundColor White
Write-Host " AIAgent API:      http://localhost:5048/swagger" -ForegroundColor White
Write-Host " Angular Frontend: http://localhost:4200" -ForegroundColor White
Write-Host "=================================================" -ForegroundColor Green
Write-Host "To stop all services, run: .\stop-all.ps1" -ForegroundColor Gray
