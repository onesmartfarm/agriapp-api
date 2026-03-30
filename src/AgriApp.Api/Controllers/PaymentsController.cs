using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _service;
    private readonly ICurrentUser _currentUser;

    public PaymentsController(PaymentService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Record a payment against an Issued or PartiallyPaid invoice.
    /// Duplicate TransactionReference values are rejected (prevents double UPI payments).
    /// When AmountPaid >= TotalAmount, the invoice transitions to Paid automatically.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
    {
        try
        {
            int centerId;
            if (_currentUser.Role == Role.SuperUser)
                centerId = _currentUser.CenterId
                    ?? throw new InvalidOperationException("SuperUser must be assigned to a center to record payments.");
            else
                centerId = _currentUser.CenterId
                    ?? throw new InvalidOperationException("User must belong to a center.");

            var result = await _service.RecordPaymentAsync(request, centerId);
            return Created($"/api/payments/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
    }
}
