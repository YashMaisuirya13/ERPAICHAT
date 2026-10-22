# ==============================================================================
# ERPAICHAT - Seed Enterprise Databases Script
# Seeds rich realistic data into ERP_InventoryDb, ERP_SupplierDb, and ERP_ProcurementDb
# ==============================================================================

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Seeding ERPAICHAT LocalDB with Enterprise Mock Data    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$connStrMaster = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"

function Execute-SqlNonQuery($dbName, $sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=$dbName;Trusted_Connection=True;TrustServerCertificate=True;")
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $rowsAffected = $cmd.ExecuteNonQuery()
        return $rowsAffected
    }
    finally {
        $conn.Close()
    }
}

# ------------------------------------------------------------------------------
# 1. SEED SUPPLIERS (ERP_SupplierDb)
# ------------------------------------------------------------------------------
Write-Host "`n[1/3] Seeding ERP_SupplierDb (Verified Suppliers)..." -ForegroundColor Yellow

$supplierGuids = @{
    TechEdge = "11111111-1111-1111-1111-111111111111"
    ApexHardware = "22222222-2222-2222-2222-222222222222"
    SiliconValley = "33333333-3333-3333-3333-333333333333"
    ErgoOffice = "44444444-4444-4444-4444-444444444444"
}

$sqlSuppliers = @"
IF NOT EXISTS (SELECT 1 FROM [Suppliers] WHERE [SupplierCode] = 'SUP-TECH-01')
BEGIN
    INSERT INTO [Suppliers] ([SupplierGuid], [SupplierCode], [Name], [Email], [Phone], [Address], [IsActive])
    VALUES 
    ('$($supplierGuids.TechEdge)', 'SUP-TECH-01', 'TechEdge Distribution Corp', 'orders@techedge.com', '+1 (555) 019-8234', '1400 Tech Hub Blvd, Chicago, IL 60601', 1),
    ('$($supplierGuids.ApexHardware)', 'SUP-APEX-02', 'Apex Hardware Global Ltd', 'procurement@apexhardware.com', '+1 (555) 028-9182', '800 Commerce Way, Dallas, TX 75201', 1),
    ('$($supplierGuids.SiliconValley)', 'SUP-SVS-03', 'Silicon Valley Semiconductor', 'supply@svsemi.com', '+1 (555) 037-1928', '250 Innovation Pkwy, San Jose, CA 95110', 1),
    ('$($supplierGuids.ErgoOffice)', 'SUP-EOS-04', 'Ergonomic Office Solutions', 'sales@ergosolutions.com', '+1 (555) 046-2819', '55 Manhattan Ave, New York, NY 10001', 1);
    PRINT 'Inserted 4 Verified Suppliers.';
END
ELSE
BEGIN
    PRINT 'Suppliers already exist. Skipping insert.';
END
"@

Execute-SqlNonQuery "ERP_SupplierDb" $sqlSuppliers
Write-Host "Supplier database seeded successfully!" -ForegroundColor Green

# ------------------------------------------------------------------------------
# 2. SEED PRODUCTS & INVENTORY (ERP_InventoryDb)
# ------------------------------------------------------------------------------
Write-Host "`n[2/3] Seeding ERP_InventoryDb (Products & Stock Balances)..." -ForegroundColor Yellow

$productGuids = @{
    ThinkPad = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    DellLatitude = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    UltraSharp4K = "cccccccc-cccc-cccc-cccc-cccccccccccc"
    MXMasterMouse = "dddddddd-dddd-dddd-dddd-dddddddddddd"
    CiscoSwitch = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"
    ErgoDesk = "ffffffff-ffff-ffff-ffff-ffffffffffff"
}

$sqlProducts = @"
-- Clear old single test product if needed or ensure our structured items exist
IF NOT EXISTS (SELECT 1 FROM [Products] WHERE [SKU] = 'LAP-X1C')
BEGIN
    INSERT INTO [Products] ([ProductGuid], [SKU], [Name], [UnitPrice], [MinimumStock])
    VALUES
    ('$($productGuids.ThinkPad)', 'LAP-X1C', 'ThinkPad X1 Carbon Gen 12 Laptop', 1499.00, 15),
    ('$($productGuids.DellLatitude)', 'LAP-5540', 'Dell Latitude 5540 Enterprise Laptop', 1199.00, 20),
    ('$($productGuids.UltraSharp4K)', 'DIS-4K27', 'Dell UltraSharp 27-inch 4K USB-C Hub Monitor', 549.00, 12),
    ('$($productGuids.MXMasterMouse)', 'PER-MXM3', 'Logitech MX Master 3S Wireless Mouse', 99.00, 30),
    ('$($productGuids.CiscoSwitch)', 'NET-C9200', 'Cisco Catalyst 9200L 24-Port Gigabit Switch', 1850.00, 5),
    ('$($productGuids.ErgoDesk)', 'FUR-DESK2', 'ErgoPro Electric Height-Adjustable Standing Desk', 620.00, 8);
    PRINT 'Inserted 6 Core Products.';
END

-- Seed Inventory Levels for each product
DECLARE @p1 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'LAP-X1C');
DECLARE @p2 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'LAP-5540');
DECLARE @p3 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'DIS-4K27');
DECLARE @p4 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'PER-MXM3');
DECLARE @p5 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'NET-C9200');
DECLARE @p6 INT = (SELECT TOP 1 Id FROM Products WHERE SKU = 'FUR-DESK2');

-- Clean and re-seed InventoryItems to guarantee reliable numbers
DELETE FROM [InventoryItems];

INSERT INTO [InventoryItems] ([InventoryGuid], [ProductId], [Quantity], [WarehouseCode])
VALUES
-- Product 1: ThinkPad - Low Stock (4 in stock vs 15 minimum!) -> Trigger for AI Low Stock & PO workflow!
(NEWID(), @p1, 4, 'WH-CENTRAL'),

-- Product 2: Dell Latitude - Healthy Stock (35 in stock vs 20 minimum)
(NEWID(), @p2, 35, 'WH-CENTRAL'),

-- Product 3: Dell 4K Monitor - Low Stock (3 in stock vs 12 minimum!)
(NEWID(), @p3, 3, 'WH-EAST'),

-- Product 4: Logitech Mouse - Healthy Stock (85 in stock vs 30 minimum)
(NEWID(), @p4, 85, 'WH-WEST'),

-- Product 5: Cisco Switch - Healthy Stock (8 in stock vs 5 minimum)
(NEWID(), @p5, 8, 'WH-EAST'),

-- Product 6: Standing Desk - Healthy Stock (14 in stock vs 8 minimum)
(NEWID(), @p6, 14, 'WH-CENTRAL');

PRINT 'Seeded Inventory Items with realistic Stock vs Minimum thresholds.';
"@

Execute-SqlNonQuery "ERP_InventoryDb" $sqlProducts
Write-Host "Products and Inventory seeded successfully!" -ForegroundColor Green

# ------------------------------------------------------------------------------
# 3. SEED PURCHASE ORDERS (ERP_ProcurementDb)
# ------------------------------------------------------------------------------
Write-Host "`n[3/3] Seeding ERP_ProcurementDb (Purchase Orders & Line Items)..." -ForegroundColor Yellow

$sqlProcurement = @"
IF NOT EXISTS (SELECT 1 FROM [PurchaseOrders] WHERE [PONumber] = 'PO-2026-1001')
BEGIN
    -- PO 1: Approved PO for 20x ThinkPad Laptops with TechEdge ($29,980.00)
    INSERT INTO [PurchaseOrders] ([PurchaseOrderGuid], [PONumber], [SupplierGuid], [OrderDate], [Status], [TotalAmount], [ApprovedAt])
    VALUES ('99999999-1001-1001-1001-100110011001', 'PO-2026-1001', '$($supplierGuids.TechEdge)', DATEADD(DAY, -5, GETUTCDATE()), 'Approved', 29980.00, DATEADD(DAY, -4, GETUTCDATE()));
    DECLARE @po1_id INT = SCOPE_IDENTITY();

    INSERT INTO [PurchaseOrderItems] ([PurchaseOrderItemGuid], [PurchaseOrderId], [ProductGuid], [Quantity], [UnitPrice], [TotalAmount])
    VALUES (NEWID(), @po1_id, '$($productGuids.ThinkPad)', 20, 1499.00, 29980.00);

    -- PO 2: Submitted (Pending Manager Approval) for 15x Dell 4K Monitors with Apex Hardware ($8,235.00)
    INSERT INTO [PurchaseOrders] ([PurchaseOrderGuid], [PONumber], [SupplierGuid], [OrderDate], [Status], [TotalAmount])
    VALUES ('99999999-1002-1002-1002-100210021002', 'PO-2026-1002', '$($supplierGuids.ApexHardware)', DATEADD(DAY, -1, GETUTCDATE()), 'Submitted', 8235.00);
    DECLARE @po2_id INT = SCOPE_IDENTITY();

    INSERT INTO [PurchaseOrderItems] ([PurchaseOrderItemGuid], [PurchaseOrderId], [ProductGuid], [Quantity], [UnitPrice], [TotalAmount])
    VALUES (NEWID(), @po2_id, '$($productGuids.UltraSharp4K)', 15, 549.00, 8235.00);

    -- PO 3: Draft PO for 50x Logitech Mice with TechEdge ($4,950.00)
    INSERT INTO [PurchaseOrders] ([PurchaseOrderGuid], [PONumber], [SupplierGuid], [OrderDate], [Status], [TotalAmount])
    VALUES ('99999999-1003-1003-1003-100310031003', 'PO-2026-1003', '$($supplierGuids.TechEdge)', GETUTCDATE(), 'Draft', 4950.00);
    DECLARE @po3_id INT = SCOPE_IDENTITY();

    INSERT INTO [PurchaseOrderItems] ([PurchaseOrderItemGuid], [PurchaseOrderId], [ProductGuid], [Quantity], [UnitPrice], [TotalAmount])
    VALUES (NEWID(), @po3_id, '$($productGuids.MXMasterMouse)', 50, 99.00, 4950.00);

    -- PO 4: Received PO for 5x Cisco Switches with Silicon Valley Semi ($9,250.00)
    INSERT INTO [PurchaseOrders] ([PurchaseOrderGuid], [PONumber], [SupplierGuid], [OrderDate], [Status], [TotalAmount], [ApprovedAt], [ReceivedAt])
    VALUES ('99999999-1004-1004-1004-100410041004', 'PO-2026-1004', '$($supplierGuids.SiliconValley)', DATEADD(DAY, -10, GETUTCDATE()), 'Received', 9250.00, DATEADD(DAY, -9, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()));
    DECLARE @po4_id INT = SCOPE_IDENTITY();

    INSERT INTO [PurchaseOrderItems] ([PurchaseOrderItemGuid], [PurchaseOrderId], [ProductGuid], [Quantity], [UnitPrice], [TotalAmount])
    VALUES (NEWID(), @po4_id, '$($productGuids.CiscoSwitch)', 5, 1850.00, 9250.00);

    PRINT 'Inserted 4 Sample Purchase Orders spanning Draft, Submitted, Approved, and Received stages.';
END
ELSE
BEGIN
    PRINT 'Purchase Orders already exist. Skipping insert.';
END
"@

Execute-SqlNonQuery "ERP_ProcurementDb" $sqlProcurement
Write-Host "Purchase Orders seeded successfully!" -ForegroundColor Green

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "   Enterprise Mock Data Seeding Successfully Completed!   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
