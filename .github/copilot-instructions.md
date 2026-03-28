Project: AgriApp - Agricultural Equipment Rental & Maintenance API.

Stack: C# 12, .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL.

Architecture: Clean Architecture (Modular Monolith).
  - /src/AgriApp.Core: Domain Entities, Enums (Role, WorkStatus, EquipmentCategory, InquiryStatus), Interfaces (ICurrentUser, ICenterScoped, IAuditable). Zero dependencies.
  - /src/AgriApp.Infrastructure: AgriDbContext (EF Core), Migrations, Repositories. Depends on Core.
  - /src/AgriApp.Application: Service classes (EquipmentService, InquiryService, WorkOrderService), GST/Commission calculators using decimal, DTOs with DataAnnotations validation. Depends on Core + Infrastructure.
  - /src/AgriApp.Api: ASP.NET Core Controllers, JWT Bearer authentication, Swagger/OpenAPI, CurrentUser middleware. Depends on all layers.

Security Rules (Non-Negotiable):
  1. Global Query Filters: EF Core HasQueryFilter on Equipment, Inquiries, and WorkOrders filters by CenterId from ICurrentUser claims. SuperUser bypasses all filters.
  2. Salesperson Privacy: Inquiry HasQueryFilter enforces that Sales users ONLY see records where SalespersonId == CurrentUserId. This is a hard security rule.
  3. Registration is restricted to SuperUser and Manager roles only. Open self-registration is forbidden.
  4. JWT secret must come from SESSION_SECRET environment variable. No hardcoded fallback keys.

Financial Accuracy:
  - All currency fields use decimal type (C#) and numeric(10,2) in PostgreSQL.
  - GST calculations use decimal arithmetic with MidpointRounding.AwayFromZero.
  - Commission calculations use decimal arithmetic. No float/double for money.

Audit Trail:
  - EF Core SaveChangesInterceptor (AuditInterceptor) automatically logs all Create/Update/Delete actions to the audit_logs table.
  - Audit records include UserId, Timestamp, Action, EntityName, EntityId, OldValue, NewValue.
  - Password fields are excluded from audit serialization.

General Rules:
  - No UI code allowed. This is a backend-only API.
  - All DTOs must use DataAnnotations for input validation.
  - Controllers must call Application Service classes, never repositories directly.
  - Enum values stored as strings in PostgreSQL for readability.
