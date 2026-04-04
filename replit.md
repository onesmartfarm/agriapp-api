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
│   ├── Entities/              # Center, User, Equipment, Inquiry, WorkOrder, AuditLog, Attendance, SalaryStructure, CommissionLedger
│   ├── Enums/                 # Role, WorkStatus, EquipmentCategory, InquiryStatus, AttendanceType, CommissionStatus
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
│   └── Services/              # EquipmentService, InquiryService, WorkOrderService, GstCalculator, CommissionCalculator, CommissionRealizationService, PayrollService
└── AgriApp.Api/               # Presentation layer
    ├── Controllers/           # Auth, Equipment, Inquiries, WorkOrders, Users, Health, Attendance, Payroll, Payment, SalaryStructure
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
- `GET /swagger` — Swagger UI documentation
- `GET /api/healthz` — Health check

## Workflow

- **AgriApp .NET API**: `cd src/AgriApp.Api && dotnet run` on port 5000

## Database

PostgreSQL via `DATABASE_URL` environment variable. EF Core migrations applied automatically on startup. JWT secret from `SESSION_SECRET` env var.
