using InventoryService.DTOs.Inventory;
using InventoryService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<InventoryResponse>>> GetAll()
    {
        var inventory = await _inventoryService.GetAllAsync();
        return Ok(inventory);
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<InventoryResponse>>> GetLowStock()
    {
        var inventory = await _inventoryService.GetLowStockAsync();
        return Ok(inventory);
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<StockTransactionResponse>>> GetTransactions()
    {
        var transactions = await _inventoryService.GetTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("transactions/product/{productId:int}")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<StockTransactionResponse>>> GetTransactionsByProduct(int productId)
    {
        try
        {
            var transactions = await _inventoryService.GetTransactionsByProductIdAsync(productId);
            return Ok(transactions);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("stock-in")]
    [Authorize(Roles = "Procurement,Admin,Manager")]
    public async Task<ActionResult<InventoryResponse>> StockIn([FromBody] StockInRequest request)
    {
        try
        {
            var result = await _inventoryService.StockInAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("stock-out")]
    [Authorize(Roles = "Procurement,Admin,Manager")]
    public async Task<ActionResult<InventoryResponse>> StockOut([FromBody] StockOutRequest request)
    {
        try
        {
            var result = await _inventoryService.StockOutAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
