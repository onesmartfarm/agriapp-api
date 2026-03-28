using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize(Roles = "SuperUser,Manager")]
public class PayrollController : ControllerBase
{
    private readonly PayrollService _payrollService;
    private readonly ICurrentUser _currentUser;

    public PayrollController(PayrollService payrollService, ICurrentUser currentUser)
    {
        _payrollService = payrollService;
        _currentUser = currentUser;
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] int month, [FromQuery] int year, [FromQuery] int? userId)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { error = "Month must be between 1 and 12." });

        if (year < 2000 || year > 2100)
            return BadRequest(new { error = "Year must be between 2000 and 2100." });

        if (userId.HasValue)
        {
            if (_currentUser.Role != Role.SuperUser)
            {
                var targetUser = await _payrollService.GetUserCenterId(userId.Value);
                if (targetUser == null)
                    return NotFound(new { error = "User not found." });
                if (targetUser != _currentUser.CenterId)
                    return Forbid();
            }

            var report = await _payrollService.CalculateMonthlyPay(userId.Value, month, year);
            if (report == null)
                return NotFound(new { error = "User not found." });
            return Ok(report);
        }

        var centerId = _currentUser.CenterId;
        if (_currentUser.Role != Role.SuperUser && !centerId.HasValue)
            return BadRequest(new { error = "User must belong to a center." });

        if (_currentUser.Role == Role.SuperUser && !centerId.HasValue)
            return BadRequest(new { error = "SuperUser must specify a userId or be assigned to a center." });

        var reports = await _payrollService.CalculateCenterPayroll(centerId!.Value, month, year);
        return Ok(reports);
    }
}
