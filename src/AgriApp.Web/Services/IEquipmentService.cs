namespace AgriApp.Web.Services;

public interface IEquipmentService
{
    Task<List<EquipmentResponse>> GetAllAsync();
    Task<EquipmentResponse?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, EquipmentResponse? Equipment)> CreateAsync(CreateEquipmentRequest request);
    Task<(bool Success, string? Error, EquipmentResponse? Equipment)> UpdateAsync(int id, UpdateEquipmentRequest request);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? Error, EquipmentQuoteResult? Quote)> GetQuoteAsync(int id, decimal hours);
}
