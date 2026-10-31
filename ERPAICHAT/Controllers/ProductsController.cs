using InventoryService.DTOs.Inventory;
using InventoryService.DTOs.Products;
using InventoryService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;

    public ProductsController(
        IProductService productService,
        IInventoryService inventoryService)
    {
        _productService = productService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null)
    {
        if (pageSize > 0)
        {
            var paged = await _productService.GetPagedAsync(page, pageSize, search);
            return Ok(paged);
        }

        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        var product = await _productService.GetByGuidAsync(id);

        if (product == null)
        {
            return NotFound(new { message = $"Product with ID '{id}' was not found." });
        }

        return Ok(product);
    }

    [HttpGet("{productId:int}/inventory")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<InventoryResponse>>> GetInventory(int productId)
    {
        try
        {
            var inventory = await _inventoryService.GetByProductIdAsync(productId);
            return Ok(inventory);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var createdProduct = await _productService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdProduct.ProductGuid },
            createdProduct);
    }
}
