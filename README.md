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
- Flutter 3.44+ for the mobile app (`src/construction_mobile`)
- Node.js 22+ for the admin web app (`src/construction_admin`)

## Run with Docker

```bash
cp .env.example .env      # required: compose has no built-in secrets
docker compose up --build
```

API: http://localhost:8080 — Swagger UI at `/swagger` (Development only).

Two health probes, answering different questions:

| Path | Question | What to do when it fails |
|---|---|---|
| `/health/live` | Is the process running? | Restart the container. |
| `/health/ready` | Can this instance serve traffic? | Take it out of the load balancer. |
| `/health` | Alias of readiness, kept for what already points at it. | |

Liveness deliberately runs no checks. A liveness probe that depends on the
database is a way of asking a third party for permission to stay alive: a
thirty-second failover would restart every replica, several times, during the
one incident when losing them all is least affordable.

`.env.example` carries local development values only, and signing in with them
gives you `admin@construction.local` / `Admin123!`. Compose deliberately has no
fallback for `JWT_SECRET_KEY`, `SUPERADMIN_PASSWORD` or `POSTGRES_PASSWORD` —
it fails to start rather than come up with a known signing key and a known
admin password. Generate real values per environment
(`openssl rand -base64 48`) and see [`docs/SECURITY.md`](docs/SECURITY.md)
before deploying anywhere shared.

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
| `Firebase__CredentialsPath` or `Firebase__CredentialsJson` | FCM service-account for push notifications |
| `ClientApp__PasswordResetUrl` | Admin-app page that completes password reset |
| `Cors__AllowedOrigins__0…` | Allowed browser origins, written `https://host` — no trailing slash. Validated on start. |
| `Auth__RefreshCookie__SameSite` | `Strict` (default), `Lax` or `None`. See below. |
| `Auth__RefreshCookie__Domain` | Blank scopes the cookie to the API host; set it to share across subdomains. |
| `Retention__LocationRecordDays` | Days of GPS history kept (default 180; `0` keeps everything) |
| `Retention__SentOutboxMessageDays` | Days a delivered email/push record is kept (default 14) |
| `Retention__AuditEntryDays` | Days an audit entry is kept. Default `0` = keep forever. |
| `Retention__TimeEntryCoordinateDays` | Days a shift's clock-in/out coordinates are kept. Default `0` = keep them with the shift. See docs/PRIVACY.md. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Where to send metrics and traces. Unset means none are exported. |

The retention defaults purge, so a deployment that sets none of the `Retention`
values still bounds its own growth. `Retention__LocationRecordDays` is the one
worth a decision: a minute-by-minute record of where an employee was is
personal data, and at one ping a minute per person the table grows by roughly a
million rows a month per hundred workers. Setting it to `0` keeps everything
and logs a warning at startup saying so.

The browser's refresh token is an `HttpOnly` cookie rather than anything in
`localStorage`, so no script on the page can read it. `SameSite=Strict` makes it
its own CSRF defence, and it works when the API and the admin panel share a
registrable domain — `api.example.com` and `admin.example.com` are same-site
even though they are different origins. **If they are on different domains, put
them on one**; the alternative is `SameSite=None`, which requires HTTPS,
re-opens CSRF, and is being blocked by browsers as a third-party cookie. The
mobile app is unaffected: it keeps using platform secure storage and receives
the token in the response body.

Note: AutoMapper ≥ 15 is dual-licensed (free for smaller organizations,
commercial license otherwise) — review licensing for your deployment, or pin
an earlier version and accept its security advisory. The pinned 15.1.3 is the
patched line.

## Mobile app

```bash
cd src/construction_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Implemented: authentication, the employee/project/vehicle/tool/material
directories (read-only, with a QR-code tool lookup open to every employee),
GPS reporting and push notifications. See
[`src/construction_mobile/README.md`](src/construction_mobile/README.md).

## Admin web app

```bash
cd src/construction_admin
npm install
cp .env.example .env
npm run dev
```

Implemented: authentication, the employee/project/vehicle/tool/material
directories (CRUD, search, filters, project assignment, tool dual
assignment, material stock adjustment), and a live map of employee
locations. See [`src/construction_admin/README.md`](src/construction_admin/README.md).

## Tests

```bash
dotnet test                                    # backend: unit + integration
cd src/construction_mobile && flutter test     # mobile
cd src/construction_admin && npm run build     # admin: type-check + bundle
```

| Suite | Covers |
|---|---|
| `tests/Construction.UnitTests` | Password hashing, JWT claims and expiry, token hashing, every module's validation rules, pagination arithmetic, and which failures reach the error log. No database. |
| `tests/Construction.IntegrationTests` | Real handlers over a throwaway PostgreSQL database: refresh-token rotation and reuse detection, soft delete with identifier reuse, atomic stock adjustment under concurrency, tool assignment. Also hosts the real API in-process and drives every endpoint over HTTP as each of the five roles, so the `[Authorize]` policies are asserted rather than assumed. |
| `src/construction_mobile/test` | Validators, error mapping, model parsing, the exact requests each repository puts on the wire, and widget tests driving the real router. |
| `src/construction_admin` | Vitest: the session and token-refresh machinery against a fake network, the route guards for every role, i18n plurals and dictionary parity, query-param normalisation, and the date arithmetic behind the schedule board and the cost report. Plus `tsc -b` type-checking and `oxlint`. |

> **Known gap.** The admin suite covers logic and guards, not whole screens.
> Its CRUD and assignment flows have been verified end to end with a Playwright
> script run against a live API, but that script is not in the repository and
> does not run in CI, so a regression in a form or a grid would still get
> through.

The integration tests need a reachable PostgreSQL server. They create and drop
their own database, so point them at one with `ConstructionTests__Postgres`
(default `Host=localhost;Port=5432;Username=postgres;Password=postgres`);
`docker compose up postgres` provides one.

CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs all of the
above on every push, with PostgreSQL 16 as a service container.

## EF Core migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> \
  --project src/Construction.Infrastructure \
  --startup-project src/Construction.Infrastructure \
  --output-dir Persistence/Migrations
```

## API versioning

Routes are `/api/v1/…`. The unversioned form (`/api/employees`) is kept as a
permanent alias for version 1 so nothing written before versioning breaks, and
the default is pinned — unversioned means v1, always. A version the server does
not have is refused rather than quietly served as v1.

The endpoint tables below list paths without the version prefix; both forms
work.

## API modules (Phase 1)

| Module | Endpoints |
|---|---|
| Authentication | `POST /api/auth/login`, `/refresh`, `/logout`, `/change-password`, `/forgot-password`, `/reset-password`, `GET /api/auth/me` |
| User accounts | `GET /api/users` (pagination, `search`, `role`, `isActive` filters, `sortBy`/`sortDescending`), `GET /api/users/{id}`, `POST /api/users`, `PUT /api/users/{id}`, `POST /api/users/{id}/deactivate` (offboarding: revokes sessions, reset links and device registrations), `POST /api/users/{id}/activate`, `POST /api/users/{id}/password`. Admin and above; a handler additionally refuses to act on an account senior to the caller or to grant a role the caller does not hold |
| Employees | `GET /api/employees` (pagination `pageNumber`/`pageSize`, `search`, `status`, `position`, `projectId` filters, `sortBy`/`sortDescending`), `GET /api/employees/{id}`, `POST /api/employees`, `PUT /api/employees/{id}`, `DELETE /api/employees/{id}` (soft), `POST`/`DELETE /api/employees/{id}/projects/{projectId}` |
| Projects | `GET /api/projects` (pagination, `search`, `status`, `client`, `employeeId` filters, `sortBy`/`sortDescending`), `GET /api/projects/{id}` (with employee roster), `POST /api/projects`, `PUT /api/projects/{id}`, `DELETE /api/projects/{id}` (soft, releases tool assignments) |
| Vehicles | `GET /api/vehicles` (pagination, `search`, `status`, `fuelType`, `assignedEmployeeId`, `unassigned` filters, `sortBy`/`sortDescending`), `GET /api/vehicles/{id}`, `POST /api/vehicles`, `PUT /api/vehicles/{id}`, `DELETE /api/vehicles/{id}` (soft), `POST /api/vehicles/{id}/assign/{employeeId}`, `POST /api/vehicles/{id}/unassign` |
| Tools | `GET /api/tools` (pagination, `search`, `status`, `category`, `assignedEmployeeId`, `assignedProjectId`, `unassigned` filters, `sortBy`/`sortDescending`), `GET /api/tools/{id}`, `GET /api/tools/by-qr/{qrCode}`, `POST /api/tools`, `PUT /api/tools/{id}`, `DELETE /api/tools/{id}` (soft), `POST /api/tools/{id}/assign-employee/{employeeId}`, `/unassign-employee`, `/assign-project/{projectId}`, `/unassign-project` |
| Materials | `GET /api/materials` (pagination, `search`, `projectId`, `warehouse`, `unassignedOnly`, `maxQuantity` filters, `sortBy`/`sortDescending`), `GET /api/materials/{id}`, `POST /api/materials`, `PUT /api/materials/{id}`, `POST /api/materials/{id}/adjust` (atomic relative stock movement), `DELETE /api/materials/{id}` (soft) |
| GPS Tracking | `POST /api/locations` (batched pings from the mobile app, identity from JWT), `GET /api/locations/current` (`projectId`, `maxAgeMinutes`, `includeInactive` filters), `GET /api/locations/employees/{id}/last`, `GET /api/locations/employees/{id}/history` (`from`/`to`, paged) |
| Push Notifications | `GET /api/notifications` (`unreadOnly`, paged), `GET /api/notifications/unread-count`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`, `POST /api/notifications/device-tokens`, `POST /api/notifications/device-tokens/unregister`, `POST /api/notifications/announce` (role/project audience filters) |
