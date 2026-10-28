using InventoryService.DTOs.Inventory;
using InventoryService.Models;
using InventoryService.Repositories.Interfaces;
using InventoryService.Services.Interfaces;
using Shared.Events.Events;
using Shared.Events.Messaging;

namespace InventoryService.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEventBus _eventBus;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IEventBus eventBus)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _eventBus = eventBus;
    }

    public async Task<List<InventoryResponse>> GetAllAsync()
    {
        var inventoryItems = await _inventoryRepository.GetAllAsync();
        return inventoryItems.Select(MapToResponse).ToList();
    }

    public async Task<List<InventoryResponse>> GetByProductIdAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {productId} not found.");
        }

        var inventoryItems = await _inventoryRepository.GetByProductIdAsync(productId);
        return inventoryItems.Select(MapToResponse).ToList();
    }

    public async Task<List<InventoryResponse>> GetLowStockAsync()
    {
        var inventoryItems = await _inventoryRepository.GetLowStockAsync();
        return inventoryItems.Select(MapToResponse).ToList();
    }

    public async Task<InventoryResponse> StockInAsync(StockInRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException(
                $"Product with ID {request.ProductId} was not found.");
        }

        var inventoryItem = await _inventoryRepository.GetByProductAndWarehouseAsync(
            request.ProductId,
            request.WarehouseCode);

        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                WarehouseCode = request.WarehouseCode
            };

            inventoryItem = await _inventoryRepository.CreateAsync(inventoryItem);
            inventoryItem.Product = product;
        }
        else
        {
            inventoryItem.Quantity += request.Quantity;
            await _inventoryRepository.UpdateAsync(inventoryItem);
        }

        // 1. Record stock-in transaction
        var transaction = new StockTransaction
        {
            ProductId = request.ProductId,
            InventoryItemId = inventoryItem.Id,
            TransactionType = "STOCK_IN",
            Quantity = request.Quantity,
            WarehouseCode = request.WarehouseCode,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = request.ReferenceNumber
        };
        await _inventoryRepository.AddTransactionAsync(transaction);

        // 2. Publish StockReceivedEvent to RabbitMQ
        await _eventBus.PublishAsync(new StockReceivedEvent
        {
            ProductId = product.Id,
            ProductGuid = product.ProductGuid,
            Quantity = request.Quantity,
            WarehouseCode = request.WarehouseCode,
            ReferenceNumber = request.ReferenceNumber
        });

        return MapToResponse(inventoryItem);
    }

    public async Task<InventoryResponse> StockOutAsync(StockOutRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException(
                $"Product with ID {request.ProductId} was not found.");
        }

        var inventoryItem = await _inventoryRepository.GetByProductAndWarehouseAsync(
            request.ProductId,
            request.WarehouseCode);

        if (inventoryItem == null)
        {
            throw new KeyNotFoundException("Inventory record not found for the specified product and warehouse.");
        }

        if (inventoryItem.Quantity < request.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock available. Current stock: {inventoryItem.Quantity}, Requested: {request.Quantity}.");
        }

        inventoryItem.Quantity -= request.Quantity;
        await _inventoryRepository.UpdateAsync(inventoryItem);

        // 1. Record stock-out transaction
        var transaction = new StockTransaction
        {
            ProductId = request.ProductId,
            InventoryItemId = inventoryItem.Id,
            TransactionType = "STOCK_OUT",
            Quantity = request.Quantity,
            WarehouseCode = request.WarehouseCode,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = request.ReferenceNumber
        };
        await _inventoryRepository.AddTransactionAsync(transaction);

        // 2. Publish StockLowEvent if quantity has dropped to or below minimum threshold
        if (inventoryItem.Quantity <= product.MinimumStock)
        {
            await _eventBus.PublishAsync(new StockLowEvent
            {
                ProductId = product.Id,
                ProductGuid = product.ProductGuid,
                SKU = product.SKU,
                ProductName = product.Name,
                WarehouseCode = request.WarehouseCode,
                CurrentQuantity = inventoryItem.Quantity,
                MinimumStock = product.MinimumStock
            });
        }

        return MapToResponse(inventoryItem);
    }

    public async Task<List<StockTransactionResponse>> GetTransactionsAsync()
    {
        var transactions = await _inventoryRepository.GetTransactionsAsync();
        return transactions.Select(MapToTransactionResponse).ToList();
    }

    public async Task<List<StockTransactionResponse>> GetTransactionsByProductIdAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {productId} was not found.");
        }

        var transactions = await _inventoryRepository.GetTransactionsByProductIdAsync(productId);
        return transactions.Select(MapToTransactionResponse).ToList();
    }

    private static InventoryResponse MapToResponse(InventoryItem inventory)
    {
        return new InventoryResponse
        {
            Id = inventory.Id,
            InventoryGuid = inventory.InventoryGuid,
            ProductId = inventory.ProductId,
            ProductGuid = inventory.Product?.ProductGuid ?? Guid.Empty,
            SKU = inventory.Product?.SKU ?? string.Empty,
            ProductName = inventory.Product?.Name ?? string.Empty,
            Quantity = inventory.Quantity,
            WarehouseCode = inventory.WarehouseCode,
            MinimumStock = inventory.Product?.MinimumStock ?? 0,
            IsLowStock = inventory.Product != null && inventory.Quantity <= inventory.Product.MinimumStock
        };
    }

    private static StockTransactionResponse MapToTransactionResponse(StockTransaction transaction)
    {
        return new StockTransactionResponse
        {
            TransactionGuid = transaction.TransactionGuid,
            ProductId = transaction.ProductId,
            ProductGuid = transaction.Product?.ProductGuid ?? Guid.Empty,
            SKU = transaction.Product?.SKU ?? string.Empty,
            ProductName = transaction.Product?.Name ?? string.Empty,
            InventoryItemId = transaction.InventoryItemId,
            TransactionType = transaction.TransactionType,
            Quantity = transaction.Quantity,
            WarehouseCode = transaction.WarehouseCode,
            TransactionDate = transaction.TransactionDate,
            ReferenceNumber = transaction.ReferenceNumber
        };
    }
}
