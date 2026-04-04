# AgriApp.Web

Blazor **WebAssembly** standalone **PWA** — UI and API client only; domain types come from **`AgriApp.Core`** (project reference).

## MudBlazor UI standards

- **Shell:** **`MainLayout.razor`** registers **`MudThemeProvider`** (custom **`_agriTheme`** — green primary `#388e3c`, amber secondary), **`MudPopoverProvider`**, **`MudDialogProvider`**, **`MudSnackbarProvider`**.
- **Layout:** **`MudLayout`** / **`MudAppBar`** / **`MudDrawer`** / **`MudMainContent`** with **`MudContainer MaxWidth="MaxWidth.ExtraLarge"`** for page body.
- **Dialogs:** Use **`MudDialog`** with **`DialogContent`** / **`DialogActions`**. **Cancel** and **Close** actions must use **`ButtonType="ButtonType.Button"`** so they do not submit a parent **`EditForm`**. Submit actions use **`ButtonType="ButtonType.Submit"`** with **`form="..."`** linking to the form `id` when the button sits outside the form markup.

## “No-ID” UI rule (foreign keys)

- For any bound field that is a **foreign key / surrogate id** (property names like **`CustomerId`**, **`EquipmentId`**, **`VendorId`**, **`CenterId`**, or a **`Guid`** reference id such as **`InvoiceId`**): **do not** use **`MudTextField`** / **`MudNumericField`** for raw ids for user selection.
- **Do** inject the appropriate **`I…Service`**, load options in **`OnInitializedAsync`** (or equivalent), and bind with **`MudSelect`** or searchable **`MudAutocomplete`**, showing **human-readable labels** (e.g. name) while the model holds the **id**.
- **SuperUser-only** optional center override may still exist in some forms; prefer a center **picker** over a bare numeric id when exposing that choice.

## HTTP clients and services

- Register and resolve the named **`HttpClient` `"AgriApi"`** via **`IHttpClientFactory`**. All API access goes through **`AgriApp.Web.Services`** interface implementations — **no** raw **`HttpClient`** in `.razor` files.
- Paths must match **AgriApp.Api** routes exactly (e.g. **`api/workorders`**, not `api/work-orders`).

## `Services/ViewModels.cs`

- **All** frontend **request/response DTOs** used by the WASM client should be **`public record`** types in **`Services/ViewModels.cs`** (single file convention for this repo).
- Do **not** add UI-only DTOs to **`AgriApp.Core`**. Do **not** create a separate **`Models/`** folder for these unless the team explicitly changes policy.

## Exception shield (user-visible errors)

- Wrap service calls from components in **`try` / `catch`** where failures are expected.
- On failure, notify with **`ISnackbar.Add(message, Severity.Error)`** (or success/info as appropriate). Avoid silent failures and **`Console.WriteLine`** for user-relevant errors.
- Service implementations should log with **`ILogger<T>`** and return or throw in a way the UI can translate into snackbar messages.

## Namespaces

- Prefer **`AgriApp.Web.Pages`**, **`AgriApp.Web.Layout`**, **`AgriApp.Web.Shared`**, **`AgriApp.Web.Services`**, **`AgriApp.Web.Security`** to match existing structure.
