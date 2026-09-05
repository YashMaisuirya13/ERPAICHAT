using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierService.DTOs.Suppliers;
using SupplierService.Services.Interfaces;

namespace SupplierService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<List<SupplierResponse>>> GetAll()
    {
        var suppliers = await _supplierService.GetAllAsync();
        return Ok(suppliers);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<SupplierResponse>> GetById(Guid id)
    {
        var supplier = await _supplierService.GetByGuidAsync(id);
        if (supplier == null)
        {
            return NotFound(new { message = $"Supplier with ID '{id}' was not found." });
        }

        return Ok(supplier);
    }

    [HttpGet("code/{code}")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<SupplierResponse>> GetByCode(string code)
    {
        var supplier = await _supplierService.GetByCodeAsync(code);
        if (supplier == null)
        {
            return NotFound(new { message = $"Supplier with code '{code}' was not found." });
        }

        return Ok(supplier);
    }

    [HttpPost]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<SupplierResponse>> Create([FromBody] CreateSupplierRequest request)
    {
        try
        {
            var createdSupplier = await _supplierService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdSupplier.SupplierGuid },
                createdSupplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        var updatedSupplier = await _supplierService.UpdateAsync(id, request);
        if (updatedSupplier == null)
        {
            return NotFound(new { message = $"Supplier with ID '{id}' was not found." });
        }

        return Ok(updatedSupplier);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _supplierService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Supplier with ID '{id}' was not found." });
        }

        return NoContent();
    }
}
