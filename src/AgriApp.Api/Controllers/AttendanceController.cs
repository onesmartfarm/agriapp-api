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
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AgriDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AttendanceController(AgriDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("clock")]
    public async Task<IActionResult> Clock([FromBody] ClockRequest request)
    {
        if (request.Latitude == 0 && request.Longitude == 0)
            return BadRequest(new { error = "GPS coordinates are required. (0,0) is not a valid location." });

        if (!Enum.TryParse<AttendanceType>(request.Type, true, out var attendanceType))
            return BadRequest(new { error = "Invalid type. Must be ClockIn or ClockOut." });

        var centerId = _currentUser.CenterId
            ?? (_currentUser.Role == Role.SuperUser
                ? throw new InvalidOperationException("SuperUser must be assigned to a center to clock attendance")
                : throw new InvalidOperationException("User must belong to a center"));

        var attendance = new Attendance
        {
            UserId = _currentUser.UserId,
            CenterId = centerId,
            Timestamp = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Type = attendanceType
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();

        return Created($"/api/attendance/{attendance.Id}", new AttendanceResponse
        {
            Id = attendance.Id,
            UserId = attendance.UserId,
            CenterId = attendance.CenterId,
            Timestamp = attendance.Timestamp,
            Latitude = attendance.Latitude,
            Longitude = attendance.Longitude,
            Type = attendance.Type.ToString()
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAttendance([FromQuery] int? month, [FromQuery] int? year)
    {
        var query = _db.Attendances.Where(a => a.UserId == _currentUser.UserId);

        if (month.HasValue && year.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            query = query.Where(a => a.Timestamp >= start && a.Timestamp < end);
        }

        var records = await query
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AttendanceResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                CenterId = a.CenterId,
                Timestamp = a.Timestamp,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Type = a.Type.ToString()
            })
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<IActionResult> GetAll([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? userId)
    {
        var query = _db.Attendances.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (month.HasValue && year.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            query = query.Where(a => a.Timestamp >= start && a.Timestamp < end);
        }

        var records = await query
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AttendanceResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                CenterId = a.CenterId,
                Timestamp = a.Timestamp,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Type = a.Type.ToString()
            })
            .ToListAsync();

        return Ok(records);
    }
}
