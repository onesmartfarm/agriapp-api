namespace AgriApp.Application.Services;

public class GstBreakdown
{
    public decimal BaseAmount { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal TotalGst { get; set; }
    public decimal GrandTotal { get; set; }
}

public static class GstCalculator
{
    private const decimal CgstRate = 0.09m;
    private const decimal SgstRate = 0.09m;

    public static GstBreakdown Calculate(decimal baseAmount)
    {
        var cgst = Math.Round(baseAmount * CgstRate, 2, MidpointRounding.AwayFromZero);
        var sgst = Math.Round(baseAmount * SgstRate, 2, MidpointRounding.AwayFromZero);
        var totalGst = cgst + sgst;
        var grandTotal = baseAmount + totalGst;

        return new GstBreakdown
        {
            BaseAmount = baseAmount,
            Cgst = cgst,
            Sgst = sgst,
            TotalGst = totalGst,
            GrandTotal = grandTotal
        };
    }

    public static GstBreakdown CalculateRental(decimal hourlyRate, decimal hours)
    {
        var baseAmount = Math.Round(hourlyRate * hours, 2, MidpointRounding.AwayFromZero);
        return Calculate(baseAmount);
    }
}
