Project: AgriApp - Agricultural Equipment Rental & Maintenance API.

Stack: C# 12, .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL.

Architecture: Clean Architecture (Modular Monolith).
  - /src/AgriApp.Core: Domain Entities, Enums (Role, WorkStatus, EquipmentCategory, InquiryStatus), Interfaces (ICurrentUser, ICenterScoped, IAuditable). Zero dependencies.
  - /src/AgriApp.Infrastructure: AgriDbContext (EF Core), Migrations, Repositories. Depends on Core.
  - /src/AgriApp.Application: Service classes (EquipmentService, InquiryService, WorkOrderService), GST/Commission calculators using decimal, DTOs with DataAnnotations validation. Depends on Core + Infrastructure.
  - /src/AgriApp.Api: ASP.NET Core Controllers, JWT Bearer authentication, Swagger/OpenAPI, CurrentUser middleware. Depends on all layers.

Security Rules (Non-Negotiable) — "Confidence-Back" Data Isolation:
  These filters are ACTIVE AT THE DATABASE LEVEL via EF Core Global Query Filters in AgriDbContext.OnModelCreating.
  1. Global Query Filters: EF Core HasQueryFilter on Equipment, Inquiries, and WorkOrders filters by CenterId from ICurrentUser claims (injected into DbContext constructor). SuperUser bypasses ALL filters — sees everything across the entire company.
  2. Salesperson Privacy: Inquiry HasQueryFilter enforces that Sales role users can ONLY see records where SalespersonId == CurrentUserId. Combined with CenterId filter. This is a HARD SECURITY RULE at the database query level.
  3. Registration is restricted to SuperUser and Manager roles only. Open self-registration is forbidden. Managers cannot escalate to SuperUser/Manager roles.
  4. Cross-tenant write protection: Non-SuperUser users are forced to their own CenterId on all create operations. The request payload CenterId is ignored for non-SuperUser.
  5. JWT secret must come from SESSION_SECRET environment variable. No hardcoded fallback keys.

Financial Accuracy:
  - All currency fields use decimal type (C#) and numeric(18,2) in PostgreSQL — configured via HasPrecision(18, 2) in AgriDbContext.
  - GST calculations use decimal arithmetic with MidpointRounding.AwayFromZero.
  - Commission calculations use decimal arithmetic. No float/double for money.
  - Any future entity with a currency/rate/commission field MUST be configured with HasPrecision(18, 2) in the DbContext.

Audit Trail:
  - EF Core SaveChangesInterceptor (AuditInterceptor) automatically logs all Create/Update/Delete actions to the audit_logs table.
  - Audit records include UserId, Timestamp, Action, EntityName, EntityId, OldValue, NewValue.
  - Password fields are excluded from audit serialization.

Stage 2 — Attendance, GPS & Payroll:
  - Attendance requires strict GPS coordinates. Requests with (0,0) are rejected.
  - Commissions are created as Pending. They only become Realized when a valid UpiTransactionId is supplied via the /api/payment/webhook endpoint.
  - The AuditInterceptor specifically tracks CommissionLedger status transitions to Realized, capturing the UpiTransactionId in the audit log.
  - Payroll formula: (BaseSalary × DaysPresent/30) + Sum(Realized Commissions for month).
  - Global Query Filters apply to Attendance, SalaryStructure, and CommissionLedger (CenterId isolation, SuperUser bypass).
  - SalaryStructure has a unique constraint on UserId (one salary structure per user).

Stage 3 — WorkOrders, Maintenance & Calendar:
  - All Calendar queries MUST use .AsNoTracking() for high performance (read-only, no EF change tracking).
  - Double-booking checks MUST use exact date overlap math: existingOrder.ScheduledStartDate < request.ScheduledEndDate && existingOrder.ScheduledEndDate > request.ScheduledStartDate. This allows back-to-back bookings (end at 12:00, next starts at 12:00 is allowed).
  - Cancelled work orders are excluded from double-booking checks.
  - WorkOrder.ResponsibleUserId maps to the legacy 'StaffId' column via HasColumnName("StaffId").
  - WorkOrder.EquipmentId is nullable — equipment is optional (e.g., InternalProject may have no equipment).
  - Composite index on (CenterId, ScheduledStartDate, ScheduledEndDate) is required for calendar performance.
  - Services must be stateless (no class-level mutable state) — thread-safe for concurrent requests.

General Rules:
  - No UI code allowed. This is a backend-only API.
  - All DTOs must use DataAnnotations for input validation.
  - Controllers must call Application Service classes, never repositories directly.
  - Enum values stored as strings in PostgreSQL for readability.
