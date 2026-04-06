using System.Text.Json.Serialization;

namespace AgriApp.Web.Services;

/// <summary>JSON shape returned by GET /api/calendar/capacity.</summary>
public sealed class CenterCapacityApiResponse
{
    [JsonPropertyName("rangeStart")]
    public DateTime RangeStart { get; set; }

    [JsonPropertyName("rangeEnd")]
    public DateTime RangeEnd { get; set; }

    [JsonPropertyName("totalScheduled")]
    public int TotalScheduled { get; set; }

    [JsonPropertyName("workOrders")]
    public List<CapacitySlotApiDto>? WorkOrders { get; set; }
}

public sealed class CapacitySlotApiDto
{
    [JsonPropertyName("workOrderId")]
    public int WorkOrderId { get; set; }

    [JsonPropertyName("centerId")]
    public int CenterId { get; set; }

    [JsonPropertyName("implementId")]
    public int? ImplementId { get; set; }

    [JsonPropertyName("tractorId")]
    public int? TractorId { get; set; }

    [JsonPropertyName("responsibleUserId")]
    public int ResponsibleUserId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("scheduledStartDate")]
    public DateTime ScheduledStartDate { get; set; }

    [JsonPropertyName("scheduledEndDate")]
    public DateTime ScheduledEndDate { get; set; }
}
