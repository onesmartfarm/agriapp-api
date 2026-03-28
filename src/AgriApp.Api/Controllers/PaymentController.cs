using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize(Roles = "SuperUser,Manager")]
public class PaymentController : ControllerBase
{
    private readonly CommissionRealizationService _commissionService;

    public PaymentController(CommissionRealizationService commissionService)
    {
        _commissionService = commissionService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UpiTransactionId))
            return BadRequest(new { error = "UpiTransactionId is required." });

        var (count, totalAmount) = await _commissionService.ConfirmPaymentAsync(
            request.UpiTransactionId, request.InquiryId);

        if (count == 0)
            return NotFound(new { error = "No pending commissions found for this inquiry." });

        return Ok(new PaymentWebhookResponse
        {
            UpiTransactionId = request.UpiTransactionId,
            InquiryId = request.InquiryId,
            CommissionsRealized = count,
            TotalAmountRealized = totalAmount
        });
    }
}
