namespace AgriApp.Core.Enums;

public enum WorkTimeLogType
{
    /// <summary>
    /// Billable working time. Only "Working" spans are counted in invoice calculations.
    /// </summary>
    Working = 0,

    /// <summary>
    /// Non-billable break time (lunch, rest, etc.).
    /// Excluded from invoice calculations.
    /// </summary>
    Break = 1,

    /// <summary>
    /// Non-billable equipment breakdown time.
    /// Excluded from invoice calculations.
    /// </summary>
    Breakdown = 2
}
