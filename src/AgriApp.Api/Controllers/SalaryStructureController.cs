using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/salary-structures")]
[Authorize(Roles = "SuperUser,Manager")]
public class SalaryStructureController : ControllerBase
{
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SalaryStructureController(AgriDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalaryStructureRequest request)
    {
        var existing = await _db.SalaryStructures.FirstOrDefaultAsync(s => s.UserId == request.UserId);
        if (existing != null)
            return Conflict(new { error = "Salary structure already exists for this user. Use PUT to update." });

        int centerId;
        if (_currentUser.Role == Role.SuperUser)
            centerId = request.CenterId ?? _currentUser.CenterId
                ?? throw new InvalidOperationException("CenterId is required");
        else
            centerId = _currentUser.CenterId
                ?? throw new InvalidOperationException("User must belong to a center");

        var salary = new SalaryStructure
        {
            UserId = request.UserId,
            CenterId = centerId,
            BaseSalary = request.BaseSalary,
            CommissionPercentage = request.CommissionPercentage,
            CreatedAt = DateTime.UtcNow
        };

        _db.SalaryStructures.Add(salary);
        await _db.SaveChangesAsync();

        return Created($"/api/salary-structures/{salary.Id}", new SalaryStructureResponse
        {
            Id = salary.Id,
            UserId = salary.UserId,
            CenterId = salary.CenterId,
            BaseSalary = salary.BaseSalary,
            CommissionPercentage = salary.CommissionPercentage
        });
    }

    [HttpPut("{userId:int}")]
    public async Task<IActionResult> Update(int userId, [FromBody] SalaryStructureRequest request)
    {
        var salary = await _db.SalaryStructures.FirstOrDefaultAsync(s => s.UserId == userId);
        if (salary == null)
            return NotFound(new { error = "Salary structure not found for this user." });

        salary.BaseSalary = request.BaseSalary;
        salary.CommissionPercentage = request.CommissionPercentage;
        salary.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new SalaryStructureResponse
        {
            Id = salary.Id,
            UserId = salary.UserId,
            CenterId = salary.CenterId,
            BaseSalary = salary.BaseSalary,
            CommissionPercentage = salary.CommissionPercentage
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var structures = await _db.SalaryStructures
            .Select(s => new SalaryStructureResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                CenterId = s.CenterId,
                BaseSalary = s.BaseSalary,
                CommissionPercentage = s.CommissionPercentage
            })
            .ToListAsync();

        return Ok(structures);
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var salary = await _db.SalaryStructures.FirstOrDefaultAsync(s => s.UserId == userId);
        if (salary == null)
            return NotFound(new { error = "Salary structure not found for this user." });

        return Ok(new SalaryStructureResponse
        {
            Id = salary.Id,
            UserId = salary.UserId,
            CenterId = salary.CenterId,
            BaseSalary = salary.BaseSalary,
            CommissionPercentage = salary.CommissionPercentage
        });
    }
}
