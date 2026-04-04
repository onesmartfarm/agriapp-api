# AgriApp.Api

## Controller routes (naming)

- Use **`[Route("api/...")]`** exactly as defined on each controller. **Clients must match these paths** (no guessed kebab-case).
- **Examples in this repo:**
  - Work orders: **`api/workorders`** (not `api/work-orders`).
  - Equipment: **`api/equipment`**.
  - Customers: **`api/customers`**, vendors: **`api/vendors`**, centers: **`api/centers`**, users: **`api/users`**, inquiries: **`api/inquiries`**, invoices: **`api/invoices`**, payments: **`api/payments`** / **`api/payment`**, attendance: **`api/attendance`**, calendar: **`api/calendar`**, auth: **`api/auth`**, payroll: **`api/payroll`**, salary structures: **`api/salary-structures`**.
- **Health:** **`GET api/healthz`** — anonymous.

## Tenant isolation: `AgriDbContext` global query filters

- **`AgriDbContext`** is constructed with optional **`ICurrentUser`**. Filters use it at query time.
- **Pattern (typical entity):** if `_currentUser == null` → no filter (e.g. migrations/design-time); if **`Role == SuperUser`** → no row filter; else **`CenterId == _currentUser.CenterId`**.
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
