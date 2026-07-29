# Architecture — Construction Workforce Management System

A production-grade workforce management platform (inspired by STAS Organizer) for
construction companies with hundreds of employees. **Phase 1 only.**

## System overview

```
┌─────────────────┐     ┌─────────────────┐
│ Flutter mobile   │     │ React admin      │
│ construction_    │     │ construction_    │
│ mobile           │     │ admin            │
└────────┬─────────┘     └────────┬─────────┘
         │ HTTPS / JWT            │ HTTPS / JWT
         ▼                        ▼
┌──────────────────────────────────────────┐
│ Construction.API (ASP.NET Core 9)        │
│ Controllers → MediatR → Handlers         │
├──────────────────────────────────────────┤
│ Construction.Application (CQRS core)     │
├──────────────────────────────────────────┤
│ Construction.Infrastructure              │
│ EF Core / JWT / SMTP / (FCM in module 8) │
├──────────────────────────────────────────┤
│ Construction.Domain (entities, enums)    │
└────────────────────┬─────────────────────┘
                     ▼
               PostgreSQL 16
```

## Clean Architecture layers

Dependencies point strictly inwards: `API → Infrastructure → Application → Domain`.

### Construction.Domain
No dependencies at all. Contains:

- **Entities**: `User`, `RefreshToken`, `PasswordResetToken`, `Employee`, `Project`,
  `EmployeeProject` (explicit many-to-many join), `Vehicle`, `Tool`, `Material`,
  `LocationRecord`, `DeviceToken`, `Notification`
- **Enums**: `UserRole`, `EmployeeStatus`, `ProjectStatus`, `VehicleStatus`,
  `FuelType`, `ToolStatus`, `NotificationType`, `DevicePlatform`
- **Common**: `BaseEntity` (Guid id + audit timestamps), `ISoftDeletable`

### Construction.Application
The CQRS core. One folder per feature under `Features/`, each operation a vertical
slice: request record + FluentValidation validator + MediatR handler in one file.

- `Common/Interfaces` — ports implemented by outer layers (`IApplicationDbContext`,
  `IJwtProvider`, `IPasswordHasher`, `ICurrentUserService`, `IDateTimeProvider`,
  `IEmailSender`)
- `Common/Behaviours` — MediatR pipeline: unhandled-exception logging → request
  logging (with 500 ms slow-request warning) → validation
- `Common/Exceptions` — typed exceptions mapped to HTTP status codes by the API
- `Common/Models/PagedList<T>` — pagination envelope used by all list endpoints
- `Common/Security/TokenHasher` — SHA-256 hashing for opaque tokens
- `Features/Authentication` — Phase 1 module 1 (see below)

### Construction.Infrastructure
Implements the Application ports:

- **Persistence** — `ApplicationDbContext`, per-entity `IEntityTypeConfiguration`s,
  `AuditableEntityInterceptor` (CreatedAt/UpdatedAt), `SoftDeleteInterceptor`
  (converts deletes to `IsDeleted = true`), EF Core migrations, `DbInitializer`
  (migrate-on-startup + Super Admin seeding), design-time factory for the EF CLI
- **Authentication** — `JwtProvider` (HMAC-SHA256 JWTs), `PasswordHasher`
  (PBKDF2-HMAC-SHA256, 100k iterations, per-password salt, constant-time compare),
  `ResetLinkBuilder`
- **Email** — MailKit SMTP sender (logs instead of sending when unconfigured)

### Construction.API
Thin HTTP shell: controllers only build commands and delegate to MediatR.

- `ExceptionHandlingMiddleware` — application exceptions → RFC 7807 problem details
- `CurrentUserService` — JWT claims → `ICurrentUserService`
- `Authorization/Policies` — role-hierarchy policies (`SuperAdminOnly`,
  `AdminAndAbove`, `ProjectManagerAndAbove`, `ForemanAndAbove`, `AllEmployees`)
- Serilog request logging + console/file sinks, Swagger with JWT security scheme,
  health check at `/health`, CORS for the admin app, HSTS + HTTPS redirection in
  production

## Repository layout

```
/
├── Construction.slnx
├── Directory.Build.props        # net9.0, nullable, implicit usings for all projects
├── docker-compose.yml           # postgres + api
├── docs/ARCHITECTURE.md
└── src/
    ├── Construction.Domain/
    ├── Construction.Application/
    ├── Construction.Infrastructure/
    ├── Construction.API/
    ├── construction_mobile/     # Flutter app  (added with its first approved module)
    └── construction_admin/      # React admin  (added with its first approved module)
```

## Database design

PostgreSQL 16, snake_case table names, all timestamps `timestamptz` (UTC).

Key decisions:

- **Soft delete** on `employees`, `projects`, `vehicles`, `tools`, `materials` via
  `ISoftDeletable` + global query filters; unique indexes are filtered on
  `IsDeleted = false` so identifiers (employee number, registration number, VIN,
  serial number, QR code) can be reused after deletion.
- **`employee_projects`** join table with composite PK `(EmployeeId, ProjectId)` —
  employees belong to many projects and vice versa; the assignment row carries
  `AssignedAt` / `AssignedByUserId`.
- **`location_records`** is append-only with a `bigint` identity key and a composite
  index `(EmployeeId, Timestamp DESC)` sized for one GPS ping per employee per
  minute; "last known location" is a single index-backed `LIMIT 1`.
- **Tokens are stored hashed** (SHA-256) — a database leak exposes no usable
  refresh or reset tokens; both have unique hash indexes for O(log n) lookup.
- `materials.Quantity` is `numeric(18,3)` with a `>= 0` check constraint.
- `notifications.DataJson` is `jsonb` for deep-link payloads.

## Authentication design (module 1)

- **Login** — email + password (PBKDF2 verify) → 15-minute JWT access token +
  7-day opaque refresh token; lifetimes configurable.
- **Refresh rotation with reuse detection** — every refresh revokes the used token
  and issues a new one, recording `ReplacedByTokenHash`. Presenting an
  already-revoked token revokes *all* of that user's active tokens (stolen-token
  containment) and returns 401.
- **Logout** — revokes the presented refresh token; idempotent.
- **Change password** — verifies the current password, then revokes every active
  refresh token so other devices must re-authenticate.
- **Forgot/reset password** — single-use, 1-hour, hashed reset tokens; the endpoint
  always answers 202 so account existence cannot be probed; on reset all sessions
  are revoked.
- **Roles** — `SuperAdmin`, `Admin`, `ProjectManager`, `Foreman`, `Worker` carried
  as a JWT role claim; policy hierarchy defined once in `Policies.cs`.
- **Seeding** — on startup (when `Database:ApplyMigrationsOnStartup` is true)
  migrations are applied and, if no Super Admin exists, one is created from
  `Seed:SuperAdmin:*` configuration.

## Cross-cutting conventions

- Everything async end-to-end; `CancellationToken` flows from controller to database.
- Handlers never set audit columns or worry about soft deletes — interceptors do.
- Validation failures, missing entities, auth failures are typed exceptions; the
  middleware is the single place mapping them to HTTP responses.
- Enums serialize as strings in the API for readable payloads.
- No secrets in the repository: production values arrive via environment variables
  (`JwtSettings__SecretKey`, `ConnectionStrings__DefaultConnection`,
  `Seed__SuperAdmin__*`); `appsettings.Development.json` carries dev-only defaults.

## Phase 1 modules & status

| # | Module | Status |
|---|--------|--------|
| 1 | Authentication | ✅ implemented & verified end-to-end |
| 2 | Employees | ✅ implemented & verified end-to-end (API) |
| 3 | Projects | ✅ implemented & verified end-to-end (API) |
| 4 | Vehicles | ✅ implemented & verified end-to-end (API) |
| 5 | Tools | ✅ implemented & verified end-to-end (API) |
| 6 | Materials | ✅ implemented & verified end-to-end (API) |
| 7 | GPS Tracking | domain + schema ready; API/UI awaiting approval |
| 8 | Push Notifications | domain + schema ready; FCM integration awaiting approval |

The full database schema ships in the initial migration so later modules add only
application/API/client code — no disruptive schema churn between modules.
