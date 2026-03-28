# Workspace

## Overview

pnpm workspace monorepo using TypeScript. Each package manages its own dependencies.

## Stack

- **Monorepo tool**: pnpm workspaces
- **Node.js version**: 24
- **Package manager**: pnpm
- **TypeScript version**: 5.9
- **API framework**: Express 5
- **Database**: PostgreSQL + Drizzle ORM
- **Authentication**: JWT (jsonwebtoken)
- **Validation**: Zod (`zod/v4`), `drizzle-zod`
- **API codegen**: Orval (from OpenAPI spec)
- **API docs**: Swagger UI (swagger-jsdoc + swagger-ui-express)
- **Build**: esbuild (ESM bundle)
- **Password hashing**: bcryptjs

## Structure

```text
artifacts-monorepo/
├── artifacts/              # Deployable applications
│   └── api-server/         # Express API server (Clean Architecture)
│       └── src/
│           ├── core/           # Enums (Role, WorkStatus), Interfaces (ICurrentUser)
│           ├── infrastructure/ # Query filters, Repositories
│           ├── application/    # Services (GST Calculator, Commission Rules), DTOs
│           ├── middlewares/    # JWT authentication & authorization
│           └── routes/         # API controllers (auth, equipment, inquiries, work-orders, users)
├── lib/                    # Shared libraries
│   ├── api-spec/           # OpenAPI spec + Orval codegen config
│   ├── api-client-react/   # Generated React Query hooks
│   ├── api-zod/            # Generated Zod schemas from OpenAPI
│   └── db/                 # Drizzle ORM schema + DB connection
│       └── src/schema/
│           ├── centers.ts      # Centers (multi-tenant silo)
│           ├── users.ts        # Users with role enum (SuperUser/Manager/Supervisor/Sales/Staff)
│           ├── equipment.ts    # Equipment (Tractor/Drone/BioCNG)
│           ├── inquiries.ts    # Customer inquiries with ownership
│           └── workOrders.ts   # Maintenance work orders
├── scripts/                # Utility scripts
│   └── src/seed.ts         # Database seed (SuperUser + Center + sample data)
├── .github/
│   └── copilot-instructions.md  # VS Code Copilot guidance
├── pnpm-workspace.yaml
├── tsconfig.base.json
├── tsconfig.json
└── package.json
```

## Agricultural Domain

### Clean Architecture Layers

1. **Core** (`artifacts/api-server/src/core/`): Enums (Role, WorkStatus, EquipmentCategory, InquiryStatus), Interfaces (ICurrentUser)
2. **Infrastructure** (`artifacts/api-server/src/infrastructure/`): Query filters (CenterId-based data silo), Repository implementations
3. **Application** (`artifacts/api-server/src/application/`): Services (GST Calculator with CGST/SGST, Commission Rules with tiered rates), DTOs
4. **API** (`artifacts/api-server/src/routes/`): Controllers with JWT middleware, role-based authorization, Swagger docs

### Security Model ("Confidence-Back")

- **CenterId Filter**: All queries for Equipment, Inquiries, and WorkOrders are automatically filtered by the user's CenterId
- **Ownership Privacy**: Sales users can ONLY access Inquiries where `salespersonId == currentUserId`
- **SuperUser Bypass**: SuperUser role ignores all CenterId and ownership filters

### Seeded Accounts

| Role      | Email               | Password       |
|-----------|---------------------|----------------|
| SuperUser | admin@agriapp.com   | SuperUser123!  |
| Manager   | rajesh@agriapp.com  | Manager123!    |
| Sales     | priya@agriapp.com   | Sales123!      |
| Staff     | amit@agriapp.com    | Staff123!      |

### API Endpoints

- `POST /api/auth/login` — Login (returns JWT)
- `POST /api/auth/register` — Register new user
- `GET /api/equipment` — List equipment (center-filtered)
- `POST /api/equipment` — Create equipment (Manager/SuperUser)
- `POST /api/equipment/:id/quote` — Rental quote with GST + commission
- `GET /api/inquiries` — List inquiries (ownership-filtered for Sales)
- `POST /api/inquiries` — Create inquiry
- `PATCH /api/inquiries/:id/status` — Update inquiry status
- `GET /api/work-orders` — List work orders (center-filtered)
- `POST /api/work-orders` — Create work order (Supervisor+)
- `PATCH /api/work-orders/:id/status` — Update work order status
- `GET /api/users` — List users (Manager/SuperUser)
- `GET /api/users/me` — Current user profile
- `GET /api/docs` — Swagger UI documentation
- `GET /api/healthz` — Health check

## TypeScript & Composite Projects

Every package extends `tsconfig.base.json` which sets `composite: true`. The root `tsconfig.json` lists all packages as project references. This means:

- **Always typecheck from the root** — run `pnpm run typecheck` (which runs `tsc --build --emitDeclarationOnly`). This builds the full dependency graph so that cross-package imports resolve correctly. Running `tsc` inside a single package will fail if its dependencies haven't been built yet.
- **`emitDeclarationOnly`** — we only emit `.d.ts` files during typecheck; actual JS bundling is handled by esbuild/tsx/vite...etc, not `tsc`.
- **Project references** — when package A depends on package B, A's `tsconfig.json` must list B in its `references` array. `tsc --build` uses this to determine build order and skip up-to-date packages.

## Root Scripts

- `pnpm run build` — runs `typecheck` first, then recursively runs `build` in all packages that define it
- `pnpm run typecheck` — runs `tsc --build --emitDeclarationOnly` using project references

## Packages

### `artifacts/api-server` (`@workspace/api-server`)

Express 5 API server with Clean Architecture. Routes live in `src/routes/` and use `@workspace/api-zod` for response validation and `@workspace/db` for persistence.

- Entry: `src/index.ts` — reads `PORT`, starts Express
- App setup: `src/app.ts` — mounts CORS, JSON parsing, Swagger UI at `/api/docs`, routes at `/api`
- Depends on: `@workspace/db`, `@workspace/api-zod`, jsonwebtoken, bcryptjs, swagger-ui-express, swagger-jsdoc

### `lib/db` (`@workspace/db`)

Database layer using Drizzle ORM with PostgreSQL. Exports a Drizzle client instance and schema models for Centers, Users, Equipment, Inquiries, and WorkOrders.

- `src/schema/centers.ts` — Centers table (multi-tenant silo)
- `src/schema/users.ts` — Users table with role enum
- `src/schema/equipment.ts` — Equipment with category enum
- `src/schema/inquiries.ts` — Inquiries with status and ownership
- `src/schema/workOrders.ts` — Work orders with status

Production migrations are handled by Replit when publishing. In development, we just use `pnpm --filter @workspace/db run push`.

### `scripts` (`@workspace/scripts`)

- `pnpm --filter @workspace/scripts run seed` — Seeds database with SuperUser, Center, and sample data
