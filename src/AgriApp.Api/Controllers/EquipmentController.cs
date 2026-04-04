using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/equipment")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly EquipmentService _service;
    private readonly ICurrentUser _currentUser;

    public EquipmentController(EquipmentService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var equipment = await _service.GetAllAsync();
        return Ok(equipment);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var equipment = await _service.GetByIdAsync(id);
        return equipment == null ? NotFound(new { error = "Equipment not found" }) : Ok(equipment);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateEquipmentRequest request)
    {
        if (!Enum.TryParse<EquipmentCategory>(request.Category, out var category))
            return BadRequest(new { error = "Invalid category. Must be Tractor, Drone, BioCNG, or Vehicle" });

        int centerId;
        if (_currentUser.Role == Role.SuperUser)
            centerId = request.CenterId;
        else
            centerId = _currentUser.CenterId
                ?? throw new InvalidOperationException("User must belong to a center");

        var equipment = await _service.CreateAsync(
            request.Name, category, request.HourlyRate, centerId,
            request.VendorId, request.PurchaseCost, request.PurchaseDate);
        return Created($"/api/equipment/{equipment.Id}", equipment);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEquipmentRequest request)
    {
        EquipmentCategory? category = null;
        if (request.Category != null)
        {
            if (!Enum.TryParse<EquipmentCategory>(request.Category, out var parsed))
                return BadRequest(new { error = "Invalid category" });
            category = parsed;
        }

        var equipment = await _service.UpdateAsync(
            id, request.Name, category, request.HourlyRate,
            request.VendorId, request.PurchaseCost, request.PurchaseDate);
        return equipment == null ? NotFound(new { error = "Equipment not found" }) : Ok(equipment);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok(new { message = "Equipment deleted" }) : NotFound(new { error = "Equipment not found" });
    }

    [HttpPost("{id:int}/quote")]
    public async Task<IActionResult> GetQuote(int id, [FromBody] RentalQuoteRequest request)
    {
        var equipment = await _service.GetByIdAsync(id);
        if (equipment == null) return NotFound(new { error = "Equipment not found" });

        var (gst, commission) = _service.GetQuote(equipment.HourlyRate, request.Hours);

        return Ok(new
        {
            equipment = new { equipment.Id, equipment.Name, Category = equipment.Category.ToString() },
            hours = request.Hours,
            pricing = gst,
            commission
        });
    }
}
