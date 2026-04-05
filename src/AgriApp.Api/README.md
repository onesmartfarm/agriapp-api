# AgriApp.Api

## Controller routes (naming)

- Use **`[Route("api/...")]`** exactly as defined on each controller. **Clients must match these paths** (no guessed kebab-case).
- **Examples in this repo:**
  - Work orders: **`api/workorders`** — **no hyphen** (not `api/work-orders`).
  - Equipment: **`api/equipment`**.
  - Service activities: **`api/service-activities`** (hyphenated segment; plural resource).
  - Customers: **`api/customers`**, vendors: **`api/vendors`**, centers: **`api/centers`**, users: **`api/users`**, inquiries: **`api/inquiries`**, invoices: **`api/invoices`**, payments: **`api/payments`** / **`api/payment`**, attendance: **`api/attendance`**, calendar: **`api/calendar`**, auth: **`api/auth`**, payroll: **`api/payroll`**, salary structures: **`api/salary-structures`**.
- **Health:** **`GET api/healthz`** — anonymous.

## Invoice math (draft generation from completed work order)

**Canonical product rule (rental / service labor + materials + fees):**

- **Billable hours** = sum of **(EndTime − StartTime)** over **`WorkOrderTimeLog`** rows where **`LogType == Working`** only (`Break` and `Breakdown` are excluded).
- **Rental / service labor** = **billable hours × `ServiceActivity.BaseRatePerHour`** when the work order has a **`ServiceActivity`** and there is positive billable time.
- **Pre-GST base amount** should align with: **labor + `WorkOrder.TotalMaterialCost` + `AdditionalFees`** (from the generate request), then **GST** is applied via **`GstCalculator.Calculate(baseAmount)`** to produce **`BaseAmount`**, **`GstAmount`**, **`TotalAmount`** on the invoice.

**Implementation:** **`InvoiceService.GenerateInvoiceFromWorkOrderAsync`** applies the above: with **positive Working hours**, a **`ServiceActivity`** is **required**; pre-GST base = **`serviceLabor + TotalMaterialCost + AdditionalFees`**.

**Edge cases:**

- **No time logs** (or only non-Working / zero Working hours): pre-GST base is typically **`TotalMaterialCost + AdditionalFees`**.
- **Duplicate invoice:** one invoice per work order (enforced in service).

## PostgreSQL: `WorkOrderTimeLog` CHECK constraint

- Table **`work_order_time_logs`** has a **CHECK** constraint **`CK_WorkOrderTimeLogs_EndTimeAfterStartTime`** enforcing **`"EndTime" > "StartTime"`** (strict inequality — equal timestamps are rejected).
- EF maps this in **`AgriDbContext.ConfigureWorkOrderTimeLog`**. Inserts/updates that violate the constraint will fail at the database.
- API validation (DTOs / **`WorkOrderService`**) should reject **`EndTime <= StartTime`** per log row so clients get **`400`** instead of a raw DB error.

## Tenant isolation: `AgriDbContext` global query filters

- **`AgriDbContext`** is constructed with optional **`ICurrentUser`**. Filters use it at query time.
- **Pattern (typical entity):** if `_currentUser == null` → no filter (e.g. migrations/design-time); if **`Role == SuperUser`** → no row filter; else **`CenterId == _currentUser.CenterId`**.
- **`ServiceActivity`** follows the same **center-scoped** filter pattern as **`Customer`**, **`Vendor`**, **`Equipment`**, etc.
- **`Inquiry`** adds a **Sales** rule: same center **and** (unless SuperUser) **`SalespersonId == current user id`**.
- **`Center`** and **`User`** sets are **not** given the same global filter as tenant rows; **centers listing/detail** uses **controller logic** (e.g. non–SuperUser limited to their center).
- **Always use tracked/query paths that go through the context** so filters apply (avoid raw SQL that bypasses EF).

## JWT authorization

- **Authentication:** **`JwtBearerDefaults.AuthenticationScheme`** — validate issuer, audience, lifetime, signing key (`SESSION_SECRET` / `Jwt:Key`).
- **Pipeline:** `UseAuthentication()` then `UseAuthorization()` before `MapControllers()`.
- **Default for business controllers:** class-level **`[Authorize]`** — valid JWT required unless overridden.
- **Anonymous endpoints:** e.g. **`AuthController`** login/register-style actions with **`[AllowAnonymous]`**, **`HealthController`** health check.
- **Role checks:** method-level **`[Authorize(Roles = "...")]`** using **`AgriApp.Core.Enums.Role`** string names (`SuperUser`, `Manager`, `Staff`, `Sales`, `Supervisor`, etc.) — match token claims emitted at login.
- **Swagger:** Bearer scheme defined; send **`Authorization: Bearer {token}`** for protected endpoints.
