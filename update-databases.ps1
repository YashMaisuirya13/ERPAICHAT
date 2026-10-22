# PowerShell script to update EF Core databases in LocalDB

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Updating ERPAICHAT LocalDB Databases  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. InventoryService Database
Write-Host "`n[1/3] Updating InventoryService Database (ERP_InventoryDb)..." -ForegroundColor Yellow
dotnet ef database update --project "ERPAICHAT\InventoryService.csproj"
if ($LASTEXITCODE -eq 0) {
    Write-Host "InventoryService Database updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to update InventoryService Database." -ForegroundColor Red
}

# 2. SupplierService Database
Write-Host "`n[2/3] Updating SupplierService Database (ERP_SupplierDb)..." -ForegroundColor Yellow
dotnet ef database update --project "SupplierService\SupplierService.csproj"
if ($LASTEXITCODE -eq 0) {
    Write-Host "SupplierService Database updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to update SupplierService Database." -ForegroundColor Red
}

# 3. ProcurementService Database
Write-Host "`n[3/3] Updating ProcurementService Database (ERP_ProcurementDb)..." -ForegroundColor Yellow
dotnet ef database update --project "ProcurementService\ProcurementService.csproj"
if ($LASTEXITCODE -eq 0) {
    Write-Host "ProcurementService Database updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to update ProcurementService Database." -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Database Migration Process Completed  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
