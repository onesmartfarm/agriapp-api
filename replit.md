# AgriApp - Agricultural Equipment Rental & Maintenance API

## Overview

C# 12 / .NET 8 Clean Architecture backend API for managing agricultural equipment rentals, maintenance work orders, and customer inquiries. Uses EF Core with PostgreSQL, JWT authentication, Swagger/OpenAPI, Global Query Filters for multi-tenant CenterId isolation, and an EF Core AuditInterceptor for all financial mutations.

## Stack

- **Language**: C# 12
- **Framework**: .NET 8 / ASP.NET Core Web API
- **ORM**: Entity Framework Core 8 + Npgsql (PostgreSQL)
- **Authentication**: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- **API docs**: Swagger UI (Swashbuckle.AspNetCore)
- **Password hashing**: BCrypt.Net-Next
- **Financial arithmetic**: `decimal` with `MidpointRounding.AwayFromZero`

## Structure

```text
AgriApp.sln
src/
├── AgriApp.Core/              # Domain layer (zero dependencies)
│   ├── Entities/              # Center, User, Equipment, Inquiry, WorkOrder, AuditLog, Attendance, SalaryStructure, CommissionLedger, Invoice, Payment
│   ├── Enums/                 # Role, WorkStatus, EquipmentCategory, InquiryStatus, AttendanceType, CommissionStatus, InvoiceStatus, PaymentMethod
│   └── Interfaces/            # ICurrentUser, ICenterScoped, IAuditable
├── AgriApp.Infrastructure/    # Data access layer
│   ├── Data/
│   │   ├── AgriDbContext.cs   # EF Core context with Global Query Filters
│   │   └── Migrations/        # EF Core migrations
│   ├── Interceptors/
│   │   └── AuditInterceptor.cs # SaveChangesInterceptor for audit trail + CommissionRealized tracking
│   └── Repositories/          # UserRepository, EquipmentRepository, InquiryRepository, WorkOrderRepository
├── AgriApp.Application/       # Business logic layer
│   ├── DTOs/                  # Request/Response DTOs with DataAnnotations
│   └── Services/              # EquipmentService, InquiryService, WorkOrderService, GstCalculator, CommissionCalculator, CommissionRealizationService, PayrollService, InvoiceService, PaymentService
└── AgriApp.Api/               # Presentation layer
    ├── Controllers/           # Auth, Equipment, Inquiries, WorkOrders, Users, Health, Attendance, Payroll, Payment, SalaryStructure, Invoices, Payments
    ├── Middleware/             # CurrentUser (ICurrentUser implementation from JWT claims)
    ├── Program.cs             # DI registration, JWT config, EF Core setup, seed data
    └── appsettings.json       # Configuration
```

## Security Model

- **CenterId Global Query Filter**: Equipment, Inquiries, WorkOrders, Attendance, SalaryStructure, CommissionLedger are automatically filtered by the user's CenterId
- **Sales Ownership Privacy**: Sales users ONLY see Inquiries where `SalespersonId == CurrentUserId`
- **SuperUser Bypass**: SuperUser role ignores all CenterId and ownership filters
- **Registration Restriction**: Only SuperUser and Manager can register new users

## Seeded Accounts

| Role       | Email                | Password       |
|------------|----------------------|----------------|
| SuperUser  | admin@agriapp.com    | SuperUser123!  |
| Manager    | rajesh@agriapp.com   | Manager123!    |
| Sales      | priya@agriapp.com    | Sales123!      |
| Staff      | amit@agriapp.com     | Staff123!      |

## API Endpoints

- `POST /api/auth/login` — Login (returns JWT)
- `POST /api/auth/register` — Register new user (SuperUser/Manager only)
- `GET /api/equipment` — List equipment (center-filtered)
- `GET /api/equipment/{id}` — Get equipment by ID
- `POST /api/equipment` — Create equipment (Manager/SuperUser)
- `PUT /api/equipment/{id}` — Update equipment (Manager/SuperUser)
- `DELETE /api/equipment/{id}` — Delete equipment (Manager/SuperUser)
- `POST /api/equipment/{id}/quote` — Rental quote with GST + commission
- `GET /api/inquiries` — List inquiries (ownership-filtered for Sales)
- `GET /api/inquiries/{id}` — Get inquiry by ID
- `POST /api/inquiries` — Create inquiry (Sales/Manager/SuperUser)
- `PATCH /api/inquiries/{id}/status` — Update inquiry status
- `GET /api/work-orders` — List work orders (center-filtered)
- `GET /api/work-orders/{id}` — Get work order by ID
- `POST /api/work-orders` — Create work order (Supervisor+)
- `PATCH /api/work-orders/{id}/status` — Update work order status
- `GET /api/users` — List users (Manager/SuperUser)
- `GET /api/users/me` — Current user profile
- `POST /api/attendance/clock` — Clock in/out with GPS coordinates
- `GET /api/attendance/my` — My attendance records
- `GET /api/attendance` — All attendance (Manager/SuperUser)
- `POST /api/salary-structures` — Create salary structure (Manager/SuperUser)
- `PUT /api/salary-structures/{userId}` — Update salary structure
- `GET /api/salary-structures` — List all salary structures
- `GET /api/salary-structures/{userId}` — Get salary by user
- `GET /api/payroll/report` — Payroll report (Manager/SuperUser)
- `POST /api/payment/webhook` — Realize commissions via UPI payment
- `POST /api/invoices/generate` — Generate Draft invoice from Completed WorkOrder (Manager/SuperUser)
- `GET /api/invoices` — List invoices (center-filtered)
- `GET /api/invoices/{id}` — Get invoice by ID
- `PATCH /api/invoices/{id}/issue` — Transition Draft → Issued (Manager/SuperUser)
- `POST /api/payments` — Record payment against Issued/PartiallyPaid invoice (Manager/SuperUser)
- `GET /swagger` — Swagger UI documentation
- `GET /api/healthz` — Health check

## Stage 5 — Blazor WASM Frontend (AgriApp.Web)

**Project**: `src/AgriApp.Web/` — Blazor WebAssembly Standalone, .NET 8, MudBlazor 8
**References**: `AgriApp.Core` (enums: Role, WorkOrderType, WorkStatus, InvoiceStatus)

### Infrastructure
- `Auth/JwtAuthenticationStateProvider.cs` — Reads JWT from LocalStorage, decodes claims, validates expiry, notifies Blazor auth state
- `Auth/JwtAuthorizationMessageHandler.cs` — DelegatingHandler: attaches `Authorization: Bearer <token>` to all secured HTTP requests
- Two named HttpClients: `PublicApi` (login, no token) and `SecuredApi` (token via handler)

### HTTP Services
- `IAuthService/AuthService` — `POST /api/auth/login`, stores token, calls `NotifyUserLoginAsync`
- `IWorkOrderService/WorkOrderService` — full CRUD for `/api/work-orders`
- `IAttendanceService/AttendanceService` — clock-in/out via `/api/attendance/clock`

### UI (MudBlazor)
- `Layout/MainLayout.razor` — MudLayout + MudAppBar (green #2E7D32 brand) + responsive drawer + logout
- `Layout/NavMenu.razor` — strictly role-gated `<AuthorizeView>` (Staff/Sales/Supervisor/Manager/SuperUser)
- `Layout/LoginLayout.razor` — full-screen green gradient wrapper for login page
- `Pages/Login.razor` — `EditForm` + `DataAnnotationsValidator` + ISnackbar error handling for 400/401/403
- `Pages/Dashboard.razor` — role-gated summary cards
- `Shared/RedirectToLogin.razor` — unauthenticated redirect with returnUrl

### Security & Validation
- `CascadingAuthenticationState` in `App.razor`; `AuthorizeRouteView` for all protected routes
- All forms use `<EditForm>` + `<DataAnnotationsValidator />` (no raw HTML injection)
- API errors (400/401/403/500) caught per-operation and displayed via `ISnackbar`

## Workflow

- **AgriApp .NET API**: `cd src/AgriApp.Api && dotnet run` on port 5000
- **AgriApp Blazor WASM**: `cd src/AgriApp.Web && dotnet run --urls http://0.0.0.0:6000` on port 6000

## Database

PostgreSQL via `DATABASE_URL` environment variable. EF Core migrations applied automatically on startup. JWT secret from `SESSION_SECRET` env var.
