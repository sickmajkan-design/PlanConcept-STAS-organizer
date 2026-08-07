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
  `LocationRecord`, `DeviceToken`, `Notification`, `OutboxMessage`
- **Enums**: `UserRole`, `EmployeeStatus`, `ProjectStatus`, `VehicleStatus`,
  `FuelType`, `ToolStatus`, `NotificationType`, `DevicePlatform`,
  `OutboxMessageType`
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
  liveness and readiness probes (`/health/live`, `/health/ready`), CORS for the
  admin app, HSTS + HTTPS redirection in production

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
- **`employee_projects`** is a dated posting rather than a bare membership: a
  surrogate `Id` plus `StartDate` / `EndDate` (null being open-ended), carrying
  `AssignedAt` / `AssignedByUserId`. It began life with a composite PK
  `(EmployeeId, ProjectId)`, which made "the same person back on that site next
  month" unrepresentable. Overlap is refused by an exclusion constraint,
  `EXCLUDE USING gist ("EmployeeId" =, "ProjectId" =, daterange(...) &&)`,
  scoped per project on purpose — one person covering two sites at once is
  real, and forbidding it would make the schedule board lie about where people
  are.
- **`absences`** carries the same shape with a status. Only *approved* leave is
  protected from overlap, by a partial exclusion constraint
  (`WHERE Status = Approved AND IsDeleted = false`): two people may ask for the
  same days, because a request is a question, but nobody can be granted leave
  twice over. Both constraints need the `btree_gist` extension, created by the
  migration.
- **`employee_rates`**, **`material_movements`** and **`vehicle_expenses`** carry
  the money. Rates are dated with the same exclusion constraint as the postings,
  because a cost report is about the past: a single current-rate column on the
  employee would rewrite every report ever run whenever anyone's pay changed.
  Movements keep `materials.Quantity` as a cache of their own sum — the stock
  screen reads it on every page, and summing the history to draw a list would be
  paid for every time — so the movement and the update are written in one
  transaction. `vehicle_expenses` puts fuel, servicing, insurance and
  registration in one table; they differ in two nullable fields and the question
  being asked adds them together anyway.
- **Two check constraints here are `CASE` expressions, not `OR` chains.** The
  obvious form, `("Kind" = 1 AND "Litres" > 0) OR ("Kind" <> 1 AND "Litres" IS
  NULL)`, evaluates to NULL when `Litres` is NULL, and a CHECK only rejects on
  FALSE — so it let a fill-up through with no litres at all. Both were caught by
  executing them, not by reading them.
- **`location_records`** is append-only with a `bigint` identity key and a composite
  index `(EmployeeId, Timestamp DESC)` sized for one GPS ping per employee per
  minute; "last known location" is a single index-backed `LIMIT 1`.
- **Tokens are stored hashed** (SHA-256) — a database leak exposes no usable
  refresh or reset tokens; both have unique hash indexes for O(log n) lookup.
- `materials.Quantity` is `numeric(18,3)` with a `>= 0` check constraint.
- `notifications.DataJson` is `jsonb` for deep-link payloads.
- **`outbox_messages`** carries queued email and push. Its due index is
  filtered on `SentAt IS NULL AND AbandonedAt IS NULL`, so a table that has
  delivered a million messages and owes nothing has an empty index to scan. A
  check constraint refuses a row that is both sent and abandoned — the two are
  set by different paths, and a row carrying both would leave nobody able to
  say what happened to it.

## A half-built feature, and how it stayed hidden

Work-item attachments — the photograph of the defect — were described as done
and were not reachable at all. The entity had `WorkItemId`, the migration
created the column with a foreign key, the check constraint counted it as a
fifth owner, both clients rendered an upload control, and `WorkItemDto` carried
an attachment count. But `AttachmentOwnerType` stopped at four values, so
nothing could name the owner, and three places behind it had no case for it
either.

Two of those three failed *silently in the wrong direction* rather than
loudly:

- the list query's owner switch ended in `_ => query.Where(a => a.ToolId == ...)`,
  so asking for a work item's files returned a tool's;
- the DTO's projection chain ended at `s.ToolId!.Value`, so a row owned by
  anything else reported itself as a tool.

Both were catch-alls written when there were four owners and never revisited
when a fifth arrived. The lesson taken: a `switch` over a closed set that ends
in `_` will not tell you when the set grows. Both are now exhaustive and throw
on an unknown value, which is what turns the next addition into a failure
instead of a wrong answer.

The consequence on the phone was worse than a missing screen. `uploadPhoto`
hardcoded `ownerType: 'Project'` and its only button sat on the project detail
screen, which is Foreman-and-above — so no worker could upload any photograph
anywhere, even though `AttachmentRules` carried a deliberate, commented
carve-out permitting exactly that.

## Spreadsheet exports

The Application layer builds a `Spreadsheet` — sheets, typed columns, rows —
and Infrastructure renders it with ClosedXML. The port keeps the library out of
the feature handlers, which is what lets the exports be tested by reading the
bytes back rather than by trusting a length.

`.xlsx` rather than CSV, and the reason is the audience rather than taste.
Excel in a Serbian locale expects a semicolon delimiter and reads a
comma-delimited file as one column per row; it also opens a UTF-8 file as
Windows-1250 unless it finds a byte-order mark, which turns every š and ć into
mojibake. A workbook has neither problem, and it carries number formats — a
duration column written as `[h]:mm` sums past 24 hours instead of wrapping back
round to zero, which a text column cannot do at all.

Headings are localised, uniquely in this API. Everywhere else English is
defensible because the clients translate what they display; an export leaves
the system and is opened by someone who never sees the app, so nothing
downstream can translate it. The language is a query parameter rather than
`Accept-Language`, because the file outlives the request that produced it.

The cost exports are built by sending the report queries, not by repeating
their arithmetic — so a foreman's export withholds exactly what their screen
does, and there is no second place for the rule to drift.

`Content-Disposition` is named in `WithExposedHeaders`. Cross-origin JavaScript
cannot read a response header that is not, and without it every download would
arrive with a generic name — visible only once the API and the panel are on
different origins, which is production and not development.

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

### Account administration

`/api/users` owns the account lifecycle. Two decisions in it are worth stating,
because both are security properties rather than conveniences.

**Deactivation is a revocation, not a flag.** Setting `IsActive = false` alone
would leave a refresh token working for its remaining seven days and a device
registration delivering push indefinitely, because push goes to a device rather
than through an access check. The handler therefore also revokes every active
refresh token, marks outstanding password-reset tokens used, and deletes the
device registrations. An access token already issued still lives out its
15 minutes — inherent to stateless JWTs, and documented rather than papered
over.

**Rank, not just role, decides who may act.** The controller policy admits
Admin and above; `RoleAdministration` then allows acting only strictly below
your own role, with Super Admin able to act on peers so that a compromised
Super Admin can still be removed. Without the second check any Admin could mint
another Admin — or a Super Admin — and the role hierarchy would be decorative.
Two lockout protections sit alongside it: nobody may deactivate their own
account, and the last active Super Admin may be neither deactivated nor
demoted.

The admin panel mirrors these rules to disable buttons it knows will fail, but
the API enforces them regardless of what the UI does.

### Bilingual clients

Both clients ship Serbian (Latin, ekavian) and English and default to Serbian:
an unknown device language in this region is far likelier to be a neighbouring
one than to mean the person reads English. Neither app took a third-party
package — the admin panel uses a typed dictionary with `Intl.PluralRules`, the
mobile app Flutter's own gen-l10n.

The dictionaries are typed so a missing translation is a build failure rather
than English appearing in front of a customer: `en.ts` is the source of keys
and `sr.ts` is a complete `Record<MessageKey, Message>`.

Translating exposed a modelling gap that English had concealed. `StatusChip`
took a bare status value, but the same API value inflects differently per
entity in Serbian — a vehicle is "slobodno", a tool "slobodan", while English
says "Available" for both. Both apps now require the enum *kind* alongside the
value. The compiler located every call site.

The mobile controllers hold an `AppMessage` rather than a finished sentence,
because they have no `BuildContext` and cannot translate. Resolving in the
widget also means a message already in state re-reads correctly when the
language changes instead of keeping the language it was created in.

Text the **API** produces — validation details, conflict messages — is still
English wherever it reaches the screen. Translating it means an
`Accept-Language` contract on the API, which is a separate decision. The same
holds for the admin panel's own client-side form messages: all nine `zod`
schemas carry English strings, so a rejected field reads in English even with
the interface in Serbian. Both are the same missing decision rather than two
problems, and neither is specific to a module.

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
- **No drag-and-drop on the schedule board.** The board shows and filters a
  week; postings are changed from the employee screen. This is the competitor's
  advertised feature, but it changes how a change is made rather than what the
  system knows.

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
| Notifications (inbox, unread badge, announcements) | ✅ done |

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

  Those tests reach handlers through MediatR with the current user set
  directly, which proves what a handler does but never that it was allowed to
  run. So the same project also hosts the real API in-process
  (`ApiFixture`/`ApiAuthorizationTests`) and drives every endpoint over HTTP
  with a real bearer token for each of the five roles. Refused roles must get
  403, admitted roles must not be refused, anonymous must get 401 — a table of
  around ninety endpoints, kept affordable by the fact that authorization
  answers before validation does, so no request needs a valid body or a real
  id. Without it, an action that lost its `[Authorize]` attribute in a merge
  would ship green.

  A few tests are about startup itself rather than about a running
  application — an unreachable database, a malformed CORS origin — so they host
  their own copy of the API instead of sharing `ApiFixture`. They sit in the
  `standalone-host` collection with parallelization disabled, because
  `WebApplicationFactory` catches a top-level-statements `Program` through a
  process-wide diagnostic listener: two of them building at once can each pick
  up the other's host, which surfaces as "The entry point exited without ever
  building an IHost" in whichever one lost, with nothing wrong in the code under
  test.

Both clients now have suites, drawn along the same line: the layer where a
mistake is silent.

- **`src/construction_admin/src`** (Vitest, alongside the code) — the session
  and token-refresh machinery driven through a fake axios adapter, the route
  guards for every role, i18n plural selection and dictionary parity, query
  parameter normalisation, and the date arithmetic behind the schedule board
  and the cost report. The refresh tests earn their keep: the API rotates
  refresh tokens and treats a replayed one as theft, so a refresh that stopped
  being single-flight would sign the operator out mid-task, and nothing about
  that shows up in a type check. Files that need a DOM say so with a
  `@vitest-environment jsdom` docblock; the rest run without one.

  Not covered: whole screens. The CRUD forms and grids are still only
  exercised by an uncommitted Playwright script, which is the remaining half
  of audit item H1.

- **`src/construction_mobile/test`** (`flutter test`) — validators, error
  mapping, model parsing, widget tests driving the real router, and repository
  tests that assert the exact path, query map and body each call puts on the
  wire, using a recording `HttpClientAdapter` instead of a server — the layer
  where a silently dropped filter would otherwise reach production unnoticed.

CI runs all of it on every push.

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
- Both clients read the same inbox. The phone is woken by FCM; the browser has
  no such channel, so the panel's badge polls `unread-count` once a minute and
  on window focus. One integer a minute is cheaper than the alternative — a
  count that only moves on a page reload is a badge nobody trusts.
- `POST /api/notifications/announce` is called from the panel's inbox screen,
  gated to Admin and above to match the endpoint's policy. It answers with the
  recipient count, which the screen shows: an announcement whose audience
  filter matched nobody is otherwise indistinguishable from one that reached
  the whole company.

## Recurring jobs

Two `BackgroundService` timers in the API, and deliberately no job framework:
the product has two recurring jobs, and a scheduler would bring a schema, a
dashboard and an operational surface that together outweigh the jobs it runs.
Both are safe on every replica, which is the property that makes the simple
option viable.

- **`DailyReminderService`** — documents about to lapse, work about to fall
  due. Each sweep claims a row with a conditional update before notifying, so
  a second instance finds nothing left to claim rather than telling anyone
  twice.

- **`OutboxService`** — sends what the request path queued, every ten seconds.
  See below.

- **`DataRetentionService`** — deletes what the system has finished with, every
  six hours. Nothing to claim: a deleted row cannot be deleted again, so
  concurrent sweeps either take disjoint sets or find the rows already gone.

  Each batch is one `DELETE` bounded by `LIMIT` (`Take` before
  `ExecuteDelete`), capped at twenty per table per sweep. The bound is the
  whole design. An unbounded delete over a year of GPS pings would hold locks
  and a transaction open for minutes, block autovacuum on the table it was
  bloating, and roll everything back if interrupted; batched, an interrupted
  sweep has still made progress and the next one carries on. That `Take`
  actually reaches PostgreSQL as a `LIMIT` rather than being ignored is
  asserted by a test, because the failure would be silent and total.

  Delivered outbox messages are purged after a fortnight. Abandoned ones never
  are: each is a delivery that failed for good, and it is the only thing that
  can answer "why did they never get the email?".

  Retention windows come from the `Retention` configuration section.
  `LocationRecordDays` defaults to 180; setting it to 0 keeps everything and
  logs a warning at startup saying so. Refresh tokens are kept for a grace
  period *past their own expiry* rather than deleted when revoked — rotation
  leaves the old row behind on purpose, because presenting it again is how a
  stolen token is detected, and deleting it would turn that signal into an
  ordinary unknown token.

## The outbox

Email and push used to be sent inside the request that caused them.
`ForgotPasswordCommand` waited on SMTP — MailKit's default timeout is two
minutes — on an endpoint anyone can call without authenticating, and a mail
server that was down lost the email while keeping the reset token, leaving
somebody waiting for a link nobody was going to send. Push had the same shape:
an FCM round trip inside "assign this employee to that site", with no retry.

Both now write a row to `outbox_messages` instead, and `OutboxService` sends
it.

**Enqueuing joins the caller's unit of work.** `IOutbox.Enqueue` is
synchronous and does not save — it adds to the same change tracker the handler
is already using, so the message commits in the caller's own transaction. The
reset token and the email carrying it are one write. A handler that throws
before saving queues nothing, which is right: the thing the message was about
did not happen either.

**Claiming is what makes replicas safe.** One `UPDATE` stamps a claim token,
increments the attempt count and pushes `NextAttemptAt` a lease beyond now; the
worker then reads its rows back by that token. A second worker starting in the
middle re-checks its predicate after taking the row lock, finds the message no
longer due, and takes nothing. That is asserted by a test that runs the second
sweep *from inside the first one's send*, because two sweeps started together
may never overlap — an earlier version of the test passed against a broken
claim for exactly that reason.

`NextAttemptAt` doubling as the lease means a worker that dies mid-send strands
nothing: the message becomes due again by itself. And the attempt count is
incremented when claimed rather than after a failure, so a message that kills
the process still counts towards its limit instead of being retried for ever.

**Failure backs off and then stops.** Half a minute, doubling, six attempts —
roughly half an hour, long enough to outlast a mail server restart and short
enough that a permanently wrong address is not retried indefinitely. After that
the message is abandoned, with the last error on the row.

The payload is `jsonb` rather than columns because an email and a push share
no fields, and a table with both sets half-null has to be read with a rule in
mind. The cost is that string operations on it need a cast — which only tests
ever want, since the processor selects on the columns beside it.

## Observability

Three things, of which only the first two are code.

**A correlation id per request.** `CorrelationIdMiddleware` runs first, before
the request logger and before anything that can start a response. It takes
`X-Correlation-Id` from the caller so a chain of calls shares one, and returns
it on every response — including the ones the framework writes itself, because
`AddProblemDetails` puts it in the body of a 401 or a 403 that no handler ever
saw. Every log line written while handling the request carries it. A user
quotes one string and the log query is exact.

The header is validated rather than trusted: at most 64 characters of
`[A-Za-z0-9_-]`, anything else replaced. A value straight from a request into a
log file is a log-injection vector — a newline forges entries that look
authentic to whatever reads them — and a few kilobytes of junk per line is a
slow denial of service against whoever pays per gigabyte ingested.

**Metrics and traces over OTLP**, off unless `OTEL_EXPORTER_OTLP_ENDPOINT` is
set. Vendor-neutral on purpose: every aggregator worth using speaks it, so the
choice of backend stays a deployment decision. Alongside the standard ASP.NET
Core, HTTP-client and runtime instrumentation, `JobMetrics` reports what the
background jobs did. That distinction matters more than it looks — request
metrics say the API is up, and an outbox that cannot reach the mail server
still serves 200s all day while nobody receives a password-reset email. The two
worth an alert are `outbox.abandoned` above zero, which means somebody
definitely did not get something, and `job.failures`, which means a sweep is
not running at all.

Instrument names are asserted by tests. A renamed counter breaks a dashboard
and an alert rule silently — the query returns no data, which looks exactly
like a system with nothing to report.

**What is not code.** Running an aggregator, building the dashboards, and
writing the alert rules live in the deployment. Shipping the signal is what
makes them possible, not what replaces them. Until a collector exists, logs go
to the console (plain text for a developer; point
`Serilog:WriteTo:0:Args:formatter` at `CompactJsonFormatter` for a deployment)
and to a file that, in a container, is written to a layer that disappears with
the container.

## Health probes

`/health/live` answers whether the process is running, and runs no checks to do
it. `/health/ready` checks the database. `/health` is an alias of readiness,
kept for what already points at it.

The split is the whole point. One endpoint that checked the database conflated
"this process is broken, restart it" with "this instance cannot serve traffic
right now", and an orchestrator acts on the first by killing the container — so
a thirty-second database failover restarted every replica, repeatedly, during
the one incident when losing them all is least affordable. Restarting an API
because PostgreSQL is briefly away fixes nothing and costs the warm connection
pools.

Both responses are JSON naming each check, its status and its duration, and no
more. The exception is left out on purpose: these endpoints are
unauthenticated, and a failed database check carries an Npgsql message naming
the host, the database and the user it tried to connect as. That belongs in the
log, where it already is.

The compose file deliberately defines no container healthcheck for the API: the
.NET runtime image ships without curl or wget, so the usual one-liner would
report unhealthy forever, and adding an HTTP client to a production image to
fetch a URL from inside it is a poor trade. An orchestrator fetches the probes
over the network and needs nothing installed.

## Where the refresh token lives

Two clients, two answers, chosen by the client rather than guessed from a user
agent.

The mobile app keeps it in platform secure storage — Keychain,
EncryptedSharedPreferences — and receives it in the response body as before.

The browser cannot do that. `localStorage` is readable by any script on the
origin, so a seven-day refresh token there turns one XSS — from a dependency, a
future rich-text field, an embedded map widget — into a persistent account
takeover rather than a session-length one: the attacker walks away with a
credential that keeps minting access tokens for a week, from anywhere, after
the tab is closed.

So the admin panel sends `X-Auth-Mode: cookie` on sign-in and refresh, and the
API replies with an `HttpOnly`, `SameSite=Strict` cookie scoped to `/api/auth`,
`Secure` when the request arrived over HTTPS — and an **empty** `refreshToken`
in the body. Both halves matter: a cookie that merely duplicates something
already readable by script is not a mitigation, it is a second copy. The
browser then holds a credential no script can reach; an attacker with XSS can
still act *through* the open page, which is a much smaller and much more
recoverable problem.

Three details that are easy to get wrong and are pinned down by tests:

- **The refresh endpoint's body must be optional.** `RefreshTokenCommand`
  declares the token non-nullable because a handler cannot rotate nothing, and
  `[ApiController]` reads that as a required field — so a browser sending `{}`
  plus a cookie was rejected during model binding, before the cookie was ever
  looked at. The HTTP surface takes its own `TokenRequest` where the field is
  optional, and the controller assembles the command from whichever source had
  the token.
- **A blank `Domain` in configuration is not the same as no domain.** It binds
  to an empty string, reaches the wire as `Domain=`, and every browser rejects
  the cookie — so the operator would be signed out on the next refresh, with
  nothing in the logs. Read through `EffectiveDomain`, which maps blank to null.
- **Deleting a cookie requires the same attributes it was written with.**
  Otherwise the browser treats it as a different cookie and keeps the original:
  a sign-out that leaves the credential in place.

`SameSite=Strict` is what makes the cookie its own CSRF defence: another site
cannot cause the browser to send it at all. It works when the API and the panel
share a registrable domain, which is a deployment constraint worth knowing
about — see `Auth:RefreshCookie` in `appsettings.json`.

## API versioning

Every controller answers on two paths: `/api/v1/employees`, which is what a
client should call, and `/api/employees`, which is a permanent alias for
version 1.

The reason to do this before release rather than after: once the mobile app is
in an app store there is no way to make everyone update. A phone on a site runs
whatever its owner last installed, and the first change that alters a response
shape breaks it silently — a screen that stops filling in, reported weeks later
as "the app is broken". With versioned routes the old build keeps calling v1,
which keeps behaving the way it did the day it shipped.

Two decisions worth stating:

**The unversioned routes are an alias, not a deprecation.** Everything written
before versioning existed calls them, removing them would break clients to no
benefit, and `ApiVersioningExtensions.Default` is pinned at 1.0 so they can
never come to mean something else. Letting the default float would move an
un-updated client onto a version it was never written for — arriving as changed
behaviour rather than as an error, which is worse than a 404. A test guards the
constant.

**An unknown version is refused rather than served as v1.** `/api/v9/employees`
answers 404 — nothing claims that route, so routing finds no candidate before
version negotiation is reached. The status is less important than the absence
of a silent fallback: answering a v9 client with v1 data would look like
success and be wrong in whatever way v9 was meant to differ.

The health probes are deliberately outside all of this. An orchestrator's probe
URL should not change when the API does, and a probe is not part of the
contract a client codes against.

## Configuration that fails fast

The rule: a setting that is wrong should stop the process at startup, with a
message naming the setting and what to put in it. The alternative is not "it
works anyway" — it is a failure that surfaces later, somewhere unrelated, to
somebody who has no reason to suspect configuration.

`JwtSettings` has always validated this way. Three more now do.

**Email and Firebase** are bound through `AddOptions().Validate().ValidateOnStart()`
rather than `Configure()`. SMTP port must be a port; `FromAddress` must parse as
an address, because a rejected envelope sender means every message bounces while
the outbox records the send as attempted. Firebase credentials may be given as a
path or as raw JSON but not both — which one wins is an implementation detail
nobody should have to know, and the loser is usually the one just changed — the
path must exist, and the JSON must parse as an object, since a service-account
key pasted into an environment variable is easily truncated or shell-mangled.

Every one of these checks is gated on the feature being configured at all.
Leaving SMTP unset is the normal case on a developer machine; validation that
fired on an unset feature would become something to work around rather than
something to keep. A deployment that needs email is caught separately, by
`ValidateProductionConfiguration`.

**The connection string** is checked with `IsNullOrWhiteSpace`, not against
null. An empty environment variable is set as far as the binder is concerned,
and the failure then arrives from inside Npgsql as a complaint about a missing
host — which sends whoever is reading it to look at the database rather than at
the variable nobody set.

**CORS origins** are checked for shape, and this is the one worth explaining.
An origin is matched against the browser's `Origin` header by string
comparison, and that header is always exactly `scheme://host[:port]`. A
configured `https://admin.example.com/` — with the trailing slash the address
bar shows, and that every other URL setting in the file wants — does not throw
and does not warn. It simply never matches. The symptom is an admin panel that
cannot reach the API, an error in the browser console, and nothing at all in
the server log, because as far as the server is concerned the policy loaded
fine. `CorsOrigins.Describe` rejects a trailing slash, a path, a query, a
fragment, an explicitly written default port, credentials in the URL, and a
wildcard (which cannot be combined with `AllowCredentials`), and the message
names the exact string that would have worked.

Two deliberate exceptions. A host in capitals is accepted, because `Uri`
lowercases it and it genuinely does match — rejecting it would be a startup
failure with nothing behind it. And an empty origin list is a warning, not a
failure: an API with no browser client in front of it is a real deployment.
Empty is different from malformed.

This one validates in every environment, not only outside development. A
malformed origin is never intentional, and finding out locally is the point.

The rules are tested against `CorsService` itself rather than against a
restatement of what it is believed to do, so if ASP.NET Core ever starts
normalising these away the test fails and the validator can be relaxed.
