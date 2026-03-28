namespace AgriApp.Application.DTOs;

public class PayrollReportRequest
{
    public int? UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class PayrollReportResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalDaysPresent { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal ProratedSalary { get; set; }
    public decimal RealizedCommissions { get; set; }
    public decimal TotalPay { get; set; }
}
