using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/inquiries")]
[Authorize]
public class InquiriesController : ControllerBase
{
    private readonly InquiryService _service;
    private readonly ICurrentUser _currentUser;

    public InquiriesController(InquiryService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inquiries = await _service.GetAllAsync();
        return Ok(inquiries);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inquiry = await _service.GetByIdAsync(id);
        return inquiry == null ? NotFound(new { error = "Inquiry not found" }) : Ok(inquiry);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager,Sales")]
    public async Task<IActionResult> Create([FromBody] CreateInquiryRequest request)
    {
        try
        {
            int centerId;
            if (_currentUser.Role == Core.Enums.Role.SuperUser)
                centerId = request.CenterId ?? _currentUser.CenterId
                    ?? throw new InvalidOperationException("CenterId is required for SuperUser when not assigned to a center");
            else
                centerId = _currentUser.CenterId
                    ?? throw new InvalidOperationException("User must belong to a center");

            var inquiry = await _service.CreateAsync(request, centerId);

            var dto = await _service.GetByIdAsync(inquiry.Id)
                ?? throw new InvalidOperationException("Failed to load inquiry after create.");
            return Created($"/api/inquiries/{inquiry.Id}", dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SuperUser,Manager,Sales")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInquiryStatusRequest request)
    {
        if (!Enum.TryParse<InquiryStatus>(request.Status, out var status))
            return BadRequest(new { error = "Invalid status. Must be New, InProgress, Converted, or Closed" });

        var inquiry = await _service.UpdateStatusAsync(id, status);
        if (inquiry == null)
            return NotFound(new { error = "Inquiry not found or access denied" });
        var dto = await _service.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to load inquiry after status update.");
        return Ok(dto);
    }
}
