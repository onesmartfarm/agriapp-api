# AgriApp - Agricultural Equipment Rental & Maintenance API
## Complete Project Documentation

**Version**: 1.0.0  
**Last Updated**: 2025-03-28  
**Repository**: https://github.com/onesmartfarm/agriapp-api  
**Branch**: dev  
**Status**: 🟠 In Development (Core API Complete, Web Integration 70%)

---

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Project Structure](#project-structure)
5. [Database Schema](#database-schema)
6. [Core Entities](#core-entities)
7. [API Endpoints](#api-endpoints)
8. [Security Rules](#security-rules)
9. [Integration Status](#integration-status)
10. [Development Guidelines](#development-guidelines)
11. [Deployment](#deployment)

---

## Project Overview

**AgriApp** is a comprehensive Agricultural Equipment Rental & Maintenance API built on **Clean Architecture** principles. It manages:

- ✅ Equipment rental lifecycle (Equipment, Inquiries, WorkOrders)
- ✅ Staff attendance with GPS tracking
- ✅ Payroll calculation with commission realization
- ✅ Invoice generation and payment tracking
- ✅ Multi-center operation with role-based access control

### Key Features

| Feature | Status | Description |
|---------|--------|-------------|
| Equipment Management | ✅ Complete | Create, list, quote rental pricing |
| Inquiry Management | ✅ Complete | Track customer inquiries with status pipeline |
| Work Orders | ✅ Complete | Schedule equipment with double-booking validation |
| Attendance Tracking | ✅ Complete | GPS-based clock in/out with location verification |
| Payroll System | ✅ Complete | BaseSalary + Realized Commissions calculation |
| Invoice Generation | ✅ Complete | Auto-generate with 18% GST from WorkOrders |
| Payment Processing | ✅ Complete | Record payments with duplicate prevention |
| Commission Realization | ✅ Complete | UPI webhook for pending→realized transition |
| Web UI | 🟠 70% | Blazor WebAssembly (Auth, WorkOrders, Attendance done) |
| Calendar Capacity | 🟡 UI Only | Page exists, service needs implementation |

---

## Architecture

### Pattern: Clean Architecture (Modular Monolith)

```
┌─────────────────────────────────────────────────────────────┐
│                    AgriApp.Api (Presentation)               │
│         Controllers, JWT Auth, Swagger/OpenAPI              │
└────────────────────┬────────────────────────────────────────┘
                     │ Depends on
┌────────────────────▼────────────────────────────────────────┐
│              AgriApp.Application (Use Cases)                │
│    Service Classes, DTOs, Business Logic, Calculators      │
└────────────────────┬────────────────────────────────────────┘
                     │ Depends on
┌────────────────────▼────────────────────────────────────────┐
│           AgriApp.Infrastructure (Data & External)          │
│    EF Core DbContext, Migrations, Repositories, Auditing   │
└────────────────────┬────────────────────────────────────────┘
                     │ Depends on
┌────────────────────▼────────────────────────────────────────┐
│          AgriApp.Core (Domain / Business Rules)             │
│    Entities, Enums, Interfaces (ZERO external dependencies) │
└─────────────────────────────────────────────────────────────┘
```

### Separation of Concerns

| Layer | Responsibility | Zero Dependencies |
|-------|-----------------|-------------------|
| **Core** | Domain entities, enums, interfaces | ✅ Yes |
| **Infrastructure** | EF Core, database, auditing | Only depends on Core |
| **Application** | Business logic, services, DTOs | Only depends on Core + Infrastructure |
| **Api** | HTTP controllers, authentication | Depends on all |
| **Web** | Blazor WASM UI, client services | Independent (calls API via HTTP) |

---

## Tech Stack

### Backend
- **Language**: C# 12
- **Framework**: .NET 8 (LTS)
- **Web API**: ASP.NET Core 8
- **Database**: PostgreSQL 14+
- **ORM**: Entity Framework Core 8
- **Authentication**: JWT Bearer Token
- **Password Security**: BCrypt.Net

### Frontend (Blazor WebAssembly)
- **Framework**: Blazor WASM (.NET 8)
- **UI Library**: MUD Blazor v7
- **State Management**: JWT + AuthenticationStateProvider
- **Local Storage**: Blazored.LocalStorage
- **HTTP Client**: HttpClientFactory with JWT interceptor

### Tools & Libraries
- **API Documentation**: Swagger/OpenAPI
- **Validation**: DataAnnotations
- **Decimal Precision**: numeric(18,2) PostgreSQL
- **Localization**: .NET Globalization
- **Audit Trail**: SaveChangesInterceptor

---

## Project Structure

```
agriapp-api/
├── src/
│   ├── AgriApp.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs          (Login, Register)
│   │   │   ├── UsersController.cs         (Profile, List Users)
│   │   │   ├── EquipmentController.cs     (CRUD + Quote)
│   │   │   ├── InquiriesController.cs     (CRUD + Status)
│   │   │   ├── WorkOrdersController.cs    (CRUD + Status)
│   │   │   ├── AttendanceController.cs    (Clock, History)
│   │   │   ├── CalendarController.cs      (Capacity Planning)
│   │   │   ├── InvoicesController.cs      (Generate, Issue)
│   │   │   ├── PaymentsController.cs      (Record Payment)
│   │   │   ├── PaymentController.cs       (UPI Webhook)
│   │   │   ├── PayrollController.cs       (Reports)
│   │   │   ├── SalaryStructureController.cs (CRUD)
│   │   │   └── HealthController.cs        (Healthz)
│   │   ├── Middleware/
│   │   │   └── CurrentUser.cs             (Extracts JWT claims)
│   │   └── Program.cs                     (DI setup)
│   │
│   ├── AgriApp.Application/
│   │   ├── Services/
│   │   │   ├── EquipmentService.cs
│   │   │   ├── InquiryService.cs
│   │   │   ├── WorkOrderService.cs
│   │   │   ├── InvoiceService.cs
│   │   │   ├── PaymentService.cs
│   │   │   ├── PayrollService.cs
│   │   │   ├── CommissionRealizationService.cs
│   │   │   └── CalendarService.cs
│   │   ├── DTOs/
│   │   │   ├── EquipmentDtos.cs
│   │   │   ├── InquiryDtos.cs
│   │   │   ├── WorkOrderDtos.cs
│   │   │   ├── InvoiceDtos.cs
│   │   │   ├── PaymentDtos.cs
│   │   │   ├── PayrollDtos.cs
│   │   │   └── AttendanceDtos.cs
│   │   ├── Calculators/
│   │   │   ├── GstCalculator.cs
│   │   │   └── CommissionCalculator.cs
│   │   └── Validators/ (DataAnnotations on DTOs)
│   │
│   ├── AgriApp.Infrastructure/
│   │   ├── Data/
│   │   │   ├── AgriDbContext.cs           (DbContext with Global Query Filters)
│   │   │   ├── Migrations/                (EF Core Migration History)
│   │   │   └── AuditInterceptor.cs        (SaveChangesInterceptor)
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── EquipmentRepository.cs
│   │       └── (Others if needed)
│   │
│   ├── AgriApp.Core/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Center.cs
│   │   │   ├── Equipment.cs
│   │   │   ├── Inquiry.cs
│   │   │   ├── WorkOrder.cs
│   │   │   ├── Attendance.cs
│   │   │   ├── SalaryStructure.cs
│   │   │   ├── CommissionLedger.cs
│   │   │   ├── Invoice.cs
│   │   │   ├── Payment.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/
│   │   │   ├── Role.cs               (SuperUser, Manager, Sales, Staff)
│   │   │   ├── WorkStatus.cs         (Scheduled, InProgress, Completed, Cancelled)
│   │   │   ├── EquipmentCategory.cs  (Tractor, Drone, BioCNG)
│   │   │   ├── InquiryStatus.cs      (New, InProgress, Converted, Closed)
│   │   │   ├── InvoiceStatus.cs      (Draft, Issued, PartiallyPaid, Paid)
│   │   │   ├── CommissionStatus.cs   (Pending, Realized)
│   │   │   └── AttendanceType.cs     (ClockIn, ClockOut)
│   │   └── Interfaces/
│   │       ├── ICurrentUser.cs        (Claims principal extraction)
│   │       ├── ICenterScoped.cs       (CenterId property)
│   │       └── IAuditable.cs          (Audit timestamp tracking)
│   │
│   └── AgriApp.Web/
│       ├── Pages/
│       │   ├── Login.razor             (✅ Complete)
│       │   ├── Home.razor              (✅ Dashboard)
│       │   ├── WorkOrders.razor        (✅ List + CRUD UI)
│       │   ├── WorkOrderDialog.razor   (✅ Modal dialog)
│       │   ├── Calendar.razor          (⚠️ UI ready, service missing)
│       │   ├── Counter.razor           (Demo page)
│       │   ├── Weather.razor           (Demo page)
│       │   └── (Others)
│       ├── Services/
│       │   ├── IAuthService.cs         (✅ Complete)
│       │   ├── AuthService.cs          (✅ Complete)
│       │   ├── IWorkOrderService.cs    (🟡 Partial - missing create/update)
│       │   ├── WorkOrderService.cs     (🟡 Partial)
│       │   ├── IAttendanceService.cs   (✅ Complete)
│       │   ├── AttendanceService.cs    (✅ Complete)
│       │   └── ViewModels.cs           (DTOs for web)
│       ├── Layout/
│       │   ├── MainLayout.razor
│       │   ├── NavMenu.razor
│       │   └── CSS files
│       ├── Security/
│       │   ├── JwtAuthenticationStateProvider.cs
│       │   └── JwtAuthorizationMessageHandler.cs
│       ├── Resources/
│       │   ├── SharedResource.resx     (Localization)
│       │   └── SharedResource.mr.resx  (Marathi)
│       └── Program.cs                  (WASM host config)
│
└── .github/
    └── copilot-instructions.md         (This file's content)
```

---

## Database Schema

### Entity Relationship Diagram (ERD)

```
┌─────────────┐         ┌──────────────┐
│   centers   │◄────────┤     users    │
│  (CenterId) │ 1     * │ (CenterId?)  │
└─────────────┘         └──────────────┘
                               │
                        ┌──────┴──────────────┐
                        │                     │
              ┌─────────▼────────┐  ┌────────▼──────────┐
              │    inquiries     │  │  work_orders     │
              │  (SalespersonId) │  │ (ResponsibleUserId│
              │   (EquipmentId)  │  │   →StaffId)      │
              │   (CustomerId)   │  │ (EquipmentId?)   │
              │   (CenterId)     │  │ (CenterId)       │
              └──────────────────┘  └──────────────────┘
                        │                     │
                        │            ┌────────┘
                        │            │
              ┌─────────▼────────────▼──┐
              │       equipment         │
              │      (HourlyRate)       │
              │      (CenterId)         │
              └────────────────────────┘

┌─────────────────┐      ┌──────────────┐
│  attendances    │      │ salary_struct│
│  (UserId)       │◄─────┤ (UserId-PK)  │
│  (CenterId)     │  1  │ (CenterId)   │
│  (GPS coords)   │      │ (BaseSalary) │
└─────────────────┘      └──────────────┘

┌──────────────────┐     ┌───────────────┐
│ commission_ledge │     │    invoices   │
│    (Pending→    │     │  (Draft→      │
│   Realized)     │     │ Issued→Paid)  │
│  (InquiryId)    │     │  (WorkOrderId)│
│  (UpiTransId)   │     │  (CenterId)   │
└──────────────────┘     └───────────────┘
                               │
                        ┌──────▼──────┐
                        │  payments    │
                        │ (InvoiceId)  │
                        │ (TransRef)   │
                        └──────────────┘

┌──────────────────┐
│   audit_logs     │
│  (Entity CRUD)   │
│  (OldValue       │
│   NewValue)      │
└──────────────────┘
```

---

## Core Entities

### 1. **User** (users)

```sql
CREATE TABLE users (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  role VARCHAR(50) NOT NULL DEFAULT 'Staff',  -- SuperUser|Manager|Sales|Staff
  center_id INT REFERENCES centers(id),
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP
);
```

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| id | INT | PK | Auto-increment |
| name | VARCHAR(255) | NOT NULL | Full name |
| email | VARCHAR(255) | UNIQUE, NOT NULL | Login credential |
| password_hash | VARCHAR(255) | NOT NULL | BCrypt hash |
| role | VARCHAR(50) | NOT NULL | See Role enum |
| center_id | INT | FK→centers | Optional (SuperUser may not be assigned) |
| created_at | TIMESTAMP | DEFAULT NOW() | Audit |
| updated_at | TIMESTAMP | | Audit |

**Relationships**:
- `Center` (1-to-many) — User belongs to one Center
- `SalesInquiries` (1-to-many) — User as Salesperson in Inquiries
- `AssignedWorkOrders` (1-to-many) — User as ResponsibleUserId in WorkOrders

**Key Validations**:
- Managers cannot register other Managers or SuperUsers
- Managers cannot elevate their own role
- Non-SuperUser creates inherit creator's CenterId (cannot override)

---

### 2. **Center** (centers)

```sql
CREATE TABLE centers (
  id SERIAL PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  location VARCHAR(500) NOT NULL,
  created_at TIMESTAMP DEFAULT NOW()
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| name | VARCHAR(200) | Center name |
| location | VARCHAR(500) | Address |
| created_at | TIMESTAMP | Created timestamp |

**Relationships**:
- `Users` (1-to-many)
- `Equipment` (1-to-many)
- `Inquiries` (1-to-many)
- `WorkOrders` (1-to-many)
- `Attendances` (1-to-many)

---

### 3. **Equipment** (equipment)

```sql
CREATE TABLE equipment (
  id SERIAL PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  category VARCHAR(50) NOT NULL,  -- Tractor|Drone|BioCNG
  hourly_rate NUMERIC(18,2) NOT NULL,
  center_id INT NOT NULL REFERENCES centers(id),
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP,
  
  UNIQUE(name, center_id)
);
```

| Field | Type | Precision | Notes |
|-------|------|-----------|-------|
| id | INT | | PK |
| name | VARCHAR(200) | | Equipment name |
| category | VARCHAR(50) | | Tractor, Drone, BioCNG |
| hourly_rate | NUMERIC | 18,2 | Decimal for currency |
| center_id | INT | | FK→centers |
| created_at | TIMESTAMP | | Audit |
| updated_at | TIMESTAMP | | Audit |

**Global Query Filter**: CenterId match (SuperUser bypasses)  
**Relationships**: Inquiries, WorkOrders (1-to-many)

---

### 4. **Inquiry** (inquiries)

```sql
CREATE TABLE inquiries (
  id SERIAL PRIMARY KEY,
  customer_id INT NOT NULL REFERENCES users(id),
  equipment_id INT NOT NULL REFERENCES equipment(id),
  salesperson_id INT NOT NULL REFERENCES users(id),
  center_id INT NOT NULL REFERENCES centers(id),
  status VARCHAR(50) NOT NULL DEFAULT 'New',  -- New|InProgress|Converted|Closed
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| customer_id | INT | FK→users (customer) |
| equipment_id | INT | FK→equipment |
| salesperson_id | INT | FK→users (salesperson) |
| center_id | INT | FK→centers |
| status | VARCHAR(50) | New, InProgress, Converted, Closed |
| created_at | TIMESTAMP | Audit |
| updated_at | TIMESTAMP | Audit |

**Global Query Filter**:
1. CenterId match (SuperUser bypasses)
2. **Salesperson Privacy**: Sales users can ONLY see records where SalespersonId == CurrentUserId

---

### 5. **WorkOrder** (work_orders)

```sql
CREATE TABLE work_orders (
  id SERIAL PRIMARY KEY,
  description VARCHAR(500) NOT NULL,
  scheduled_start_date TIMESTAMP NOT NULL,
  scheduled_end_date TIMESTAMP NOT NULL,
  equipment_id INT REFERENCES equipment(id),  -- Optional
  responsible_user_id INT REFERENCES users(id),  -- Maps to column: staff_id
  center_id INT NOT NULL REFERENCES centers(id),
  status VARCHAR(50) NOT NULL DEFAULT 'Scheduled',  -- Scheduled|InProgress|Completed|Cancelled
  total_material_cost NUMERIC(18,2) DEFAULT 0,
  additional_fees NUMERIC(18,2) DEFAULT 0,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP,
  
  CONSTRAINT check_dates CHECK (scheduled_end_date > scheduled_start_date),
  INDEX idx_calendar (center_id, scheduled_start_date, scheduled_end_date)
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| description | VARCHAR(500) | Work description |
| scheduled_start_date | TIMESTAMP | Start of work |
| scheduled_end_date | TIMESTAMP | End of work |
| equipment_id | INT | FK→equipment (Optional) |
| responsible_user_id | INT | FK→users (Staff) |
| center_id | INT | FK→centers |
| status | VARCHAR(50) | Scheduled, InProgress, Completed, Cancelled |
| total_material_cost | NUMERIC(18,2) | Sum of materials |
| additional_fees | NUMERIC(18,2) | Other fees |
| created_at | TIMESTAMP | Audit |
| updated_at | TIMESTAMP | Audit |

**Key Rules**:
- Double-booking validation: No overlapping `[start_date, end_date)` for same equipment
- Back-to-back allowed: end at 12:00, next starts at 12:00 ✅
- Cancelled orders excluded from double-booking checks
- Composite index on (CenterId, ScheduledStartDate, ScheduledEndDate) for calendar performance

---

### 6. **Attendance** (attendances)

```sql
CREATE TABLE attendances (
  id SERIAL PRIMARY KEY,
  user_id INT NOT NULL REFERENCES users(id),
  center_id INT NOT NULL REFERENCES centers(id),
  timestamp TIMESTAMP NOT NULL DEFAULT NOW(),
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  type VARCHAR(50) NOT NULL,  -- ClockIn|ClockOut
  created_at TIMESTAMP DEFAULT NOW(),
  
  CONSTRAINT check_gps CHECK NOT (latitude = 0 AND longitude = 0)
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| user_id | INT | FK→users |
| center_id | INT | FK→centers |
| timestamp | TIMESTAMP | Clock time |
| latitude | FLOAT | GPS coord (rejects 0,0) |
| longitude | FLOAT | GPS coord (rejects 0,0) |
| type | VARCHAR(50) | ClockIn or ClockOut |
| created_at | TIMESTAMP | Audit |

**Validation**: GPS coordinates (0,0) are **strictly rejected** (prevents false clocking)

---

### 7. **SalaryStructure** (salary_structures)

```sql
CREATE TABLE salary_structures (
  id SERIAL PRIMARY KEY,
  user_id INT UNIQUE NOT NULL REFERENCES users(id),  -- One per user
  center_id INT NOT NULL REFERENCES centers(id),
  base_salary NUMERIC(18,2) NOT NULL,
  commission_percentage NUMERIC(5,2) NOT NULL,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| user_id | INT | UNIQUE FK→users (one-to-one) |
| center_id | INT | FK→centers |
| base_salary | NUMERIC(18,2) | Base monthly salary |
| commission_percentage | NUMERIC(5,2) | % of realized commissions |
| created_at | TIMESTAMP | Audit |
| updated_at | TIMESTAMP | Audit |

**Constraint**: One salary structure per user (UNIQUE on user_id)

---

### 8. **CommissionLedger** (commission_ledgers)

```sql
CREATE TABLE commission_ledgers (
  id SERIAL PRIMARY KEY,
  inquiry_id INT NOT NULL REFERENCES inquiries(id),
  user_id INT NOT NULL REFERENCES users(id),
  center_id INT NOT NULL REFERENCES centers(id),
  amount NUMERIC(18,2) NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'Pending',  -- Pending|Realized
  upi_transaction_id VARCHAR(255),
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| inquiry_id | INT | FK→inquiries |
| user_id | INT | FK→users (salesperson) |
| center_id | INT | FK→centers |
| amount | NUMERIC(18,2) | Commission amount |
| status | VARCHAR(50) | Pending → Realized (on UPI webhook) |
| upi_transaction_id | VARCHAR(255) | From webhook, audit tracked |
| created_at | TIMESTAMP | Audit |
| updated_at | TIMESTAMP | Audit |

**Lifecycle**: Pending → Realized (via `/api/payment/webhook`)  
**Audit**: AuditInterceptor tracks status transition + UpiTransactionId

---

### 9. **Invoice** (invoices)

```sql
CREATE TABLE invoices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id INT NOT NULL REFERENCES work_orders(id),
  center_id INT NOT NULL REFERENCES centers(id),
  customer_id INT NOT NULL REFERENCES users(id),
  base_amount NUMERIC(18,2) NOT NULL,
  gst_amount NUMERIC(18,2) NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,
  amount_paid NUMERIC(18,2) DEFAULT 0,
  due_date DATE NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'Draft',  -- Draft|Issued|PartiallyPaid|Paid
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | UUID | PK |
| work_order_id | INT | FK→work_orders (one-to-one) |
| center_id | INT | FK→centers |
| customer_id | INT | FK→users |
| base_amount | NUMERIC(18,2) | TotalMaterialCost + AdditionalFees |
| gst_amount | NUMERIC(18,2) | 18% of base_amount |
| total_amount | NUMERIC(18,2) | base_amount + gst_amount |
| amount_paid | NUMERIC(18,2) | Sum of payments |
| due_date | DATE | Payment deadline |
| status | VARCHAR(50) | Draft → Issued → PartiallyPaid → Paid |
| created_at | TIMESTAMP | Audit |
| updated_at | TIMESTAMP | Audit |

**Formula**:
```
base_amount = work_order.total_material_cost + work_order.additional_fees
gst_amount = base_amount * 0.18  (18% GST)
total_amount = base_amount + gst_amount
```

**Status Flow**:
```
Draft (generated) → Issued (payable) → PartiallyPaid → Paid
```

---

### 10. **Payment** (payments)

```sql
CREATE TABLE payments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  invoice_id UUID NOT NULL REFERENCES invoices(id),
  amount NUMERIC(18,2) NOT NULL,
  transaction_reference VARCHAR(255) UNIQUE NOT NULL,  -- Prevents duplicates
  payment_method VARCHAR(50),
  recorded_at TIMESTAMP DEFAULT NOW()
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | UUID | PK |
| invoice_id | UUID | FK→invoices |
| amount | NUMERIC(18,2) | Payment amount |
| transaction_reference | VARCHAR(255) | UNIQUE (prevents UPI double-payment) |
| payment_method | VARCHAR(50) | UPI, Cash, Bank Transfer, etc. |
| recorded_at | TIMESTAMP | Payment timestamp |

**Key Rule**: Duplicate `transaction_reference` is rejected (prevents duplicate UPI payments)

---

### 11. **AuditLog** (audit_logs)

```sql
CREATE TABLE audit_logs (
  id SERIAL PRIMARY KEY,
  user_id INT,
  action VARCHAR(50) NOT NULL,  -- Create|Update|Delete
  entity_name VARCHAR(100) NOT NULL,
  entity_id VARCHAR(255) NOT NULL,
  old_value TEXT,
  new_value TEXT,
  timestamp TIMESTAMP DEFAULT NOW()
);
```

| Field | Type | Notes |
|-------|------|-------|
| id | INT | PK |
| user_id | INT | FK→users (who made change) |
| action | VARCHAR(50) | Create, Update, Delete |
| entity_name | VARCHAR(100) | Entity type (Invoice, User, etc.) |
| entity_id | VARCHAR(255) | PK of changed entity |
| old_value | TEXT | JSON before change |
| new_value | TEXT | JSON after change |
| timestamp | TIMESTAMP | When changed |

**Auto-Tracked**: SaveChangesInterceptor logs all changes (except password fields)

---

## API Endpoints

### Authentication (13 endpoints total)

#### **POST `/api/auth/login`** — User Login
```http
POST /api/auth/login HTTP/1.1
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Response 200:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "role": "Staff",
  "centerId": 1
}
```

#### **POST `/api/auth/register`** — Register User (SuperUser & Manager only)
```http
POST /api/auth/register HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "secure123",
  "role": "Staff",
  "centerId": 1
}

Response 201: (Same as login response)
```

---

### Users (2 endpoints)

#### **GET `/api/users`** — List All Users (Manager+)
```http
GET /api/users HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "role": "Staff",
    "centerId": 1
  },
  ...
]
```

#### **GET `/api/users/me`** — Get Current User Profile
```http
GET /api/users/me HTTP/1.1
Authorization: Bearer <token>

Response 200: (Same structure as above)
```

---

### Equipment (6 endpoints)

#### **GET `/api/equipment`** — List Equipment
```http
GET /api/equipment HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "name": "Tractor A",
    "category": "Tractor",
    "hourlyRate": 500.00,
    "centerId": 1
  },
  ...
]
```

#### **GET `/api/equipment/{id}`** — Get Equipment by ID
```http
GET /api/equipment/1 HTTP/1.1
Authorization: Bearer <token>

Response 200: (Same structure as above)
```

#### **POST `/api/equipment`** — Create Equipment (Manager+)
```http
POST /api/equipment HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Drone X1",
  "category": "Drone",
  "hourlyRate": 1000.00,
  "centerId": 1
}

Response 201: (Same structure)
```

#### **PUT `/api/equipment/{id}`** — Update Equipment (Manager+)
```http
PUT /api/equipment/1 HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Tractor A Updated",
  "category": "Tractor",
  "hourlyRate": 550.00
}

Response 200: (Same structure)
```

#### **DELETE `/api/equipment/{id}`** — Delete Equipment (Manager+)
```http
DELETE /api/equipment/1 HTTP/1.1
Authorization: Bearer <token>

Response 200: { "message": "Equipment deleted" }
```

#### **POST `/api/equipment/{id}/quote`** — Get Rental Quote
```http
POST /api/equipment/1/quote HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "hours": 10
}

Response 200:
{
  "equipment": {
    "id": 1,
    "name": "Tractor A",
    "category": "Tractor"
  },
  "hours": 10,
  "pricing": {
    "baseAmount": 5000.00,
    "gstAmount": 900.00,
    "totalAmount": 5900.00
  },
  "commission": 590.00
}
```

---

### Inquiries (4 endpoints)

#### **GET `/api/inquiries`** — List Inquiries
```http
GET /api/inquiries HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "customerId": 5,
    "equipmentId": 1,
    "salespersonId": 2,
    "status": "New"
  },
  ...
]
```

#### **GET `/api/inquiries/{id}`** — Get Inquiry by ID
```http
GET /api/inquiries/1 HTTP/1.1
Authorization: Bearer <token>

Response 200: (Same structure as above)
```

#### **POST `/api/inquiries`** — Create Inquiry (Sales+)
```http
POST /api/inquiries HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "customerId": 5,
  "equipmentId": 1,
  "salespersonId": 2,
  "centerId": 1
}

Response 201: (Same structure)
```

#### **PATCH `/api/inquiries/{id}/status`** — Update Inquiry Status (Sales+)
```http
PATCH /api/inquiries/1/status HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "status": "Converted"
}

Response 200: (Updated inquiry)
```

---

### Work Orders (4 endpoints)

#### **GET `/api/workorders`** — List Work Orders
```http
GET /api/workorders HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "description": "Plow field at Farm A",
    "status": "Scheduled",
    "scheduledStartDate": "2025-04-01T08:00:00Z",
    "scheduledEndDate": "2025-04-01T16:00:00Z",
    "equipmentName": "Tractor A",
    "totalMaterialCost": 500.00
  },
  ...
]
```

#### **GET `/api/workorders/{id}`** — Get Work Order by ID
```http
GET /api/workorders/1 HTTP/1.1
Authorization: Bearer <token>

Response 200: (Same structure as above)
```

#### **POST `/api/workorders`** — Create Work Order (Manager+)
```http
POST /api/workorders HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "description": "Plow field at Farm A",
  "scheduledStartDate": "2025-04-01T08:00:00Z",
  "scheduledEndDate": "2025-04-01T16:00:00Z",
  "equipmentId": 1,
  "responsibleUserId": 3,
  "centerId": 1,
  "totalMaterialCost": 500.00,
  "additionalFees": 100.00
}

Response 201: (Same structure)
```

**Validation**:
- Double-booking check: No overlapping times for same equipment
- Cancelled orders excluded from check
- Back-to-back bookings allowed

#### **PATCH `/api/workorders/{id}/status`** — Update Work Order Status
```http
PATCH /api/workorders/1/status HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "status": "Completed"
}

Response 200: (Updated work order)
```

---

### Attendance (3 endpoints)

#### **POST `/api/attendance/clock`** — Clock In/Out
```http
POST /api/attendance/clock HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "type": "ClockIn",
  "latitude": 18.5204,
  "longitude": 73.8567
}

Response 201:
{
  "id": 1,
  "userId": 3,
  "centerId": 1,
  "timestamp": "2025-03-28T08:30:00Z",
  "latitude": 18.5204,
  "longitude": 73.8567,
  "type": "ClockIn"
}
```

**Validation**: GPS (0,0) is **rejected** with error "GPS coordinates are required"

#### **GET `/api/attendance/my`** — Get My Attendance Records
```http
GET /api/attendance/my?month=3&year=2025 HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "userId": 3,
    "centerId": 1,
    "timestamp": "2025-03-28T08:30:00Z",
    "latitude": 18.5204,
    "longitude": 73.8567,
    "type": "ClockIn"
  },
  ...
]
```

#### **GET `/api/attendance`** — Get All Attendance (Manager+)
```http
GET /api/attendance?month=3&year=2025&userId=3 HTTP/1.1
Authorization: Bearer <token>

Response 200: (List of attendance records)
```

---

### Calendar (1 endpoint)

#### **GET `/api/calendar/capacity`** — Get Capacity (Read-only)
```http
GET /api/calendar/capacity?start=2025-04-01&end=2025-04-30 HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "date": "2025-04-01",
    "equipmentName": "Tractor A",
    "workOrderCount": 2,
    "totalDurationHours": 8.5,
    "utilizationPercentage": 0.35
  },
  ...
]
```

**Performance**: Uses `.AsNoTracking()` — read-only, no EF change tracking

---

### Invoices (3 endpoints)

#### **GET `/api/invoices`** — List Invoices
```http
GET /api/invoices HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "workOrderId": 1,
    "centerId": 1,
    "customerId": 5,
    "baseAmount": 600.00,
    "gstAmount": 108.00,
    "totalAmount": 708.00,
    "amountPaid": 0.00,
    "dueDate": "2025-04-30",
    "status": "Draft"
  },
  ...
]
```

#### **GET `/api/invoices/{id}`** — Get Invoice by ID
```http
GET /api/invoices/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Authorization: Bearer <token>

Response 200: (Same structure)
```

#### **POST `/api/invoices/generate`** — Generate Invoice from Work Order (Manager+)
```http
POST /api/invoices/generate HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "workOrderId": 1,
  "centerId": 1
}

Response 201:
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Draft",
  "totalAmount": 708.00,
  ...
}
```

**Formula**:
```
baseAmount = workOrder.totalMaterialCost + workOrder.additionalFees
gstAmount = baseAmount * 0.18
totalAmount = baseAmount + gstAmount
```

#### **PATCH `/api/invoices/{id}/issue`** — Transition Draft → Issued (Manager+)
```http
PATCH /api/invoices/550e8400-e29b-41d4-a716-446655440000/issue HTTP/1.1
Authorization: Bearer <token>

Response 200: (Updated invoice with status = "Issued")
```

---

### Payments (2 endpoints)

#### **POST `/api/payments`** — Record Payment (Manager+)
```http
POST /api/payments HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "invoiceId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 708.00,
  "transactionReference": "UPI123456789",
  "paymentMethod": "UPI"
}

Response 201:
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "invoiceId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 708.00,
  "transactionReference": "UPI123456789",
  "recordedAt": "2025-03-28T10:15:00Z"
}
```

**Key Rules**:
- Duplicate `transactionReference` rejected (prevents duplicate UPI payments)
- Auto-transition: If `amountPaid >= totalAmount`, invoice → Paid

#### **POST `/api/payment/webhook`** — UPI Payment Webhook
```http
POST /api/payment/webhook HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "upiTransactionId": "UPI123456789",
  "inquiryId": 1
}

Response 200:
{
  "upiTransactionId": "UPI123456789",
  "inquiryId": 1,
  "commissionsRealized": 1,
  "totalAmountRealized": 100.00
}
```

**Action**: CommissionLedger status Pending → Realized  
**Audit**: Tracked via AuditInterceptor with UpiTransactionId

---

### Salary Structure (3 endpoints)

#### **POST `/api/salary-structures`** — Create Salary Structure (Manager+)
```http
POST /api/salary-structures HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": 3,
  "baseSalary": 30000.00,
  "commissionPercentage": 5.00,
  "centerId": 1
}

Response 201:
{
  "id": 1,
  "userId": 3,
  "centerId": 1,
  "baseSalary": 30000.00,
  "commissionPercentage": 5.00
}
```

**Constraint**: One salary structure per user (rejects if already exists)

#### **PUT `/api/salary-structures/{userId}`** — Update Salary Structure (Manager+)
```http
PUT /api/salary-structures/3 HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "baseSalary": 35000.00,
  "commissionPercentage": 6.00,
  "centerId": 1
}

Response 200: (Updated structure)
```

#### **GET `/api/salary-structures`** — List Salary Structures (Manager+)
```http
GET /api/salary-structures HTTP/1.1
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 1,
    "userId": 3,
    "centerId": 1,
    "baseSalary": 30000.00,
    "commissionPercentage": 5.00
  },
  ...
]
```

---

### Payroll (1 endpoint)

#### **GET `/api/payroll/report`** — Calculate Payroll Report (Manager+)
```http
GET /api/payroll/report?month=3&year=2025&userId=3 HTTP/1.1
Authorization: Bearer <token>

Response 200:
{
  "userId": 3,
  "month": 3,
  "year": 2025,
  "baseSalary": 30000.00,
  "daysPresent": 20,
  "salaryForDays": 20000.00,
  "realizedCommissions": 500.00,
  "totalPay": 20500.00
}
```

**Formula**:
```
salaryForDays = (baseSalary * daysPresent) / 30
totalPay = salaryForDays + sum(realizedCommissions for month)
```

**Or without userId** (center-wide):
```http
GET /api/payroll/report?month=3&year=2025&centerId=1 HTTP/1.1

Response 200: (List of user reports)
```

---

### Health Check (1 endpoint)

#### **GET `/api/healthz`** — Health Check (Public)
```http
GET /api/healthz HTTP/1.1

Response 200: { "status": "ok" }
```

---

## Security Rules

### 🔐 Non-Negotiable "Confidence-Back" Data Isolation

#### 1. **Global Query Filters** (Database-Level via EF Core)

Enforced in `AgriDbContext.OnModelCreating()`:

```csharp
// Equipment
modelBuilder.Entity<Equipment>()
    .HasQueryFilter(e => 
        _currentUser!.Role == Role.SuperUser 
        || e.CenterId == _currentUser.CenterId);

// Inquiries (DUAL filter)
modelBuilder.Entity<Inquiry>()
    .HasQueryFilter(i => 
        (_currentUser!.Role == Role.SuperUser 
            || i.CenterId == _currentUser.CenterId)
        && (_currentUser.Role != Role.Sales 
            || i.SalespersonId == _currentUser.UserId));

// WorkOrders, Attendances, etc.
// Similar CenterId filtering
```

**Effect**:
- ✅ SuperUser sees ALL records across all centers
- ✅ Manager/Staff see only their center's records
- ✅ Sales users additionally filtered to their own inquiries

---

#### 2. **Salesperson Privacy** (Hard Security Rule)

Sales role users **CANNOT** see inquiries where `SalespersonId != CurrentUserId`.

```csharp
// Example: Sales user can only see inquiries they created
var myInquiries = await _db.Inquiries
    .Where(i => i.SalespersonId == _currentUser.UserId)
    .ToListAsync();
```

**Enforcement**: `HasQueryFilter` + authorization checks in controllers

---

#### 3. **Registration Restrictions**

```csharp
// Only SuperUser & Manager can call POST /api/auth/register
[Authorize(Roles = "SuperUser,Manager")]
public async Task<ActionResult<AuthResponse>> Register(...)

// Validation logic:
if (role == Role.SuperUser && callerRole != Role.SuperUser)
    return Forbid();  // Managers cannot create SuperUsers

if (role == Role.Manager && callerRole != Role.SuperUser)
    return Forbid();  // Managers cannot create Managers
```

---

#### 4. **Cross-Tenant Write Protection**

Non-SuperUser requests are forced to their own `CenterId`:

```csharp
int centerId;
if (_currentUser.Role == Role.SuperUser)
    centerId = request.CenterId;  // SuperUser can override
else
    centerId = _currentUser.CenterId;  // Non-SuperUser locked to their center
```

**Effect**: Prevents non-SuperUser from creating records in other centers

---

#### 5. **JWT Secret from Environment Variable**

```csharp
private string GenerateToken(User user)
{
    var jwtKey = _config["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT key not configured");
    
    // No hardcoded fallback keys allowed
}
```

**Required**: Set `JWT:Key` environment variable (or `SESSION_SECRET`)

---

### 💰 Financial Accuracy

- **All currency fields**: `NUMERIC(18,2)` type
- **C# decimal**: All calculations use `decimal` (no `float`/`double`)
- **GST & Commission**: Use `decimal` arithmetic with `MidpointRounding.AwayFromZero`
- **Example**:
  ```csharp
  decimal gst = baseAmount * 0.18m;
  decimal totalAmount = baseAmount + Decimal.Round(gst, 2, MidpointRounding.AwayFromZero);
  ```

---

### 📝 Audit Trail

**SaveChangesInterceptor** automatically logs:

```sql
INSERT INTO audit_logs (user_id, action, entity_name, entity_id, old_value, new_value, timestamp)
VALUES (3, 'Update', 'Invoice', 'uuid123', '{"status":"Draft"}', '{"status":"Issued"}', NOW());
```

**Tracked**:
- ✅ Create, Update, Delete actions
- ✅ User who made change (UserId)
- ✅ Old value / New value (JSON)
- ✅ Password fields excluded from serialization

---

### 🎭 Role-Based Authorization

| Role | Can Do | Cannot Do |
|------|--------|-----------|
| **SuperUser** | Everything, bypass all filters | N/A |
| **Manager** | Create staff, manage own center | Escalate roles, access other centers |
| **Sales** | Create inquiries, see own inquiries | See other sales inquiries, manage finances |
| **Staff** | Clock in/out, update work order status | Manage users, finances |

---

## Integration Status

### ✅ **FULLY INTEGRATED** (3/13 Core APIs)

| API | Service | UI | Status |
|-----|---------|----|----|
| **Authentication** | AuthService | Login.razor | ✅ Complete |
| **Work Orders** | WorkOrderService (partial) | WorkOrders.razor | ✅ Complete (read-only) |
| **Attendance** | AttendanceService | (pending component) | ✅ Complete (API) |

---

### 🟡 **PARTIALLY INTEGRATED** (1/13)

| API | Service | UI | Missing |
|-----|---------|----|----|
| **Calendar** | CalendarService (not created) | Calendar.razor (UI exists) | Service implementation |

---

### ❌ **NOT INTEGRATED** (9/13)

| # | API | Required For | Priority |
|---|-----|--------------|----------|
| 1 | Equipment | WorkOrder creation, Quotes | 🔴 CRITICAL |
| 2 | Inquiries | Sales pipeline | 🔴 CRITICAL |
| 3 | Invoices | Financial tracking | 🔴 CRITICAL |
| 4 | Payments | Revenue collection | 🔴 CRITICAL |
| 5 | Commission Webhook | Payroll realization | 🟠 Important |
| 6 | Payroll | Finance reports | 🟠 Important |
| 7 | Salary Structure | Payroll prerequisite | 🟠 Important |
| 8 | Users | User management UI | 🟡 Nice-to-have |
| 9 | Health Check | Monitoring | 🟡 Nice-to-have |

---

## Development Guidelines

### 🔄 When Adding a New API Service

1. **Create DTOs** in `AgriApp.Application/DTOs/`
   ```csharp
   public record CreateEquipmentRequest(string Name, string Category, decimal HourlyRate);
   public record EquipmentResponse(int Id, string Name, string Category, decimal HourlyRate);
   ```

2. **Create Service Interface & Implementation** in `AgriApp.Application/Services/`
   ```csharp
   public interface IEquipmentService
   {
       Task<List<EquipmentResponse>> GetAllAsync();
       Task<EquipmentResponse> CreateAsync(CreateEquipmentRequest request);
   }
   ```

3. **Implement Global Query Filter** in `AgriDbContext.OnModelCreating()`
   ```csharp
   modelBuilder.Entity<Equipment>()
       .HasQueryFilter(e => 
           _currentUser!.Role == Role.SuperUser 
           || e.CenterId == _currentUser.CenterId);
   ```

4. **Configure Precision** for currency fields:
   ```csharp
   modelBuilder.Entity<Equipment>()
       .Property(e => e.HourlyRate)
       .HasPrecision(18, 2);
   ```

5. **Create Repository** (if needed) in `AgriApp.Infrastructure/Repositories/`

6. **Register in DI** in `AgriApp.Api/Program.cs`:
   ```csharp
   builder.Services.AddScoped<EquipmentService>();
   ```

7. **Create Controller** in `AgriApp.Api/Controllers/`

8. **Create Blazor Service** in `AgriApp.Web/Services/`

9. **Create Razor Page/Component** in `AgriApp.Web/Pages/`

---

### 🔒 When Building Authorization

1. Use `[Authorize(Roles = "SuperUser,Manager")]` on controller actions
2. Leverage Global Query Filters (automatic in services)
3. **Never query directly**; always go through DbContext (filters apply automatically)
4. Test with different roles using JWT claims

---

### 💰 When Adding Currency Fields

**ALWAYS**:
1. Use `decimal` in C#
2. Use `NUMERIC(18,2)` in SQL
3. Configure via `HasPrecision(18, 2)` in DbContext
4. Use `decimal` arithmetic (no `float`/`double`)
5. Round with `MidpointRounding.AwayFromZero`

**Example**:
```csharp
decimal gst = baseAmount * 0.18m;
decimal total = Decimal.Round(baseAmount + gst, 2, MidpointRounding.AwayFromZero);
```

---

### 📱 When Adding Blazor Components

1. Use **MUD Blazor** for UI components (already installed)
2. Use `@inject IServiceInterface ServiceName` for dependency injection
3. Use `<AuthorizeView Roles="RoleName">` for conditional rendering
4. Use `@if (_loading) { <MudProgressCircular /> }` for async states
5. Handle errors with `<MudAlert Severity="Severity.Error">` messages

---

### 🧪 Testing Strategy

- **Unit Tests**: Test business logic in `AgriApp.Application`
- **Integration Tests**: Test DbContext + migrations
- **API Tests**: Use Swagger UI during development
- **Manual Tests**: Test different roles (SuperUser, Manager, Sales, Staff)

---

## Deployment

### Environment Variables

```bash
# PostgreSQL
DB_CONNECTION_STRING=User Id=postgres;Password=xxxx;Server=localhost;Port=5432;Database=agriapp;

# JWT
JWT:Key=your-secret-key-min-32-chars-long

# API URL (for Web project)
ApiBaseUrl=http://localhost:5000
```

### Database Migrations

```bash
# Apply migrations
dotnet ef database update --project src/AgriApp.Infrastructure

# Create new migration
dotnet ef migrations add MigrationName --project src/AgriApp.Infrastructure
```

### Running the Projects

**API (ASP.NET Core)**:
```bash
cd src/AgriApp.Api
dotnet run
# Swagger UI at http://localhost:5000/swagger
```

**Web (Blazor WASM)**:
```bash
cd src/AgriApp.Web
dotnet run
# App at http://localhost:5001
```

---

## Current Development Status

### 🟢 Complete & Tested
- ✅ Authentication (JWT)
- ✅ User management (basic)
- ✅ Equipment CRUD
- ✅ Inquiries CRUD
- ✅ Work Orders CRUD + double-booking validation
- ✅ Attendance with GPS
- ✅ Salary structures
- ✅ Invoice generation (18% GST formula)
- ✅ Payment recording (duplicate prevention)
- ✅ Commission realization (UPI webhook)
- ✅ Payroll calculations
- ✅ Audit trail (automatic)

### 🟠 In Progress
- 🟡 Web UI: Equipment component
- 🟡 Web UI: Inquiries component
- 🟡 Web UI: Invoices component
- 🟡 Web UI: Payments component
- 🟡 Web UI: Payroll reports component
- 🟡 Calendar service implementation

### 🔴 Not Started
- ❌ Integration tests
- ❌ E2E tests
- ❌ Performance optimization
- ❌ Caching strategy

---

## Quick Reference

### Key Files

| File | Purpose |
|------|---------|
| `src/AgriApp.Core/Entities/*.cs` | Domain models |
| `src/AgriApp.Infrastructure/Data/AgriDbContext.cs` | Global Query Filters |
| `src/AgriApp.Application/Services/*.cs` | Business logic |
| `src/AgriApp.Api/Controllers/*.cs` | HTTP endpoints |
| `src/AgriApp.Web/Services/*.cs` | Blazor service clients |
| `.github/copilot-instructions.md` | Team standards |

### Important Constants

- **GST Rate**: 18%
- **Decimal Precision**: (18, 2)
- **JWT Expiration**: 24 hours (configurable)
- **API Base URL**: `http://localhost:5000`
- **Web Base URL**: `http://localhost:5001`

### Critical Security Checklist

- [ ] All endpoints check `[Authorize]` attribute
- [ ] Global Query Filters applied to all center-scoped entities
- [ ] Salesperson privacy filter on Inquiries
- [ ] CenterId isolation on all write operations
- [ ] Password fields excluded from audit logs
- [ ] JWT secret from environment variable
- [ ] No hardcoded credentials

---

**Document Version**: 1.0.0  
**Last Modified**: 2025-03-28  
**Next Update**: When new major feature added
