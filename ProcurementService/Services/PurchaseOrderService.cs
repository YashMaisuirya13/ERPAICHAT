using ProcurementService.Clients.Interfaces;
using ProcurementService.DTOs.PurchaseOrders;
using ProcurementService.Models;
using ProcurementService.Repositories.Interfaces;
using ProcurementService.Services.Interfaces;
using Shared.Events.Events;
using Shared.Events.Messaging;
using Shared.Events.Models;

namespace ProcurementService.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly ISupplierClient _supplierClient;
    private readonly IInventoryClient _inventoryClient;
    private readonly IEventBus _eventBus;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        ISupplierClient supplierClient,
        IInventoryClient inventoryClient,
        IEventBus eventBus)
    {
        _repository = repository;
        _supplierClient = supplierClient;
        _inventoryClient = inventoryClient;
        _eventBus = eventBus;
    }

    public async Task<List<PurchaseOrderResponse>> GetAllAsync()
    {
        var orders = await _repository.GetAllAsync();
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<PagedResult<PurchaseOrderResponse>> GetPagedAsync(int page, int pageSize, string? search = null, string? status = null)
    {
        var pagedOrders = await _repository.GetPagedAsync(page, pageSize, search, status);
        return new PagedResult<PurchaseOrderResponse>
        {
            Items = pagedOrders.Items.Select(MapToResponse).ToList(),
            TotalCount = pagedOrders.TotalCount,
            PageNumber = pagedOrders.PageNumber,
            PageSize = pagedOrders.PageSize
        };
    }

    public async Task<PurchaseOrderResponse?> GetByGuidAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        return order == null ? null : MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request)
    {
        // 1. Verify that Supplier exists in SupplierService and is active
        var supplier = await _supplierClient.GetSupplierByGuidAsync(request.SupplierGuid);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{request.SupplierGuid}' was not found in SupplierService.");
        }

        if (!supplier.IsActive)
        {
            throw new InvalidOperationException($"Supplier '{supplier.Name}' ({request.SupplierGuid}) is inactive and cannot receive Purchase Orders.");
        }

        // 2. Verify that every Product exists in InventoryService
        foreach (var item in request.Items)
        {
            var product = await _inventoryClient.GetProductByGuidAsync(item.ProductGuid);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID '{item.ProductGuid}' was not found in InventoryService.");
            }
        }

        var poNumber = await _repository.GenerateNextPONumberAsync();

        var purchaseOrder = new PurchaseOrder
        {
            PONumber = poNumber,
            SupplierGuid = request.SupplierGuid,
            OrderDate = DateTime.UtcNow,
            Status = "Draft" // AI and users start in Draft
        };

        foreach (var item in request.Items)
        {
            var total = item.Quantity * item.UnitPrice;
            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                ProductGuid = item.ProductGuid,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = total
            });
        }

        purchaseOrder.TotalAmount = purchaseOrder.Items.Sum(x => x.TotalAmount);

        var created = await _repository.CreateAsync(purchaseOrder);

        // 3. Publish POCreatedEvent to RabbitMQ
        await _eventBus.PublishAsync(new POCreatedEvent
        {
            PurchaseOrderGuid = created.PurchaseOrderGuid,
            PONumber = created.PONumber,
            SupplierGuid = created.SupplierGuid,
            TotalAmount = created.TotalAmount,
            Status = created.Status,
            Items = created.Items.Select(x => new POItemDto
            {
                ProductGuid = x.ProductGuid,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                TotalAmount = x.TotalAmount
            }).ToList()
        });

        return MapToResponse(created);
    }

    public async Task<PurchaseOrderResponse?> UpdateAsync(Guid guid, UpdatePurchaseOrderRequest request)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "Draft")
        {
            throw new InvalidOperationException($"Cannot modify Purchase Order in '{order.Status}' status. Only 'Draft' orders can be edited.");
        }

        // Validate Supplier
        var supplier = await _supplierClient.GetSupplierByGuidAsync(request.SupplierGuid);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{request.SupplierGuid}' was not found in SupplierService.");
        }

        if (!supplier.IsActive)
        {
            throw new InvalidOperationException($"Supplier '{supplier.Name}' ({request.SupplierGuid}) is inactive.");
        }

        // Validate Products
        foreach (var item in request.Items)
        {
            var product = await _inventoryClient.GetProductByGuidAsync(item.ProductGuid);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID '{item.ProductGuid}' was not found in InventoryService.");
            }
        }

        order.SupplierGuid = request.SupplierGuid;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            order.Status = request.Status;
        }

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            var total = item.Quantity * item.UnitPrice;
            order.Items.Add(new PurchaseOrderItem
            {
                ProductGuid = item.ProductGuid,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = total
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.TotalAmount);

        await _repository.UpdateAsync(order);
        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> SubmitForApprovalAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "Draft" && order.Status != "Rejected")
        {
            throw new InvalidOperationException($"Cannot submit Purchase Order in '{order.Status}' status. Only 'Draft' or 'Rejected' orders can be submitted for approval.");
        }

        if (order.Items == null || !order.Items.Any())
        {
            throw new InvalidOperationException("Cannot submit a Purchase Order with no line items.");
        }

        order.Status = "PendingApproval";
        order.RejectionReason = null;
        await _repository.UpdateAsync(order);
        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> ApproveAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "PendingApproval" && order.Status != "Submitted")
        {
            throw new InvalidOperationException($"Cannot approve Purchase Order in '{order.Status}' status. Only 'PendingApproval' or 'Submitted' orders can be approved.");
        }

        order.Status = "Approved";
        order.ApprovedAt = DateTime.UtcNow;
        order.RejectionReason = null;
        await _repository.UpdateAsync(order);

        // Publish POApprovedEvent to RabbitMQ
        await _eventBus.PublishAsync(new POApprovedEvent
        {
            PurchaseOrderGuid = order.PurchaseOrderGuid,
            PONumber = order.PONumber,
            SupplierGuid = order.SupplierGuid,
            TotalAmount = order.TotalAmount,
            ApprovedAt = order.ApprovedAt.Value
        });

        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> RejectAsync(Guid guid, RejectPurchaseOrderRequest? request)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "PendingApproval" && order.Status != "Submitted")
        {
            throw new InvalidOperationException($"Cannot reject Purchase Order in '{order.Status}' status. Only 'PendingApproval' or 'Submitted' orders can be rejected.");
        }

        order.Status = "Rejected";
        order.RejectionReason = request?.Reason ?? "Rejected by manager";
        await _repository.UpdateAsync(order);

        // Publish PORejectedEvent to RabbitMQ
        await _eventBus.PublishAsync(new PORejectedEvent
        {
            PurchaseOrderGuid = order.PurchaseOrderGuid,
            PONumber = order.PONumber,
            RejectionReason = order.RejectionReason
        });

        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> MarkAsSentAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "Approved")
        {
            throw new InvalidOperationException($"Cannot mark Purchase Order as sent in '{order.Status}' status. Order must be 'Approved' first.");
        }

        order.Status = "Sent";
        order.SentAt = DateTime.UtcNow;
        await _repository.UpdateAsync(order);
        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> MarkAsReceivedAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status != "Sent" && order.Status != "Approved")
        {
            throw new InvalidOperationException($"Cannot mark Purchase Order as received in '{order.Status}' status. Order must be 'Approved' or 'Sent' first.");
        }

        order.Status = "Received";
        order.ReceivedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(order);

        // Directly restock inventory in InventoryService and publish StockReceivedEvent
        foreach (var item in order.Items)
        {
            try
            {
                var product = await _inventoryClient.GetProductByGuidAsync(item.ProductGuid);
                if (product != null && product.Id > 0)
                {
                    await _inventoryClient.StockInAsync(product.Id, item.Quantity, "WH-CENTRAL", order.PONumber);
                }
            }
            catch
            {
                // Non-blocking fallback; event bus still notifies
            }

            await _eventBus.PublishAsync(new StockReceivedEvent
            {
                ProductGuid = item.ProductGuid,
                Quantity = item.Quantity,
                ReferenceNumber = order.PONumber,
                PurchaseOrderGuid = order.PurchaseOrderGuid
            });
        }

        return MapToResponse(order);
    }

    public async Task<PurchaseOrderResponse?> CancelAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return null;
        }

        if (order.Status == "Received")
        {
            throw new InvalidOperationException("Cannot cancel a Purchase Order that has already been 'Received'.");
        }

        order.Status = "Cancelled";
        await _repository.UpdateAsync(order);
        return MapToResponse(order);
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        var order = await _repository.GetByGuidAsync(guid);
        if (order == null)
        {
            return false;
        }

        if (order.Status != "Draft")
        {
            throw new InvalidOperationException($"Cannot delete Purchase Order in '{order.Status}' status. Only 'Draft' orders can be deleted.");
        }

        await _repository.DeleteAsync(order);
        return true;
    }

    private static PurchaseOrderResponse MapToResponse(PurchaseOrder order)
    {
        return new PurchaseOrderResponse
        {
            Id = order.Id,
            PurchaseOrderGuid = order.PurchaseOrderGuid,
            PONumber = order.PONumber,
            SupplierGuid = order.SupplierGuid,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            RejectionReason = order.RejectionReason,
            ApprovedAt = order.ApprovedAt,
            SentAt = order.SentAt,
            ReceivedAt = order.ReceivedAt,
            Items = order.Items.Select(item => new PurchaseOrderItemResponse
            {
                Id = item.Id,
                PurchaseOrderItemGuid = item.PurchaseOrderItemGuid,
                ProductGuid = item.ProductGuid,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.TotalAmount
            }).ToList()
        };
    }
}
