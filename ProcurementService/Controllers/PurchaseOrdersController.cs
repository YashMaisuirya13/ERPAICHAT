using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementService.DTOs.PurchaseOrders;
using ProcurementService.Exceptions;
using ProcurementService.Services.Interfaces;

namespace ProcurementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        if (pageSize.HasValue && pageSize.Value > 0)
        {
            var paged = await _service.GetPagedAsync(page, pageSize.Value, search, status);
            return Ok(paged);
        }

        var orders = await _service.GetAllAsync();
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Procurement,Manager")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetById(Guid id)
    {
        var order = await _service.GetByGuidAsync(id);
        if (order == null)
        {
            return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.PurchaseOrderGuid },
                created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                service = ex.ServiceName,
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> Update(Guid id, [FromBody] UpdatePurchaseOrderRequest request)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                service = ex.ServiceName,
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> SubmitForApproval(Guid id)
    {
        try
        {
            var submitted = await _service.SubmitForApprovalAsync(id);
            if (submitted == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(submitted);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> Approve(Guid id)
    {
        try
        {
            var approved = await _service.ApproveAsync(id);
            if (approved == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(approved);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> Reject(Guid id, [FromBody] RejectPurchaseOrderRequest? request)
    {
        try
        {
            var rejected = await _service.RejectAsync(id, request);
            if (rejected == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(rejected);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> MarkAsSent(Guid id)
    {
        try
        {
            var sent = await _service.MarkAsSentAsync(id);
            if (sent == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(sent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = "Procurement,Admin,Manager")]
    public async Task<ActionResult<PurchaseOrderResponse>> MarkAsReceived(Guid id)
    {
        try
        {
            var received = await _service.MarkAsReceivedAsync(id);
            if (received == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(received);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Procurement,Admin,Manager")]
    public async Task<ActionResult<PurchaseOrderResponse>> Cancel(Guid id)
    {
        try
        {
            var cancelled = await _service.CancelAsync(id);
            if (cancelled == null)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return Ok(cancelled);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Purchase Order with ID '{id}' was not found." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
