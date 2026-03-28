using AgriApp.Application.DTOs;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Application.Services;

public class PayrollService
{
    private readonly AgriDbContext _db;

    public PayrollService(AgriDbContext db)
    {
        _db = db;
    }

    public async Task<int?> GetUserCenterId(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.CenterId;
    }

    public async Task<PayrollReportResponse?> CalculateMonthlyPay(int userId, int month, int year)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var attendanceRecords = await _db.Attendances
            .Where(a => a.UserId == userId
                && a.Timestamp >= startDate
                && a.Timestamp < endDate
                && a.Type == AttendanceType.ClockIn)
            .ToListAsync();

        int totalDaysPresent = attendanceRecords
            .Select(a => a.Timestamp.Date)
            .Distinct()
            .Count();

        var salary = await _db.SalaryStructures
            .FirstOrDefaultAsync(s => s.UserId == userId);

        decimal baseSalary = salary?.BaseSalary ?? 0m;
        decimal proratedSalary = Math.Round(baseSalary * ((decimal)totalDaysPresent / 30m), 2, MidpointRounding.AwayFromZero);

        decimal realizedCommissions = await _db.CommissionLedgers
            .Where(c => c.UserId == userId
                && c.Status == CommissionStatus.Realized
                && c.UpdatedAt >= startDate
                && c.UpdatedAt < endDate)
            .SumAsync(c => c.Amount);

        decimal totalPay = proratedSalary + realizedCommissions;

        return new PayrollReportResponse
        {
            UserId = userId,
            UserName = user.Name,
            UserEmail = user.Email,
            Month = month,
            Year = year,
            TotalDaysPresent = totalDaysPresent,
            BaseSalary = baseSalary,
            ProratedSalary = proratedSalary,
            RealizedCommissions = realizedCommissions,
            TotalPay = totalPay
        };
    }

    public async Task<List<PayrollReportResponse>> CalculateCenterPayroll(int centerId, int month, int year)
    {
        var users = await _db.Users
            .Where(u => u.CenterId == centerId)
            .ToListAsync();

        var reports = new List<PayrollReportResponse>();
        foreach (var user in users)
        {
            var report = await CalculateMonthlyPay(user.Id, month, year);
            if (report != null)
                reports.Add(report);
        }

        return reports;
    }
}
