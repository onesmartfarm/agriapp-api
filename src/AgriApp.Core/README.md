# AgriApp.Core

## Role in Clean Architecture

- **Innermost layer.** Holds **domain concepts only**: no HTTP, Blazor, EF, or application orchestration.
- **`Entities/`** — Persistence-oriented domain types (e.g. `User`, `Center`, `Customer`, `Vendor`, `Equipment`, **`ServiceActivity`**, `Inquiry`, `WorkOrder`, **`WorkOrderTimeLog`**, `Invoice`, `Payment`, `Attendance`, `SalaryStructure`, `CommissionLedger`, `AuditLog`).
- **`Enums/`** — Domain enumerations (`Role`, `WorkStatus`, `InquiryStatus`, `EquipmentCategory`, **`WorkTimeLogType`**, etc.).
- **`Interfaces/`** — Small contracts used across layers (`ICurrentUser`, `IAuditable`, `ICenterScoped`).

## Multi-tenancy: `CenterId`

- Most operational entities are **scoped to a center** via **`CenterId`** (see **`ICenterScoped`** where applicable).
- **`User`** may have **`CenterId`** (optional) for staff assignment; **`Center`** links to an optional **`AdminUserId`** (admin user for that center).
- **`ServiceActivity`** is **`ICenterScoped`**: each center maintains its own catalog of billable field services (e.g. Rotavation, Cultivation) with a **`BaseRatePerHour`** used for invoice labor lines.
- **Filtering and authorization** are **not** implemented here — only the **data shape**. Tenant rules live in **Infrastructure** (`AgriDbContext` query filters) and **API** (`ICurrentUser`).

## Service activity model

- **`ServiceActivity`** describes **what service is being sold** (the activity billed to the customer), not which metal moved the dirt.
- Fields: **`Id`**, **`Name`**, **`Description`**, **`BaseRatePerHour`**, **`CenterId`**, audit timestamps; navigation to **`Center`** and **`WorkOrders`**.
- A **`WorkOrder`** may link **`ServiceActivityId`** → **`ServiceActivity`**. When present with **Working** time logs, invoice generation uses **`BaseRatePerHour`** × billable hours (see **AgriApp.Api** README for invoice rules).

## Equipment pairing: tractor vs. implement

- **`Equipment`** represents any asset row (tractor, vehicle, attachment, etc.).
- **`Equipment.IsImplement`**:
  - **`false`** — **power source** (tractor, vehicle, or other unit that pulls/provides power). Linked from **`WorkOrder.TractorId`** → **`Tractor`** navigation.
  - **`true`** — **tool / attachment** (implement). Linked from **`WorkOrder.ImplementId`** → **`Implement`** navigation.
- **Pairing logic (domain intent):**
  - **Tractor** = what provides power for the job (internal cost, fuel, utilization).
  - **Implement** = what actually does the soil/tool work (cultivator, rotavator, etc.).
  - Both FKs are **optional**; scheduling rules treat **implement** and **tractor** like distinct resources for double-booking checks (along with responsible staff).
- **Inquiries** still reference a single **`EquipmentId`** (equipment-of-interest for CRM); work orders use the **pair** + **service activity** for operations and billing.

## `WorkOrderTimeLog` and `WorkTimeLogType`

- **`WorkOrderTimeLog`** records **time spans** on a work order: **`StartTime`**, **`EndTime`**, **`LogType`**, optional **`Notes`**, **`WorkOrderId`**, audit fields.
- **`LogType`** (`**WorkTimeLogType**` enum):
  - **`Working`** — Billable field time. **Only** these rows contribute **hours** to invoice labor (sum of `(EndTime - StartTime)` for rows where `LogType == Working`).
  - **`Break`** — Non-billable (e.g. lunch, rest). Excluded from invoice hours.
  - **`Breakdown`** — Non-billable equipment downtime. Excluded from invoice hours.
- **Persistence:** PostgreSQL enforces **`EndTime > StartTime`** via a **CHECK** constraint on `work_order_time_logs` (see **AgriApp.Api** README). The API/application should validate the same rule before save.

## Hard rule: POCO-only core

- **`AgriApp.Core.csproj` has no NuGet dependencies** beyond the SDK — keep it that way.
- **Do not** reference ASP.NET Core, Blazor, MudBlazor, EF Core, or HTTP client packages.
- Entities remain **POCOs** (properties + navigations); persistence configuration belongs in **AgriApp.Infrastructure**.
