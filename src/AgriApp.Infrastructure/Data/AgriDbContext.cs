using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgriApp.Infrastructure.Data;

public class AgriDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public AgriDbContext(DbContextOptions<AgriDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Center> Centers => Set<Center>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ServiceActivity> ServiceActivities => Set<ServiceActivity>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<CommissionLedger> CommissionLedgers => Set<CommissionLedger>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WorkOrderTimeLog> WorkOrderTimeLogs => Set<WorkOrderTimeLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCenter(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureVendor(modelBuilder);
        ConfigureEquipment(modelBuilder);
        ConfigureServiceActivity(modelBuilder);
        ConfigureInquiry(modelBuilder);
        ConfigureWorkOrder(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureAttendance(modelBuilder);
        ConfigureSalaryStructure(modelBuilder);
        ConfigureCommissionLedger(modelBuilder);
        ConfigureInvoice(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureWorkOrderTimeLog(modelBuilder);
    }

    private void ConfigureCenter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Center>(entity =>
        {
            entity.ToTable("centers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CurrencySymbol).IsRequired().HasMaxLength(16);
            entity.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.AdminUser)
                  .WithMany()
                  .HasForeignKey(e => e.AdminUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Users)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Customers)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureVendor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Vendors)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureEquipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Category).HasConversion<string>().IsRequired();
            entity.Property(e => e.HourlyRate).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.PurchaseCost).HasPrecision(18, 2);
            entity.Property(e => e.IsImplement).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Equipment)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                  .WithMany(v => v.Equipment)
                  .HasForeignKey(e => e.VendorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureServiceActivity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceActivity>(entity =>
        {
            entity.ToTable("service_activities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.BaseRatePerHour).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.ServiceActivities)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureInquiry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inquiry>(entity =>
        {
            entity.ToTable("inquiries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Inquiries)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceActivity)
                  .WithMany(a => a.Inquiries)
                  .HasForeignKey(e => e.ServiceActivityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Equipment)
                  .WithMany(eq => eq.Inquiries)
                  .HasForeignKey(e => e.EquipmentId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Salesperson)
                  .WithMany(u => u.SalesInquiries)
                  .HasForeignKey(e => e.SalespersonId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                (e.CenterId == _currentUser.CenterId &&
                 (_currentUser.Role != Role.Sales || e.SalespersonId == _currentUser.UserId)));
        });
    }

    private void ConfigureWorkOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.ToTable("work_orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().IsRequired();
            entity.Property(e => e.TotalMaterialCost).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.Property(e => e.ResponsibleUserId).HasColumnName("StaffId").IsRequired();

            entity.HasOne(e => e.ServiceActivity)
                  .WithMany(a => a.WorkOrders)
                  .HasForeignKey(e => e.ServiceActivityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Implement)
                  .WithMany(eq => eq.WorkOrdersAsImplement)
                  .HasForeignKey(e => e.ImplementId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tractor)
                  .WithMany(eq => eq.WorkOrdersAsTractor)
                  .HasForeignKey(e => e.TractorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.WorkOrders)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.WorkOrders)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsibleUser)
                  .WithMany(u => u.AssignedWorkOrders)
                  .HasForeignKey(e => e.ResponsibleUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Inquiry)
                  .WithMany()
                  .HasForeignKey(e => e.InquiryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);

            entity.HasIndex(w => new { w.CenterId, w.ScheduledStartDate, w.ScheduledEndDate })
                  .HasDatabaseName("IX_WorkOrders_CenterId_Schedule");
            entity.HasIndex(w => w.ImplementId)
                  .HasDatabaseName("IX_WorkOrders_ImplementId");
            entity.HasIndex(w => w.TractorId)
                  .HasDatabaseName("IX_WorkOrders_TractorId");
            entity.HasIndex(w => w.ServiceActivityId);
            entity.HasIndex(w => w.ResponsibleUserId)
                  .HasDatabaseName("IX_WorkOrders_ResponsibleUserId");
            entity.HasIndex(w => w.CustomerId);
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
        });
    }

    private void ConfigureAttendance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Latitude).HasPrecision(18, 6).IsRequired();
            entity.Property(e => e.Longitude).HasPrecision(18, 6).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().IsRequired();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Center)
                  .WithMany()
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureSalaryStructure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalaryStructure>(entity =>
        {
            entity.ToTable("salary_structures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BaseSalary).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CommissionPercentage).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Center)
                  .WithMany()
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureCommissionLedger(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommissionLedger>(entity =>
        {
            entity.ToTable("commission_ledgers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.UpiTransactionId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Inquiry)
                  .WithMany()
                  .HasForeignKey(e => e.InquiryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Center)
                  .WithMany()
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureInvoice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BaseAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.GstAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.AmountPaid).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.WorkOrder)
                  .WithMany()
                  .HasForeignKey(e => e.WorkOrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(e => e.CenterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(i => new { i.CenterId, i.Status })
                  .HasDatabaseName("IX_Invoices_CenterId_Status");

            entity.HasIndex(i => i.WorkOrderId)
                  .IsUnique()
                  .HasDatabaseName("IX_Invoices_WorkOrderId_Unique");

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigurePayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.PaymentMethod).HasConversion<string>().IsRequired();
            entity.Property(e => e.TransactionReference).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Payments)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.TransactionReference)
                  .IsUnique()
                  .HasDatabaseName("IX_Payments_TransactionReference_Unique");

            entity.HasIndex(p => p.InvoiceId)
                  .HasDatabaseName("IX_Payments_InvoiceId");

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
        });
    }

    private void ConfigureWorkOrderTimeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderTimeLog>(entity =>
        {
            entity.ToTable("work_order_time_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.WorkOrderId).IsRequired();
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.LogType).HasConversion<string>().IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Relationship
            entity.HasOne(e => e.WorkOrder)
                  .WithMany(w => w.TimeLogs)
                  .HasForeignKey(e => e.WorkOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index for querying logs by WorkOrder
            entity.HasIndex(e => e.WorkOrderId)
                  .HasDatabaseName("IX_WorkOrderTimeLogs_WorkOrderId");

            // Constraint: EndTime must be after StartTime
            entity.HasCheckConstraint(
                "CK_WorkOrderTimeLogs_EndTimeAfterStartTime",
                "\"EndTime\" > \"StartTime\"");
        });
    }
}

