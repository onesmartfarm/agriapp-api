using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/service-activities")]
[Authorize]
public class ServiceActivitiesController : ControllerBase
{
    private readonly ServiceActivityService _service;

    public ServiceActivitiesController(ServiceActivityService service)
    {
        _service = service;
    }

    /// <param name="centerId">When SuperUser, optional filter by center.</param>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? centerId)
        => Ok(await _service.GetAllAsync(centerId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var row = await _service.GetByIdAsync(id);
        return row == null ? NotFound(new { error = "Service activity not found" }) : Ok(row);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateServiceActivityRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);
            return Created($"/api/service-activities/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceActivityRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        return updated == null ? NotFound(new { error = "Service activity not found" }) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok(new { message = "Service activity deleted" }) : NotFound(new { error = "Service activity not found" });
    }
}
