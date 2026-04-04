using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;

namespace AgriApp.Core.Entities;

public class Invoice : ICenterScoped, IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → centers.Id (int, matches existing PK type)</summary>
    public int CenterId { get; set; }

    /// <summary>FK → work_orders.Id (int, matches existing PK type)</summary>
    public int WorkOrderId { get; set; }

    /// <summary>FK → customers.Id — the customer being billed</summary>
    public int CustomerId { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
