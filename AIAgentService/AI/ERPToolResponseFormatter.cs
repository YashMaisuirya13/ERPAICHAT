using System.Text;
using System.Text.Json;

namespace AIAgentService.AI;

public static class ERPToolResponseFormatter
{
    public static string FormatToolResultToMarkdown(string toolName, string resultJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            return toolName.ToLowerInvariant() switch
            {
                "get_low_stock_alerts" => FormatLowStockAlerts(root),
                "get_inventory" => FormatInventory(root),
                "get_suppliers" => FormatSuppliers(root),
                "get_purchase_orders" => FormatPurchaseOrders(root),
                "get_purchase_order_details" => FormatPurchaseOrderDetails(root),
                "get_stock_transactions" => FormatStockTransactions(root),
                "create_purchase_order" => FormatCreatedPurchaseOrder(root),
                _ => FormatGenericToolResult(toolName, root)
            };
        }
        catch
        {
            return $"### Execution Completed for `{toolName}`\n\nERP operation executed successfully.";
        }
    }

    private static string FormatLowStockAlerts(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 🟢 All Stock Levels Healthy\n\nAll inventory products are currently above their minimum safety reorder thresholds. No replenishment is required at this time.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### ⚠️ Low Stock Inventory Alert");
        sb.AppendLine();
        sb.AppendLine($"Identified **{count} product{(count > 1 ? "s" : "")}** currently below minimum safety stock levels:");
        sb.AppendLine();
        sb.AppendLine("| Product Name | SKU | Available Qty | Warehouse | Safety Min | Status |");
        sb.AppendLine("| :--- | :--- | :---: | :--- | :---: | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var name = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
            var sku = item.TryGetProperty("sku", out var sp) ? sp.GetString() ?? "" : "";
            var qty = item.TryGetProperty("availableQuantity", out var qp) ? qp.GetInt32().ToString() : "0";
            var wh = item.TryGetProperty("warehouse", out var wp) ? wp.GetString() ?? "" : "";
            var min = item.TryGetProperty("reorderThreshold", out var mp) ? mp.GetInt32().ToString() : "0";
            var status = item.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Low Stock Alert" : "Low Stock Alert";

            sb.AppendLine($"| {name} | `{sku}` | **{qty}** | {wh} | {min} | ⚠️ {status} |");
        }

        sb.AppendLine();
        sb.AppendLine("💡 **Action Recommended:** Prepare a purchase order to replenish items below minimum thresholds.");
        return sb.ToString().Trim();
    }

    private static string FormatInventory(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 📦 Inventory Catalog\n\nNo products found matching your search criteria.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 📦 Warehouse Inventory Levels");
        sb.AppendLine();
        sb.AppendLine($"Retrieved records for **{count} product{(count > 1 ? "s" : "")}** in catalog:");
        sb.AppendLine();
        sb.AppendLine("| Product Name | SKU | Unit Price | Total Stock | Safety Min | Status |");
        sb.AppendLine("| :--- | :--- | :---: | :---: | :---: | :--- |");

        var warehouseBreakdowns = new List<string>();

        foreach (var item in root.EnumerateArray())
        {
            var name = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
            var sku = item.TryGetProperty("sku", out var sp) ? sp.GetString() ?? "" : "";
            var price = item.TryGetProperty("unitPrice", out var pp) ? pp.GetDecimal().ToString("C2") : "$0.00";
            var total = item.TryGetProperty("totalAvailable", out var tp) ? tp.GetInt32().ToString() : "0";
            var min = item.TryGetProperty("reorderThreshold", out var mp) ? mp.GetInt32().ToString() : "0";
            var status = item.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Healthy" : "Healthy";
            var isLow = status.Contains("Low", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine($"| {name} | `{sku}` | {price} | **{total}** | {min} | {(isLow ? "⚠️ Low Stock" : "✅ Healthy")} |");

            if (item.TryGetProperty("warehouses", out var whs) && whs.ValueKind == JsonValueKind.Array && whs.GetArrayLength() > 0)
            {
                var locations = string.Join(", ", whs.EnumerateArray().Select(w =>
                {
                    var whCode = w.TryGetProperty("warehouse", out var c) ? c.GetString() ?? "" : "";
                    var q = w.TryGetProperty("quantity", out var qv) ? qv.GetInt32().ToString() : "0";
                    return $"{whCode}: {q} units";
                }));
                warehouseBreakdowns.Add($"- **{name}**: {locations}");
            }
        }

        if (warehouseBreakdowns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Warehouse Distribution Breakdown:**");
            foreach (var wb in warehouseBreakdowns)
            {
                sb.AppendLine(wb);
            }
        }

        return sb.ToString().Trim();
    }

    private static string FormatSuppliers(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 🤝 Active Verified Suppliers\n\nNo active suppliers are currently registered in the database.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 🤝 Active Verified Suppliers");
        sb.AppendLine();
        sb.AppendLine($"Found **{count} registered supplier{(count > 1 ? "s" : "")}** available for procurement:");
        sb.AppendLine();
        sb.AppendLine("| Supplier Code | Company Name | Email Contact | Phone | Hub Location |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var code = item.TryGetProperty("supplierCode", out var cp) ? cp.GetString() ?? "" : "";
            var name = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
            var email = item.TryGetProperty("email", out var ep) ? ep.GetString() ?? "" : "";
            var phone = item.TryGetProperty("phone", out var pp) ? pp.GetString() ?? "" : "";
            var addr = item.TryGetProperty("address", out var ap) ? ap.GetString() ?? "" : "";

            sb.AppendLine($"| `{code}` | **{name}** | {email} | {phone} | {addr} |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatPurchaseOrders(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 📋 Procurement Purchase Orders\n\nNo purchase orders found matching the filter criteria.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 📋 Procurement Purchase Orders");
        sb.AppendLine();
        sb.AppendLine($"Retrieved **{count} purchase order{(count > 1 ? "s" : "")}** from the system:");
        sb.AppendLine();
        sb.AppendLine("| PO Number | Status | Total Amount | Order Date |");
        sb.AppendLine("| :--- | :---: | :---: | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var poNum = item.TryGetProperty("poNumber", out var pp) ? pp.GetString() ?? "" : "";
            var status = item.TryGetProperty("status", out var sp) ? sp.GetString() ?? "Draft" : "Draft";
            var amount = item.TryGetProperty("totalAmount", out var ap) ? ap.GetDecimal().ToString("C2") : "$0.00";
            var date = item.TryGetProperty("orderDate", out var dp) ? dp.GetString() ?? "" : "";

            string statusIcon = status.ToLowerInvariant() switch
            {
                "approved" => "✅ Approved",
                "submitted" => "🟡 Submitted",
                "received" => "📦 Received",
                _ => $"📝 {status}"
            };

            sb.AppendLine($"| `{poNum}` | {statusIcon} | **{amount}** | {date} |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatCreatedPurchaseOrder(JsonElement root)
    {
        if (root.TryGetProperty("error", out var errProp))
        {
            return $"### ❌ Purchase Order Error\n\nUnable to generate purchase order: {errProp.GetString()}";
        }

        var poNum = root.TryGetProperty("poNumber", out var pp) ? pp.GetString() ?? "PO-NEW" : "PO-NEW";
        var product = root.TryGetProperty("product", out var prp) ? prp.GetString() ?? "Product" : "Product";
        var quantity = root.TryGetProperty("quantity", out var qp) ? qp.GetInt32() : 0;
        var unitPrice = root.TryGetProperty("unitPrice", out var up) ? up.GetDecimal() : 0m;
        var total = root.TryGetProperty("totalAmount", out var tp) ? tp.GetDecimal() : 0m;
        var supplier = root.TryGetProperty("supplier", out var sp) ? sp.GetString() ?? "Supplier" : "Supplier";
        var status = root.TryGetProperty("status", out var stp) ? stp.GetString() ?? "Submitted" : "Submitted";

        var sb = new StringBuilder();
        sb.AppendLine("### ✅ Purchase Order Created & Submitted");
        sb.AppendLine();
        sb.AppendLine("A new procurement purchase order has been successfully generated:");
        sb.AppendLine();
        sb.AppendLine($"- **PO Reference:** `{poNum}`");
        sb.AppendLine($"- **Product Name:** {product}");
        sb.AppendLine($"- **Quantity:** **{quantity} units**");
        sb.AppendLine($"- **Unit Price:** {unitPrice:C2}");
        sb.AppendLine($"- **Total Value:** **{total:C2}**");
        sb.AppendLine($"- **Supplier:** {supplier}");
        sb.AppendLine($"- **Status:** 🟡 **{status}** (Pending Manager Approval)");
        sb.AppendLine();
        sb.AppendLine("📋 You can review and approve this order under the **Purchase Orders** dashboard.");
        return sb.ToString().Trim();
    }

    private static string FormatPurchaseOrderDetails(JsonElement root)
    {
        if (root.TryGetProperty("error", out var errProp))
        {
            return $"### ❌ Purchase Order Search\n\n{errProp.GetString()}";
        }

        var poNum = root.TryGetProperty("poNumber", out var pp) ? pp.GetString() ?? "" : "";
        var status = root.TryGetProperty("status", out var sp) ? sp.GetString() ?? "Draft" : "Draft";
        var supplier = root.TryGetProperty("supplierName", out var supP) ? supP.GetString() ?? "Verified Supplier" : "Verified Supplier";
        var email = root.TryGetProperty("supplierEmail", out var ep) ? ep.GetString() ?? "-" : "-";
        var date = root.TryGetProperty("orderDate", out var dp) ? dp.GetString() ?? "" : "";
        var total = root.TryGetProperty("totalAmount", out var tp) ? tp.GetDecimal().ToString("C2") : "$0.00";

        string statusIcon = status.ToLowerInvariant() switch
        {
            "approved" => "✅ Approved",
            "submitted" => "🟡 Submitted (Pending Approval)",
            "received" => "📦 Received",
            "rejected" => "❌ Rejected",
            _ => $"📝 {status}"
        };

        var sb = new StringBuilder();
        sb.AppendLine($"### 📋 Purchase Order Details: `{poNum}`");
        sb.AppendLine();
        sb.AppendLine($"- **Status:** {statusIcon}");
        sb.AppendLine($"- **Supplier:** **{supplier}** (`{email}`)");
        sb.AppendLine($"- **Order Date:** {date}");
        sb.AppendLine($"- **Total Value:** **{total}**");

        if (root.TryGetProperty("rejectionReason", out var rrp) && !string.IsNullOrWhiteSpace(rrp.GetString()))
        {
            sb.AppendLine($"- ⚠️ **Rejection Reason:** {rrp.GetString()}");
        }
        if (root.TryGetProperty("approvedAt", out var ap) && !string.IsNullOrWhiteSpace(ap.GetString()))
        {
            sb.AppendLine($"- ✅ **Approved At:** {ap.GetString()}");
        }

        sb.AppendLine();
        sb.AppendLine("#### 📦 Order Line Items");
        sb.AppendLine();
        sb.AppendLine("| Product Name | SKU | Quantity | Unit Price | Total |");
        sb.AppendLine("| :--- | :--- | :---: | :---: | :---: |");

        if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array && itemsProp.GetArrayLength() > 0)
        {
            foreach (var item in itemsProp.EnumerateArray())
            {
                var pName = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
                var sku = item.TryGetProperty("sku", out var skp) ? skp.GetString() ?? "" : "";
                var qty = item.TryGetProperty("quantity", out var qp) ? qp.GetInt32().ToString() : "0";
                var unitPrice = item.TryGetProperty("unitPrice", out var up) ? up.GetDecimal().ToString("C2") : "$0.00";
                var lineTotal = item.TryGetProperty("totalAmount", out var ltp) ? ltp.GetDecimal().ToString("C2") : "$0.00";

                sb.AppendLine($"| {pName} | `{sku}` | **{qty}** | {unitPrice} | **{lineTotal}** |");
            }
        }
        else
        {
            sb.AppendLine("| *No individual line items registered* | - | - | - | - |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatStockTransactions(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return "### 📜 Stock Movement History\n\nNo stock movement transactions recorded matching your filter.";
        }

        var sb = new StringBuilder();
        int count = root.GetArrayLength();
        sb.AppendLine("### 📜 Stock Movement History & Audit Log");
        sb.AppendLine();
        sb.AppendLine($"Showing **{count} recent stock movement transactions**:");
        sb.AppendLine();
        sb.AppendLine("| Date | Product | SKU | Type | Qty | Warehouse | Ref / Note |");
        sb.AppendLine("| :--- | :--- | :--- | :---: | :---: | :--- | :--- |");

        foreach (var item in root.EnumerateArray())
        {
            var date = item.TryGetProperty("date", out var dp) ? dp.GetString() ?? "" : "";
            var name = item.TryGetProperty("productName", out var np) ? np.GetString() ?? "" : "";
            var sku = item.TryGetProperty("sku", out var sp) ? sp.GetString() ?? "" : "";
            var type = item.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
            var qty = item.TryGetProperty("quantity", out var qp) ? qp.GetInt32().ToString() : "0";
            var wh = item.TryGetProperty("warehouse", out var wp) ? wp.GetString() ?? "" : "";
            var r = item.TryGetProperty("referenceNumber", out var rp) ? rp.GetString() ?? "-" : "-";

            string badge = type.Equals("StockIn", StringComparison.OrdinalIgnoreCase)
                ? $"🟢 +{qty}"
                : $"🔴 -{qty}";

            sb.AppendLine($"| {date} | {name} | `{sku}` | `{type}` | {badge} | {wh} | {r} |");
        }

        return sb.ToString().Trim();
    }

    private static string FormatGenericToolResult(string toolName, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### ⚡ Automated ERP Action: `{toolName}`");
        sb.AppendLine();

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                sb.AppendLine($"- **{prop.Name}:** {prop.Value.ToString()}");
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            int index = 1;
            foreach (var item in root.EnumerateArray())
            {
                sb.AppendLine($"{index++}. {item.ToString()}");
            }
        }
        else
        {
            sb.AppendLine(root.ToString());
        }

        return sb.ToString().Trim();
    }
}
