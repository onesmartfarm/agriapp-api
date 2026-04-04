using AgriApp.Application.DTOs;
using AgriApp.Application.Services;
using AgriApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly VendorService _service;
    private readonly ICurrentUser _currentUser;

    public VendorsController(VendorService service, ICurrentUser currentUser)
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
        var vendor = await _service.GetByIdAsync(id);
        return vendor == null ? NotFound(new { error = "Vendor not found" }) : Ok(vendor);
    }

    [HttpPost]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest request)
    {
        try
        {
            var vendor = await _service.CreateAsync(request);
            return Created($"/api/vendors/{vendor.Id}", vendor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVendorRequest request)
    {
        var vendor = await _service.UpdateAsync(id, request);
        return vendor == null ? NotFound(new { error = "Vendor not found" }) : Ok(vendor);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? Ok(new { message = "Vendor deleted" }) : NotFound(new { error = "Vendor not found" });
    }
}
