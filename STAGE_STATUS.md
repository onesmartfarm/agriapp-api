# AgriApp — stage status (checkpoint)

Use this file to reset AI or human context on where the solution stands.

## Current stage

- **8.0 — Service–activity implementation**
  - **`ServiceActivity`** entity and API (`api/service-activities`), center-scoped catalog with **`BaseRatePerHour`**.
  - **Work orders** link **service activity**, **implement** (`ImplementId`, `IsImplement == true`), and **tractor** (`TractorId`, `IsImplement == false`).
  - **Invoices** use **Working** time logs × **service activity rate** when billable hours exist (see **AgriApp.Api** `README.md` for formula and implementation notes).
  - **Web:** `/services`, work order dialog updates, equipment **implement** flag, “no-ID” selects for service / customer / asset names.

## Completed (reference)

| Stage | Theme |
|-------|--------|
| **7** | Customer / vendor separation, CRM and financial entities aligned to centers |
| **7.5** | **Work order time log** persistence, billing timeline model, PostgreSQL CHECK on time logs |
| **7.7** | **Multi-currency** centers (`CurrencySymbol`, `TimeZoneId`), seed and UI adornment patterns |

## Pending / backlog (not exhaustive)

- **Fuel tracking** (consumption/cost tied to tractor or work orders).
- **Operational calendar wiring** (capacity UI vs API shape consistency, richer scheduling).
- **Sales commissions** (further productization / reporting beyond current ledger flows).

## Related docs

- **`src/AgriApp.Core/README.md`** — domain: service activity, equipment pairing, **`WorkOrderTimeLog`** / **`WorkTimeLogType`**.
- **`src/AgriApp.Api/README.md`** — routes, invoice math, DB constraints.
- **`src/AgriApp.Web/README.md`** — dialogs, currency, no-ID UI rules.

---

*Last updated: Stage 8.0 checkpoint (service activities + work order pairing + invoice labor from service rate).*
