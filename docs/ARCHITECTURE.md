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

## Production-readiness review

A full architecture pass was run over the solution. What it changed, and what
it deliberately left alone:

### Hardened

- **Fatal startup failures now exit non-zero.** The top-level handler logged
  and fell through, so the process still exited 0 — an orchestrator read a
  crash as a clean shutdown and neither restarted nor alerted.
- **Rate limiting on the guessable endpoints.** Sign-in and the password-reset
  pair are limited per client address. Refresh, logout and `/me` are
  deliberately *not*: a site usually reaches the API through one NAT address,
  so an office-wide limit would be spent on routine token refreshes. Refresh
  is already protected by 64-byte random tokens plus reuse detection.
- **Client address taken from forwarded headers.** Behind a proxy —
  i.e. every real deployment — `RemoteIpAddress` is the proxy, so every
  refresh-token audit row recorded the load balancer instead of the caller,
  silently degrading the reuse-detection trail. `UseForwardedHeaders` runs
  before anything reads the address, including the rate limiter's partition.
- **Exception middleware no longer rewrites a started response.** Setting the
  status code after the body has begun streaming throws a second exception on
  top of the first; it now logs and re-throws so the connection aborts
  cleanly. Problem details also carry a `traceId`, so a user-reported failure
  can be matched to a log entry.
- **Migrate-on-startup defaults to off.** Two replicas would race to migrate.
  Development still opts in explicitly.

### The error log now means something

Request logging was registered *inside* the exception middleware, so Serilog
saw every exception before it had been translated and recorded it as a 500 —
even when the client had correctly received a 400, 404 or 409. A duplicate
employee number, an ordinary not-found, a user navigating away mid-request:
all of them landed in the log as server faults. Nothing was broken from the
caller's side, which is why it survived this long, but it makes the error rate
useless as an alerting signal and buries real failures.

Three changes:

- Request logging now wraps the exception middleware, so it records the status
  the client actually received.
- A caller that hangs up is no longer an error. `OperationCanceledException`
  raised because `RequestAborted` fired is logged at information and answered
  with 499 (the established "client closed request" convention), keeping
  aborts out of the 5xx bucket. A cancellation the caller did *not* ask for —
  a timeout inside a handler — is still an error, and is covered by a test.
- The mediator pipeline draws the same distinction before the exception
  reaches the API.

Measured over a full end-to-end run — 308 requests, including every expected
400/401/403/404/409 and five client aborts — the API now logs **zero** error
lines. Before the change, all 40 non-2xx requests were logged as
`ERR … responded 500`.

### Layering

`AddInfrastructure` used to configure the ASP.NET Core bearer scheme, so a
non-web host could not compose the application without dragging in the web
authentication stack — which is exactly what the integration tests hit.
Validating an incoming `Authorization` header is a web-host concern and now
lives in the API (`AddJwtBearerAuthentication`); Infrastructure keeps only how
tokens are issued and how credentials are stored.

### Duplication removed

- Five list queries carried an identical copy of the paging bounds and the
  sort-field allow-list. They now derive from `PagedQueryValidator<T>` /
  `SortablePagedQueryValidator<T>`, which is also where the limits are stated
  once (‑65 lines).
- The admin app's five list pages repeated the same paging/sorting/search
  state, grid configuration and delete-confirmation flow. Extracted into
  `useListQueryState`, `ResourceDataGrid` and `useDeleteWithConfirm`
  (‑166 lines), with each page keeping its own columns and filter.
- All eight mobile repositories carried their own copy of the `DioException`
  → `ApiException` conversion, and five of them rebuilt the same paged query
  map. They now extend `ApiRepository` (555 → 397 lines against a 106-line
  shared base), which is also where the
  guarantee callers depend on — a repository only ever throws `ApiException`,
  never a transport type — is stated once. Because that refactor changed how
  every request is built, 17 tests now pin each repository's path, query map
  and body against a recording Dio adapter.

### Shared building blocks in the clients

A second pass removed the remaining copy-paste in both client apps. Each
abstraction below exists because the same code appeared in four or five places,
not because a layer seemed desirable:

| Where | What it replaced |
|---|---|
| `core/widgets/info_tile.dart` | Five byte-identical private `_InfoTile` widgets, one per detail screen. |
| `core/pagination/filtered_paged_list_notifier.dart` | The filter field, getter and reload logic repeated in all five list controllers. |
| `api/resource.ts` | The `get`/`create`/`update`/`remove` calls and the hand-written query-parameter block in all five resource modules. |
| `features/resourceQueries.ts` | The cache-key triple, the two query hooks, and the invalidate-on-success wiring repeated across twenty mutations. |

Two of these are worth explaining, because they encode a decision rather than
just saving lines.

**`listParams` states one rule about empty filters.** Every list endpoint reads
an absent parameter as "no filter" and applies its own default, so an empty
value must be dropped rather than sent. The rule is: drop `undefined`, `null`,
`''` and `false`; keep `0`. That last part matters — `maxQuantity: 0` means
"out of stock", which is a real filter. The old code expressed this by using
`||` on eight fields and `??` on one, which is the kind of distinction that
survives exactly as long as nobody copies the wrong line.

**`useResourceMutation` takes its invalidation keys explicitly.** It would have
been shorter to always invalidate the resource's own collection, but that is
wrong for assignment: adding an employee to a project also changes what the
projects endpoint returns. Naming the affected caches at each call site is what
keeps a screen from showing stale data after a successful write, so the
parameter is required rather than defaulted.

The resource modules also gained something the extraction did not aim for:
because `createCrudApi` forwards the whole typed query object, adding a filter
now means adding one field to an interface instead of editing an interface and
a parallel parameter list that could silently disagree with it.

### Known limits, not fixed

- **`location_records` grows without bound.** One ping per employee per minute
  is roughly a million rows a month for a hundred-person crew. The "last known
  position" query stays fast — it is a single lateral join on the
  `(EmployeeId, Timestamp DESC)` index — but history queries, backups and disk
  will degrade over a year. A retention window or monthly partitioning is the
  fix; it is a schema decision, not a code cleanup.
- **Search cannot use an index.** Every list filters with
  `LIKE '%term%'` over `lower(column)`, which forces a sequential scan. At the
  stated scale (hundreds of employees) this is irrelevant. If the data grows an
  order of magnitude, the answer is a `pg_trgm` GIN index rather than
  reworking the queries.
- **No account lockout.** Rate limiting slows guessing; it does not lock an
  account after repeated failures. Worth adding if the system faces the public
  internet rather than a company network.

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
  carries no mocking library and no dual-licensed assertion package. Also
  covers what the mediator pipeline puts in the error log, since that is what
  alerting reads.
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

The clients are not covered equally. The mobile app has a real suite
(`flutter test`); the admin app has only `tsc -b` and `oxlint` — its behaviour
has been verified end to end with a Playwright script run against a live API,
but that script is not committed and does not run in CI, so the admin panel is
currently unguarded against regressions. On the mobile side the suite
includes
repository tests that assert the exact path, query map and body each call puts
on the wire, using a recording `HttpClientAdapter` instead of a server — the
layer where a silently dropped filter would otherwise reach production
unnoticed. CI runs all of it on every push.

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
