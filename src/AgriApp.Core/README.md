# AgriApp.Core

## Role in Clean Architecture

- **Innermost layer.** Holds **domain concepts only**: no HTTP, Blazor, EF, or application orchestration.
- **`Entities/`** — Persistence-oriented domain types (e.g. `User`, `Center`, `Customer`, `Vendor`, `Equipment`, `Inquiry`, `WorkOrder`, `Invoice`, `Payment`, `Attendance`, `SalaryStructure`, `CommissionLedger`, `AuditLog`).
- **`Enums/`** — Domain enumerations (`Role`, `WorkStatus`, `InquiryStatus`, `EquipmentCategory`, etc.).
- **`Interfaces/`** — Small contracts used across layers (`ICurrentUser`, `IAuditable`, `ICenterScoped`).

## Multi-tenancy: `CenterId`

- Most operational entities are **scoped to a center** via **`CenterId`** (see **`ICenterScoped`** where applicable).
- **`User`** may have **`CenterId`** (optional) for staff assignment; **`Center`** links to an optional **`AdminUserId`** (admin user for that center).
- **Filtering and authorization** are **not** implemented here — only the **data shape**. Tenant rules live in **Infrastructure** (`AgriDbContext` query filters) and **API** (`ICurrentUser`).

## Hard rule: POCO-only core

- **`AgriApp.Core.csproj` has no NuGet dependencies** beyond the SDK — keep it that way.
- **Do not** reference ASP.NET Core, Blazor, MudBlazor, EF Core, or HTTP client packages.
- Entities remain **POCOs** (properties + navigations); persistence configuration belongs in **AgriApp.Infrastructure**.
