using AgriApp.Application.DTOs;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/centers")]
[Authorize]
public class CentersController : ControllerBase
{
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CentersController(AgriDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _db.Centers.AsNoTracking();

        if (_currentUser.Role != Core.Enums.Role.SuperUser && _currentUser.CenterId.HasValue)
            query = query.Where(c => c.Id == _currentUser.CenterId.Value);

        var centers = await query
            .Select(c => new CenterResponse
            {
                Id = c.Id,
                Name = c.Name,
                Location = c.Location,
                AdminUserId = c.AdminUserId
            })
            .ToListAsync();

        return Ok(centers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var center = await _db.Centers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CenterResponse
            {
                Id = c.Id,
                Name = c.Name,
                Location = c.Location,
                AdminUserId = c.AdminUserId
            })
            .FirstOrDefaultAsync();

        return center == null ? NotFound(new { error = "Center not found" }) : Ok(center);
    }
}
