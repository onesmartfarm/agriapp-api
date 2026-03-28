namespace AgriApp.Application.Services;

public class CommissionResult
{
    public decimal RentalAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetToCompany { get; set; }
}

public static class CommissionCalculator
{
    private static readonly (decimal MinAmount, decimal Rate)[] Tiers =
    {
        (50000m, 0.10m),
        (20000m, 0.08m),
        (5000m,  0.05m),
        (0m,     0.03m)
    };

    public static CommissionResult Calculate(decimal rentalAmount)
    {
        var rate = Tiers.First(t => rentalAmount >= t.MinAmount).Rate;
        var commission = Math.Round(rentalAmount * rate, 2, MidpointRounding.AwayFromZero);
        var net = rentalAmount - commission;

        return new CommissionResult
        {
            RentalAmount = rentalAmount,
            CommissionRate = rate,
            CommissionAmount = commission,
            NetToCompany = net
        };
    }
}
