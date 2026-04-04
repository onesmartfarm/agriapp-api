# AgriApp.Web — Architectural State Report

**Date**: 2025-03-28  
**Project**: AgriApp.Web (Blazor WASM)  
**Framework**: .NET 8  
**Status**: 🟠 Ready for next development batch

---

## 1. Project Structure

```
src/AgriApp.Web/
├── Pages/                          # Routable Razor components
│   ├── Login.razor                 ✅ Authentication UI
│   ├── Home.razor                  ✅ Dashboard
│   ├── WorkOrders.razor            ✅ Work orders list + CRUD UI
│   ├── WorkOrderDialog.razor       ✅ Modal dialog for create/edit
│   ├── Calendar.razor              ⚠️  UI exists, no service method
│   ├── Counter.razor               📦 Demo component
│   ├── Weather.razor               📦 Demo component
│
├── Services/                       # API wrapper services
│   ├── IAuthService.cs             ✅ Interface (Login, Logout)
│   ├── AuthService.cs              ✅ Implementation
│   ├── IWorkOrderService.cs        🟡 Interface (read-only)
│   ├── WorkOrderService.cs         🟡 Implementation (GetAll, GetById)
│   ├── IAttendanceService.cs       ✅ Interface (Clock in/out, history)
│   ├── AttendanceService.cs        ✅ Implementation
│   ├── ViewModels.cs               📦 Shared DTOs for web layer
│
├── Security/                       # JWT & Authorization
│   ├── JwtAuthenticationStateProvider.cs    # Token storage + Auth state
│   ├── JwtAuthorizationMessageHandler.cs    # HttpClient interceptor (Bearer token)
│
├── Layout/                         # Master layouts
│   ├── MainLayout.razor
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor
│   ├── NavMenu.razor.css
│
├── Shared/                         # Shared components
│   ├── RedirectToLogin.razor       # Protected route redirect
│
├── Resources/                      # Localization
│   ├── SharedResource.resx         # English
│   ├── SharedResource.mr.resx      # Marathi
│   ├── SharedResource.cs           # Generated resource class
│
├── wwwroot/                        # Static assets
│   ├── index.html                  # SPA entry point
│   ├── appsettings.json            # Configuration
│   ├── manifest.webmanifest        # PWA manifest
│   ├── service-worker.js           # Service worker
│   ├── service-worker.published.js
│   ├── css/app.css
│   ├── sample-data/weather.json
│
├── App.razor                       # Root component
├── _Imports.razor                  # Global imports
├── Program.cs                      # WASM host startup
├── Properties/launchSettings.json  # Launch configuration
└── AgriApp.Web.csproj             # Project file

```

---

## 2. Namespace Audit

### Service Layer Namespaces

All API wrapper services use the standard namespace:

```csharp
namespace AgriApp.Web.Services;
```

**Current Registered Services** (from `Program.cs`):

| Service Interface | Implementation | Namespace | Status |
|-------------------|----------------|-----------|--------|
| `IAuthService` | `AuthService` | `AgriApp.Web.Services` | ✅ Registered |
| `IWorkOrderService` | `WorkOrderService` | `AgriApp.Web.Services` | ✅ Registered |
| `IAttendanceService` | `AttendanceService` | `AgriApp.Web.Services` | ✅ Registered |

### Security Layer Namespaces

```csharp
namespace AgriApp.Web.Security;
```

- `JwtAuthenticationStateProvider` — Token persistence + auth state
- `JwtAuthorizationMessageHandler` — HttpClient middleware (injects Bearer token)

---

## 3. The Imports (_Imports.razor)

### Current Global Imports

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.JSInterop
@using MudBlazor
@using AgriApp.Web
@using AgriApp.Web.Layout
@using AgriApp.Web.Shared
@using AgriApp.Web.Services
@using AgriApp.Core.Enums
```

### Analysis

✅ **Present & Good**:
- HTTP client namespaces (`System.Net.Http*`)
- Blazor component namespaces
- Authorization support
- MUD Blazor components
- Local namespaces (Layout, Shared, Services)
- Core enum types from backend

⚠️ **Notable Absence** (intentional):
- `System.Globalization` — Set in Program.cs via `CultureInfo.DefaultThreadCurrentCulture`
- Forms validation namespaces — Available but not globally imported (individual pages can add)

---

## 4. Current Inventory

### ✅ Services (3 Fully Registered)

#### **1. AuthService** (Fully Integrated)

```csharp
Namespace: AgriApp.Web.Services
Interface:  IAuthService
Scope:      Scoped (DI registered)
Methods:
  - LoginAsync(email, password) → (Success, Error?)
  - LogoutAsync() → void
```

**File**: `src/AgriApp.Web/Services/AuthService.cs`  
**Dependencies**: `JwtAuthenticationStateProvider`, `HttpClient`  
**Status**: ✅ Complete

---

#### **2. WorkOrderService** (Partially Integrated)

```csharp
Namespace: AgriApp.Web.Services
Interface:  IWorkOrderService
Scope:      Scoped (DI registered)
Methods:
  - GetAllAsync() → List<WorkOrderListItem>
  - GetByIdAsync(id) → WorkOrderListItem?
Missing:
  - CreateAsync() — NOT IMPLEMENTED
  - UpdateStatusAsync() — NOT IMPLEMENTED
  - DeleteAsync() — NOT IMPLEMENTED
```

**File**: `src/AgriApp.Web/Services/WorkOrderService.cs`  
**Dependencies**: `HttpClient` ("AgriApi" named client)  
**API Endpoint**: `GET /api/workorders`, `GET /api/workorders/{id}`  
**Status**: 🟡 Read-only (list/details only)

---

#### **3. AttendanceService** (Fully Integrated)

```csharp
Namespace: AgriApp.Web.Services
Interface:  IAttendanceService
Scope:      Scoped (DI registered)
Methods:
  - ClockInAsync(lat?, lng?) → (Success, Error?)
  - ClockOutAsync(lat?, lng?) → (Success, Error?)
  - GetMyAttendanceAsync() → List<AttendanceResponse>
```

**File**: `src/AgriApp.Web/Services/AttendanceService.cs`  
**Dependencies**: `HttpClient` ("AgriApi" named client)  
**API Endpoints**: `POST /api/attendance/clock`, `GET /api/attendance/my`  
**Status**: ✅ Complete (no UI component yet)

---

### 📦 Supporting DTOs (ViewModels.cs)

```csharp
Namespace: AgriApp.Web.Services

Records:
  ✅ LoginRequest(Email, Password)
  ✅ LoginResponse(Token, Email, Role, CenterId?)
  ✅ WorkOrderListItem(Id, Description, Status, ScheduledStartDate, ScheduledEndDate, EquipmentName?, TotalMaterialCost)
  ✅ AttendanceClockRequest(Action, Latitude?, Longitude?)
  ✅ AttendanceResponse(Id, Action, Timestamp, Latitude?, Longitude?)
```

---

### 📄 Pages (7 Total)

#### **Public Pages**

| Page | Route | Status | UI Framework | Purpose |
|------|-------|--------|--------------|---------|
| `Login.razor` | `/login` | ✅ Complete | MUD Blazor | Authentication form |

#### **Protected Pages** (require `[Authorize]`)

| Page | Route | Status | UI Framework | Completeness |
|------|-------|--------|--------------|--------------|
| `Home.razor` | `/` | ✅ Complete | MUD Blazor | Dashboard |
| `WorkOrders.razor` | `/workorders` | ✅ Complete | MUD Blazor DataGrid | List + CRUD UI (backend methods missing) |
| `WorkOrderDialog.razor` | (embedded modal) | ✅ Complete | MUD Blazor Dialog | Create/Edit modal |
| `Calendar.razor` | `/calendar` | ⚠️ Partial | MUD Blazor SimpleTable | UI ready, service method missing |

#### **Demo Pages**

| Page | Route | Status | Purpose |
|------|-------|--------|---------|
| `Counter.razor` | `/counter` | 📦 Demo | Increment counter |
| `Weather.razor` | `/weather` | 📦 Demo | Fetch sample JSON data |

---

### 🔐 Security Components (2 Total)

| Component | Purpose | Status |
|-----------|---------|--------|
| `JwtAuthenticationStateProvider` | Manages JWT token + `AuthenticationState` | ✅ Implemented |
| `JwtAuthorizationMessageHandler` | HttpClient middleware (attaches Bearer token) | ✅ Implemented |

---

### 📐 Layout & Shared Components (3 Total)

| Component | Purpose | Status |
|-----------|---------|--------|
| `MainLayout.razor` | Master layout with NavMenu + content area | ✅ Implemented |
| `NavMenu.razor` | Navigation sidebar | ✅ Implemented |
| `RedirectToLogin.razor` | Protected route redirect for unauthenticated users | ✅ Implemented |

---

## 5. Dependency Injection Container

### Registered in Program.cs

```csharp
// ── Core Framework ──
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddAuthorizationCore();

// ── Authentication ──
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// ── HTTP & Message Handlers ──
builder.Services.AddTransient<JwtAuthorizationMessageHandler>();
builder.Services.AddHttpClient("AgriApi", client =>
    client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

// ── API Service Wrappers ──
builder.Services.AddScoped<IAuthService>(sp => 
    new AuthService(factory.CreateClient("AgriApi"), authProvider));
builder.Services.AddScoped<IWorkOrderService>(sp => 
    new WorkOrderService(factory.CreateClient("AgriApi")));
builder.Services.AddScoped<IAttendanceService>(sp => 
    new AttendanceService(factory.CreateClient("AgriApi")));
```

---

## 6. Configuration & Build

### appsettings.json

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

- **ApiBaseUrl**: Backend API base URL
- Used in `Program.cs` to configure named HttpClient `"AgriApi"`

### Launch Settings

Configured in `Properties/launchSettings.json`:
- **URL**: `https://localhost:5001`
- **Browser**: Opens on launch

---

## 7. Architectural Patterns Used

### ✅ Service Abstraction Pattern

All API calls go through **interface-based services**:
- Component → `IServiceInterface` → `ServiceImplementation` → `HttpClient`
- Enables **unit testing** and **loose coupling**

### ✅ Dependency Injection

- **Scoped services**: Auth services, API wrappers
- **Transient handlers**: Message handlers
- **Factory pattern**: `IHttpClientFactory` for named clients

### ✅ JWT Token Management

- **Storage**: `Blazored.LocalStorage` (browser LocalStorage)
- **State**: `AuthenticationStateProvider` (Blazor's auth system)
- **Injection**: `JwtAuthorizationMessageHandler` (automatic Bearer header)

### ✅ Role-Based Authorization

- Uses `[Authorize(Roles = "...")]` attributes
- Respects backend role enum: `SuperUser | Manager | Sales | Staff`

---

## 8. Ready-to-Extend Areas

### 🟡 Need Implementation

These services have interfaces but incomplete or missing implementations:

1. **IEquipmentService** — NOT CREATED YET
   - Purpose: Equipment CRUD + quotes
   - Priority: 🔴 CRITICAL
   - Component needed: `EquipmentService.cs`

2. **IInquiryService** — NOT CREATED YET
   - Purpose: Inquiry CRUD + status transitions
   - Priority: 🔴 CRITICAL
   - Component needed: `InquiryService.cs`

3. **IInvoiceService** — NOT CREATED YET
   - Purpose: Invoice generation + payment tracking
   - Priority: 🔴 CRITICAL
   - Component needed: `InvoiceService.cs`

4. **IPaymentService** — NOT CREATED YET
   - Purpose: Record payments against invoices
   - Priority: 🔴 CRITICAL
   - Component needed: `PaymentService.cs`

5. **ICalendarService** — NOT CREATED YET
   - Purpose: Fetch capacity/scheduling data
   - Priority: 🟠 IMPORTANT
   - Component needed: `CalendarService.cs`
   - Note: `Calendar.razor` page already exists with UI

6. **IPayrollService** — NOT CREATED YET
   - Purpose: Fetch payroll reports
   - Priority: 🟠 IMPORTANT
   - Component needed: `PayrollService.cs`

### 📝 Next Service Template

```csharp
// File: src/AgriApp.Web/Services/IEquipmentService.cs
namespace AgriApp.Web.Services;

public interface IEquipmentService
{
    Task<List<EquipmentResponse>> GetAllAsync();
    Task<EquipmentResponse?> GetByIdAsync(int id);
    Task<EquipmentResponse> CreateAsync(CreateEquipmentRequest request);
    Task<EquipmentResponse?> UpdateAsync(int id, UpdateEquipmentRequest request);
    Task<bool> DeleteAsync(int id);
    Task<QuoteResponse> GetQuoteAsync(int id, RentalQuoteRequest request);
}

// File: src/AgriApp.Web/Services/EquipmentService.cs
using System.Net.Http.Json;

namespace AgriApp.Web.Services;

public class EquipmentService : IEquipmentService
{
    private readonly HttpClient _http;

    public EquipmentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<EquipmentResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<EquipmentResponse>>("api/equipment")
                   ?? new List<EquipmentResponse>();
        }
        catch
        {
            return new List<EquipmentResponse>();
        }
    }

    // ... implement remaining methods
}

// Update ViewModels.cs with DTOs
public record EquipmentResponse(int Id, string Name, string Category, decimal HourlyRate);
public record CreateEquipmentRequest(string Name, string Category, decimal HourlyRate);
// etc.
```

---

## 9. Synchronization Checklist ✓

Before next development batch:

- [x] Project structure documented
- [x] Namespaces verified (`AgriApp.Web.Services`, `AgriApp.Web.Security`)
- [x] Global imports reviewed (`_Imports.razor`)
- [x] Service inventory mapped (3 registered, 6 missing)
- [x] Page inventory listed (7 pages, 2 partial)
- [x] DI container examined (named client, message handler)
- [x] Configuration reviewed (ApiBaseUrl in appsettings.json)
- [x] Architectural patterns identified
- [x] Extension points documented

---

## 10. Summary Table

| Category | Status | Count | Notes |
|----------|--------|-------|-------|
| **Services Registered** | ✅ Complete | 3 | Auth, WorkOrder (read), Attendance |
| **Services Needed** | 🔴 Blocking | 6 | Equipment, Inquiry, Invoice, Payment, Calendar, Payroll |
| **Pages Implemented** | ✅ Complete | 4 | Login, Home, WorkOrders, Calendar |
| **Pages with Issues** | ⚠️ Partial | 1 | WorkOrders (CRUD UI ready, service incomplete) |
| **Demo Pages** | 📦 Extra | 2 | Counter, Weather |
| **Global Imports** | ✅ Adequate | 10 | Core + MUD + Services + Enums |
| **Layouts & Shared** | ✅ Complete | 3 | MainLayout, NavMenu, RedirectToLogin |
| **Security Components** | ✅ Complete | 2 | JWT auth + message handler |

---

**Report Generated**: 2025-03-28  
**Architecture Sync Status**: 🟢 READY FOR NEXT BATCH  
**Recommended Next Step**: Create `IEquipmentService` + DTOs
