using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _service;
    private readonly ICurrentUser _currentUser;

    public InvoicesController(InvoiceService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var invoice = await _service.GetByIdAsync(id);
        return invoice == null
            ? NotFound(new { error = "Invoice not found or access denied." })
            : Ok(invoice);
    }

    /// <summary>
    /// Generate a Draft invoice from a Completed WorkOrder.
    /// TotalAmount = (TotalMaterialCost + AdditionalFees) + 18% GST.
    /// Only one invoice may be generated per WorkOrder.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request)
    {
        try
        {
            int centerId;
            if (_currentUser.Role == Role.SuperUser)
                centerId = request.CenterId ?? _currentUser.CenterId
                    ?? throw new InvalidOperationException("CenterId required for SuperUser.");
            else
                centerId = _currentUser.CenterId
                    ?? throw new InvalidOperationException("User must belong to a center.");

            var invoice = await _service.GenerateInvoiceFromWorkOrderAsync(request, centerId);
            return Created($"/api/invoices/{invoice.Id}", invoice);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>
    /// Transition a Draft invoice to Issued — makes it payable.
    /// </summary>
    [HttpPatch("{id:guid}/issue")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Issue(Guid id)
    {
        try
        {
            var invoice = await _service.IssueAsync(id);
            return invoice == null
                ? NotFound(new { error = "Invoice not found or access denied." })
                : Ok(invoice);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}
