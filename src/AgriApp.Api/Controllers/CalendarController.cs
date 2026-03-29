using AgriApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    /// <summary>
    /// Returns all scheduled work orders within the given date range.
    /// Query uses .AsNoTracking() for high performance.
    /// Respects CenterId Global Query Filter — SuperUser sees all centers.
    /// </summary>
    [HttpGet("capacity")]
    public async Task<IActionResult> GetCapacity(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        if (end <= start)
            return BadRequest(new { error = "End date must be after start date." });

        if ((end - start).TotalDays > 366)
            return BadRequest(new { error = "Date range cannot exceed 366 days." });

        var result = await _calendarService.GetCenterCapacityAsync(
            DateTime.SpecifyKind(start, DateTimeKind.Utc),
            DateTime.SpecifyKind(end, DateTimeKind.Utc));

        return Ok(result);
    }
}
