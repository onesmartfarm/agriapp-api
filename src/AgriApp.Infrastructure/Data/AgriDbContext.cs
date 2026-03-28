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
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCenter(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureEquipment(modelBuilder);
        ConfigureInquiry(modelBuilder);
        ConfigureWorkOrder(modelBuilder);
        ConfigureAuditLog(modelBuilder);
    }

    private void ConfigureCenter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Center>(entity =>
        {
            entity.ToTable("centers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
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

    private void ConfigureEquipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Category).HasConversion<string>().IsRequired();
            entity.Property(e => e.HourlyRate).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Center)
                  .WithMany(c => c.Equipment)
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
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Equipment)
                  .WithMany(eq => eq.Inquiries)
                  .HasForeignKey(e => e.EquipmentId)
                  .OnDelete(DeleteBehavior.Restrict);

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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Equipment)
                  .WithMany(eq => eq.WorkOrders)
                  .HasForeignKey(e => e.EquipmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Staff)
                  .WithMany(u => u.AssignedWorkOrders)
                  .HasForeignKey(e => e.StaffId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e =>
                _currentUser == null ||
                _currentUser.Role == Role.SuperUser ||
                e.CenterId == _currentUser.CenterId);
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
}
