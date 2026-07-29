# Construction Workforce Management System

Production-grade workforce management platform for construction companies
(inspired by STAS Organizer). Backend: ASP.NET Core 9 + PostgreSQL, Clean
Architecture with CQRS. Clients (added module by module): Flutter mobile app
and React admin panel.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full architecture,
database design and module status.

## Prerequisites

- .NET 9 SDK (building with a newer SDK also works — projects target `net9.0`)
- PostgreSQL 16 (or Docker)

## Run with Docker

```bash
docker compose up --build
```

API: http://localhost:8080 — Swagger UI at `/swagger` (Development only),
health check at `/health`.

Default development credentials (Development environment only):
`admin@construction.local` / `Admin123!`

## Run locally

```bash
# start postgres (e.g. docker compose up postgres) then:
dotnet run --project src/Construction.API
```

The API applies EF Core migrations and seeds the Super Admin on startup
(`Database:ApplyMigrationsOnStartup`).

## Configuration

Production values are supplied via environment variables — no secrets live in
the repository:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars, validated on start) |
| `Seed__SuperAdmin__Email` / `Seed__SuperAdmin__Password` | Initial Super Admin (first boot only) |
| `EmailSettings__Host`, `__Port`, `__Username`, `__Password` | SMTP for password-reset emails |
| `ClientApp__PasswordResetUrl` | Admin-app page that completes password reset |
| `Cors__AllowedOrigins__0…` | Allowed browser origins |

Note: AutoMapper ≥ 15 is dual-licensed (free for smaller organizations,
commercial license otherwise) — review licensing for your deployment, or pin
an earlier version and accept its security advisory. The pinned 15.1.3 is the
patched line.

## EF Core migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> \
  --project src/Construction.Infrastructure \
  --startup-project src/Construction.Infrastructure \
  --output-dir Persistence/Migrations
```

## API modules (Phase 1)

| Module | Endpoints |
|---|---|
| Authentication | `POST /api/auth/login`, `/refresh`, `/logout`, `/change-password`, `/forgot-password`, `/reset-password`, `GET /api/auth/me` |
| Employees | `GET /api/employees` (pagination `pageNumber`/`pageSize`, `search`, `status`, `position`, `projectId` filters, `sortBy`/`sortDescending`), `GET /api/employees/{id}`, `POST /api/employees`, `PUT /api/employees/{id}`, `DELETE /api/employees/{id}` (soft), `POST`/`DELETE /api/employees/{id}/projects/{projectId}` |
| Projects | `GET /api/projects` (pagination, `search`, `status`, `client`, `employeeId` filters, `sortBy`/`sortDescending`), `GET /api/projects/{id}` (with employee roster), `POST /api/projects`, `PUT /api/projects/{id}`, `DELETE /api/projects/{id}` (soft, releases tool assignments) |
| Vehicles | `GET /api/vehicles` (pagination, `search`, `status`, `fuelType`, `assignedEmployeeId`, `unassigned` filters, `sortBy`/`sortDescending`), `GET /api/vehicles/{id}`, `POST /api/vehicles`, `PUT /api/vehicles/{id}`, `DELETE /api/vehicles/{id}` (soft), `POST /api/vehicles/{id}/assign/{employeeId}`, `POST /api/vehicles/{id}/unassign` |
| Tools, Materials, GPS Tracking, Push Notifications | schema shipped; endpoints arrive module by module |
