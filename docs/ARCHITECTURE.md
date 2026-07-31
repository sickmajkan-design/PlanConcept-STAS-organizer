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
├── .github/workflows/ci.yml     # backend + both clients on every push
├── docs/ARCHITECTURE.md
├── src/
│   ├── Construction.Domain/
│   ├── Construction.Application/
│   ├── Construction.Infrastructure/
│   ├── Construction.API/
│   ├── construction_mobile/     # Flutter app
│   └── construction_admin/      # React admin
└── tests/
    ├── Construction.UnitTests/          # no database
    └── Construction.IntegrationTests/   # throwaway PostgreSQL database
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
| 7 | GPS Tracking | ✅ implemented & verified end-to-end (API) |
| 8 | Push Notifications | ✅ implemented & verified end-to-end (API) |

The full database schema ships in the initial migration so later modules add only
application/API/client code — no disruptive schema churn between modules.

All eight Phase 1 backend modules are complete, and both clients now cover
every Phase 1 module: Authentication, Employees, Projects, Vehicles, Tools
and Materials, plus GPS tracking and push notifications on mobile.

### Mobile client status (`construction_mobile`, Flutter)

| Area | Status |
|---|---|
| Foundation (network, secure session, routing, theme) | ✅ done |
| Authentication (sign in/out, refresh, change & forgot password) | ✅ done |
| Employees (list, search, filters, detail) | ✅ done |
| Projects (list, search, filters, detail with crew) | ✅ done |
| Vehicles (list, search, status filter, detail) | ✅ done |
| Tools (list, search, status filter, detail, QR lookup) | ✅ done |
| Materials (list, search, warehouse-only filter, detail) | ✅ done |
| GPS reporting (60 s interval, offline buffer) | ✅ done |
| Push notifications (FCM token, inbox, badge, deep links) | ✅ done |

The app mirrors the API's authorization model rather than discovering it
through errors: directory-gated routes are withheld from Workers, and
location reporting is only started for accounts linked to an employee. The
one deliberate exception is tool lookup by QR code, open to every employee
(mirroring the API's `AllEmployees` policy on that one endpoint) so a Worker
can still identify a tool on site. Vehicles/Tools/Materials are read-only on
mobile — CRUD and assignment management live in the admin app — reached from
a "Resources" section on the Home screen rather than the bottom-nav tabs, to
keep the tab bar to four items.

See [`src/construction_mobile/README.md`](../src/construction_mobile/README.md)
for its structure and the client-side auth behaviour.

### Admin client status (`construction_admin`, React)

| Area | Status |
|---|---|
| Foundation (Axios client, session handling, routing, theme, layout) | ✅ done |
| Authentication (sign in/out, refresh, change & forgot password) | ✅ done |
| Employees (paged grid, search, status filter, CRUD, project assignment) | ✅ done |
| Projects (paged grid, search, status filter, CRUD, crew display) | ✅ done |
| Vehicles (paged grid, search, status filter, CRUD, employee assignment) | ✅ done |
| Tools (paged grid, search, status filter, CRUD, dual employee + project assignment) | ✅ done |
| Materials (paged grid, search, warehouse-only filter, CRUD, stock adjustment) | ✅ done |
| Live map (Google Maps, project filter, 30 s refresh) | ✅ done |

Same authorization-mirroring approach as the mobile app: the navigation
drawer and route guards withhold the directory from a `Worker`, matching the
roles the API actually serves those endpoints to. Create/edit/delete and
assignment actions rely on the API's finer-grained role checks and surface a
refusal as the same error banner used everywhere else. Heavy pages (the data
grids, the map) are code-split with `React.lazy` so the initial bundle stays
lean.

See [`src/construction_admin/README.md`](../src/construction_admin/README.md)
for its structure and the client-side auth behaviour.

## Testing strategy

Split by what each layer can prove, so the fast suite stays fast and the slow
one earns its cost:

- **`tests/Construction.UnitTests`** — pure logic with no I/O: PBKDF2 hashing,
  JWT claim and expiry construction, token hashing, every module's
  FluentValidation rules, and the pagination arithmetic both clients page off.
  Uses plain xUnit assertions and a hand-written clock fake, so the suite
  carries no mocking library and no dual-licensed assertion package.
- **`tests/Construction.IntegrationTests`** — real handlers resolved from the
  same dependency graph the API builds, sent through MediatR so the validation
  and logging behaviours stay in the path, against a throwaway PostgreSQL
  database created and dropped per run.

  PostgreSQL rather than an in-memory provider is a deliberate cost: the
  properties worth pinning down are provider-specific. Filtered unique indexes
  are what let a deleted employee number be reused; `ExecuteUpdate` is not
  implemented by the in-memory provider at all; and the "concurrent
  withdrawals cannot oversell stock" test only means anything against a
  database that actually serialises writers. An in-memory suite would report
  green while production broke.

The clients keep their own suites (`flutter test`, plus `tsc -b`, `oxlint` and
a Playwright script for the admin app). CI runs all of it on every push.

## Push notification design (module 8)

- `IPushSender` is the transport port; `FcmPushSender` (FirebaseAdmin SDK)
  implements it, chunking sends at FCM's 500-message limit and reporting
  permanently invalid tokens. Without configured credentials
  (`Firebase:CredentialsPath` or `Firebase:CredentialsJson`), pushes are
  logged instead of sent so every flow stays testable locally.
- `INotificationService` persists an inbox row per recipient, then pushes to
  all their registered devices and prunes tokens FCM reports dead. It never
  throws — notification delivery must not break the business operation.
- Device tokens are registered per user (`POST /api/notifications/device-tokens`);
  a token seen on another account is re-assigned to the current login.
- Wired events: employee→project assignment (ProjectAssigned to the employee,
  EmployeeAssigned to foremen/PMs already on that crew), vehicle assignment
  (VehicleAssigned), tool hand-out (ToolAssigned), and admin-sent
  GeneralAnnouncement with optional role / project-crew audience filters.
