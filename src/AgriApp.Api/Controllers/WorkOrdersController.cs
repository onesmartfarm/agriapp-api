using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/workorders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly WorkOrderService _service;
    private readonly ICurrentUser _currentUser;

    public WorkOrdersController(WorkOrderService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var workOrder = await _service.GetByIdAsync(id);
        return workOrder == null
            ? NotFound(new { error = "Work order not found" })
            : Ok(workOrder);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
    {
        try
        {
            int centerId;
            if (_currentUser.Role == Role.SuperUser)
                centerId = request.CenterId ?? _currentUser.CenterId
                    ?? throw new InvalidOperationException("CenterId is required for SuperUser when not assigned to a center.");
            else
                centerId = _currentUser.CenterId
                    ?? throw new InvalidOperationException("User must belong to a center.");

            var workOrder = await _service.CreateWorkOrderAsync(request, centerId);
            return Created($"/api/workorders/{workOrder.Id}", workOrder);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SuperUser,Manager,Staff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWorkOrderStatusRequest request)
    {
        if (!Enum.TryParse<WorkStatus>(request.Status, out var status))
            return BadRequest(new { error = "Invalid status. Must be: Scheduled, InProgress, Completed, or Cancelled." });

        var workOrder = await _service.UpdateStatusAsync(id, status);
        return workOrder == null
            ? NotFound(new { error = "Work order not found or access denied." })
            : Ok(workOrder);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted
            ? Ok(new { message = "Work order deleted" })
            : NotFound(new { error = "Work order not found" });
    }
}
