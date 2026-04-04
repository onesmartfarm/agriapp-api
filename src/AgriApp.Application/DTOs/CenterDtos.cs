namespace AgriApp.Application.DTOs;

public class CenterResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? AdminUserId { get; set; }
}
