using System.Text.Json.Serialization;

namespace AgriApp.Web.Services;

public sealed class EquipmentTimelineEnvelopeDto
{
    [JsonPropertyName("rangeStart")]
    public DateTime RangeStart { get; set; }

    [JsonPropertyName("rangeEnd")]
    public DateTime RangeEnd { get; set; }

    [JsonPropertyName("equipment")]
    public List<EquipmentTimelineRowDto> Equipment { get; set; } = new();
}

public sealed class EquipmentTimelineRowDto
{
    [JsonPropertyName("equipmentId")]
    public int EquipmentId { get; set; }

    [JsonPropertyName("equipmentName")]
    public string EquipmentName { get; set; } = string.Empty;

    [JsonPropertyName("isImplement")]
    public bool IsImplement { get; set; }

    [JsonPropertyName("blocks")]
    public List<TimelineBlockDto> Blocks { get; set; } = new();
}

public sealed class TimelineBlockDto
{
    [JsonPropertyName("blockType")]
    public string BlockType { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("workOrderId")]
    public int? WorkOrderId { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("serviceActivityName")]
    public string? ServiceActivityName { get; set; }

    [JsonPropertyName("pairingKey")]
    public string? PairingKey { get; set; }
}
