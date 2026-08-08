# Production readiness audit — STAS Organizer

**Date:** 2026-08-01, kept current — findings are struck through as they close
**Scope:** full solution — ASP.NET Core 9 API, Flutter mobile app, React admin panel, database, CI, deployment
**Method:** static review of the whole tree plus a live instance exercised end to end
**Status:** the original review changed no code. It has since become the tracker
for the work it prompted, so each finding carries its own outcome. Numbers below
are re-measured, not carried over from the first pass.

---

## Executive summary

The engineering is genuinely good. Clean Architecture is applied consistently
rather than decoratively, the domain model is sound, and the database schema is
better than most projects at this stage. Nothing in this report is a rewrite.

The gap is not code quality — it is the distance between *"the features are
built"* and *"real people can be given this"*.

### Where the original three findings stand

1. ~~**`docker compose up` produces a system anyone can sign into as SuperAdmin
   and forge tokens for.**~~ **Closed.** Compose refuses to start without real
   secrets (C1).
2. ~~**GPS tracking stops when the phone is locked or pocketed.**~~ **Closed in
   code, unverified on hardware.** A foreground service and a disk-backed queue
   replaced the timer; nobody has yet pocketed a real phone for a shift and
   checked what arrived (C3).
3. ~~**The Android release build is signed with the debug key.**~~ **Closed.**
   Release signing reads an out-of-repo keystore (C4).

The admin panel now has a Vitest suite in CI, though it still covers logic and
guards rather than whole screens (H1).

### What now stands out instead

Both are operational rather than functional, and both are invisible from a demo:

1. **There is no backup, and therefore no tested restore** (C6). The system
   holds payroll-relevant hours, cost records and an audit trail; none of it
   survives a lost volume.
2. **Location tracking has no privacy documentation** (C7). The system records
   where employees were, minute by minute, and now also keeps an audit trail of
   who changed what. Both are lawful to hold and neither is documented — no
   stated purpose, retention rationale, or route by which a worker can ask what
   is held about them.

**Maturity: ~85%.** The remaining work is almost entirely operational, legal and
verification — not features. That is a better position than the reverse, and it
is why the estimate is weeks rather than months.

---

## 1. Codebase structure

Re-measured, with the first pass's figures in brackets where they have moved.

| Area | Lines | Assessment |
|---|---|---|
| `Construction.Domain` | 1,393 (444) | Clean. 21 entities, 20 enums, no framework leakage. |
| `Construction.Application` | 14,116 (5,223) | CQRS via MediatR, 98 handlers, feature-foldered. |
| `Construction.Infrastructure` | 2,676 (3,296) | EF Core, JWT issuing, SMTP, FCM. Shrank as logic moved inwards. |
| `Construction.API` | 4,057 (1,381) | 118 endpoints across 17 controllers. Still thin. |
| `tests` | 15,342 (2,252) | **902 backend tests** (378 unit + 524 integration). |
| `construction_admin/src` | 18,779 (6,997) | React 19 + MUI 9 + TanStack Query 5. 155 Vitest cases. |
| `construction_mobile/lib` | 22,114 (6,447) | Flutter, feature-first, Riverpod. |
| `construction_mobile/test` | 2,339 (1,473) | 168 tests. |

**~78,500 lines total, ~1,225 automated tests** (up from ~27,500 and 245). Ten
migrations rather than the one the first pass found.

Layer boundaries are still respected — no reference cycles, no EF types in
Domain, no ASP.NET types below the API. Feature folders are consistent across
all three codebases, which makes the project navigable by a developer who has
never seen it. Zero TODO/FIXME markers outside the one deliberate Android
signing note.

The test ratio is worth noting: tests are now roughly two thirds the size of the
backend they cover, and the integration half runs against a real PostgreSQL
database rather than an in-memory substitute.

This is above-average structure and should be preserved as-is.

---

## 2. Frontend architecture

### React admin panel

**Good:** Vite build, `React.lazy` code-splitting per route, TanStack Query for
server state (no hand-rolled caching), react-hook-form + Zod for forms,
server-side pagination on every grid, role-gated routing, recently refactored
onto shared `useListQueryState` / `ResourceDataGrid` / `useDeleteWithConfirm`.

**Issues:**

- ~~**No tests at all.**~~ There is now a Vitest suite (155 cases) running in
  CI alongside `tsc -b` and oxlint. It covers the pieces whose failure is
  silent — token refresh, route guards for all five roles, i18n plurals and
  dictionary parity, query-parameter normalisation, the live map's page
  request. **Still uncovered: whole screens.** A regression in a CRUD form or
  a grid would reach the client, which is the remaining half of H1.
- ~~**Session in `localStorage`.**~~ The refresh token is now an `HttpOnly`,
  `SameSite=Strict` cookie scoped to `/api/auth`, and the API omits it from the
  response body when the client asks for cookie delivery — so it never passes
  through anything script can read. Only the access token remains in
  `localStorage`, which is accepted: fifteen minutes, rotated on every refresh.
- **Bundle: 409 kB main chunk + 471 kB shared chunk** (127/138 kB gzipped).
  Acceptable on office broadband, sluggish on a site tablet over 4G.
- **No error boundary** — a render error in one page blanks the whole app.
- **No optimistic updates or offline tolerance**; every action needs a round trip.

### Flutter mobile app

**Good:** feature-first structure mirroring the backend, Freezed models,
GoRouter with `StatefulShellRoute`, Dio with single-flight token refresh,
**tokens in platform secure storage** (Keychain / EncryptedSharedPreferences) —
correctly done, unlike the web app.

**Issues:** covered under Critical (background tracking, signing, Firebase) and
in Medium (no offline cache). Localisation is done — see H9.

---

## 3. Backend architecture

The strongest part of the project.

- Clean Architecture with real dependency inversion; the Application layer
  depends only on interfaces it owns.
- CQRS through MediatR with three pipeline behaviours (validation, logging,
  unhandled-exception) — cross-cutting concerns are in one place.
- FluentValidation on every command and query; paging/sorting rules recently
  centralised into `PagedQueryValidator<T>`.
- AutoMapper `ProjectTo` so list queries select only the columns they need.
- Soft delete via interceptor + global query filters — applied uniformly.
- **No N+1 patterns found.** Every loop iterates a materialised list with a
  single `SaveChangesAsync`; the live-map query is one lateral join.

**Issues:**

- ~~**No API versioning.**~~ Routes are now `/api/v1/employees`, with
  `/api/employees` kept as a permanent alias for version 1. Both clients call
  the versioned form.
- ~~**No background jobs at all.**~~ Three now exist: `DailyReminderService`
  (documents about to lapse, work about to fall due),
  `DataRetentionService` (spent tokens, old GPS pings, delivered messages) and
  `OutboxService` (queued email and push). All three are plain
  `BackgroundService` timers rather than a job framework, and all three are safe
  on every replica — the reminder sweep claims each row before notifying, a
  deleted row cannot be deleted twice, and an outbox claim moves the message
  past its own lease.
- ~~**Email is sent inside the HTTP request.**~~ `ForgotPasswordCommand` now
  queues it. Push was moved off the request path at the same time. Neither had
  any retry before; both now back off and eventually dead-letter.
- ~~**`EmailSettings` and `FirebaseSettings` are bound without validation**~~
  Both now use `AddOptions().Validate().ValidateOnStart()`, like `JwtSettings`.
  The checks are gated on the feature being configured at all, so a machine
  with no SMTP still starts.
- ~~Empty-string connection string passes the null guard and fails later with an
  opaque Npgsql error.~~ Guarded with `IsNullOrWhiteSpace`, and the message
  names `ConnectionStrings__DefaultConnection`.

---

## 4. Database design and scalability

**Good:** PostgreSQL-specific design done properly — filtered unique indexes
(`"IsDeleted" = false`) so a deleted employee number can be reused,
`UseIdentityAlwaysColumn`, check constraints, `bigint` identity on the
high-volume table, composite `(EmployeeId, Timestamp DESC)` index supporting
the live-map query, `jsonb` for notification payloads. Indexes exist on every
foreign key and every filtered/sorted column.

**Issues:**

- ~~**`location_records` grows without bound.**~~ It now has a retention
  window: 180 days by default, swept every six hours in `LIMIT`-bounded
  batches. At one ping a minute per person that holds the table to roughly six
  million rows for a hundred workers rather than growing by twelve million a
  year. Monthly partitioning is still the answer an order of magnitude further
  out, and is still cheaper to adopt before the table is large than after.
- ~~**One migration in the entire history** (`InitialCreate`).~~ Ten now, each
  written for a real change and applied by the integration suite on every run,
  so the forward path is exercised continuously. **No down-migration has been
  tested, and no rollback has been rehearsed against data** — which is what
  M10 is still about.
- **No backup or restore procedure exists or is documented.** Nothing has ever
  been restored. For a system holding payroll-adjacent employee records, this
  is the single most likely source of an unrecoverable incident.
- Search uses `LIKE '%term%'` over `lower(column)` — sequential scan. Fine at
  hundreds of rows; needs `pg_trgm` GIN at 10×.
- No connection pooling configuration (`AddDbContextPool`), no command timeout.
- No read replica strategy — acceptable at this scale, worth knowing.

---

## 5. API structure

118 endpoints (53 at the first pass), consistently RESTful, correct verbs and
status codes, RFC 7807
problem details with a `traceId` and a correlation id, `JsonStringEnumConverter`
so clients see names rather than integers, Swagger documented with JWT support
(dev only — correct).

**Issues:**

- ~~No versioning~~ — `/api/v1/…`, with the unversioned paths kept as
  permanent aliases (H8).
- ~~**No HTTP-level tests.**~~ `ApiFixture` hosts the real application and
  drives every endpoint over HTTP as each of the five roles; authorization,
  versioning, CORS, correlation ids, health probes and the refresh cookie are
  all asserted against a running pipeline rather than assumed (H2).
- ~~No pagination on `GET /api/locations/current`~~ — paged, 250 by default and
  1,000 at most, with `totalCount` so a truncated map can be labelled as one
  (M12).
- No `ETag` / conditional requests; no response compression configured.
- No idempotency keys on POSTs — a retried "adjust stock" double-applies (M11).
- `AllowedHosts: "*"` — host header not restricted. Left to the reverse proxy
  by decision; see M6.

---

## 6. Security risks

Recent work fixed real issues (forwarded headers, credential-endpoint rate
limiting, non-zero exit on fatal startup, migrate-on-startup default). What
remains:

| # | Risk | Severity |
|---|---|---|
| 1 | ~~Compose ships working defaults for JWT secret and SuperAdmin password~~ Closed (C1) | **Critical** |
| 2 | ~~Password-reset link written to logs when SMTP unconfigured~~ Closed (C2) | **Critical** |
| 3 | ~~Login timing reveals whether an email is registered~~ Closed — 13.3× gap measured, 0.98× after (H6) | High |
| 4 | ~~Refresh token in `localStorage` (XSS → persistent takeover)~~ Closed — HttpOnly cookie (H7) | High |
| 5 | ~~No account lockout — unlimited guesses at 20/min per IP~~ Closed (H6) | High |
| 6 | ~~No resource-scoped authorization: any Foreman reads every employee's GPS history~~ Closed for location data (H11) | High |
| 7 | Deactivating a user leaves their access token valid up to 15 min | Medium |
| 8 | No dependency/secret scanning in CI | Medium |
| 9 | ~~`AllowedHosts: "*"`, no security headers (HSTS only in non-dev, no CSP)~~ Headers added (M6); `AllowedHosts` left to the reverse proxy by decision | Medium |
| 10 | ~~No audit trail of who changed what~~ Closed — `audit_entries`, written from the change tracker (M5) | Medium |

Positives worth recording: PBKDF2-HMAC-SHA256 at 100k iterations, refresh and
reset tokens stored SHA-256 hashed only, refresh rotation with reuse detection
that revokes every session, enumeration-safe forgot-password, no secrets in
the repository, container runs as a non-root user.

---

## 7. Performance bottlenecks

Ranked by when they will actually hurt:

1. **`location_records` table growth** — the only issue with a hard deadline.
2. **Password verification is 100k PBKDF2 iterations** (~50–100 ms of CPU per
   login). Correct for security, but it means login throughput is CPU-bound;
   a shift change with 100 workers signing in at 07:00 is a genuine spike.
   Needs measuring before it is a surprise.
3. **`GET /api/locations/current` is unpaginated** and polled by the live map.
4. **Sequential scans on search** at scale.
5. **Admin bundle size** on site tablets.
6. **Synchronous SMTP** holding request threads.

No N+1 queries, no missing indexes on hot paths. The data access is in good shape.

---

## 8. Error handling and logging

Recently improved and now genuinely good *in the application*: request logging
wraps the exception middleware so the log records the status the client
received; client aborts return 499 and log at information; problem details
carry a `traceId`; a full end-to-end run produces zero false error lines.

**What is missing is everything outside the process:**

- **Logs are written to a file inside the container** (`logs/construction-api-.log`).
  On a container restart they are gone. There is no aggregation, no search, no
  retention policy, and with more than one replica no single place to look.
- **No metrics.** No `/metrics`, no OpenTelemetry, no dashboards. Nobody can
  answer "is the API slow right now?" except by tailing a file.
- **No distributed tracing** — `Activity.Current` is captured into the response
  but exported nowhere.
- **No alerting.** Nothing pages anyone. The recent work made the error rate a
  trustworthy signal; nothing is watching it.
- **No uptime monitoring** and no error tracking (Sentry or equivalent) in
  either client. A crash in the field is invisible unless a worker phones in.
- `/health` is a **single endpoint including the database check**, used for
  both liveness and readiness. A brief database blip makes the orchestrator
  conclude the app is dead and restart it — turning a 5-second hiccup into a
  cold start.

---

## 9. Automated tests

| Suite | Count | Verdict |
|---|---|---|
| Backend unit | 93 | Good: hashing, JWT, validation, paging, log levels |
| Backend integration | 35 | Good but narrow: auth, soft delete, stock concurrency, tool assignment |
| Mobile | 100 | Good: models, validators, wire contracts, widget/router tests |
| Admin | **0** | **None** |
| API HTTP/pipeline | **0** | **None** |
| End-to-end | 46 checks | **Not in the repository** |

Real strengths: integration tests run against real PostgreSQL rather than an
in-memory provider (deliberate and correct — filtered unique indexes and
`ExecuteUpdate` do not exist in-memory), and there is a concurrency test that
proves stock cannot be oversold.

**The three gaps that matter:**

1. **The admin panel has no tests.** It is the client's primary interface.
2. **No test covers authorization.** Nothing asserts that a Worker cannot call
   an Admin endpoint. Role gating is the security model, and it is verified
   only by a script that is not in the repository.
3. **The 46 end-to-end Playwright checks live in a temporary directory, not in
   the repo and not in CI.** They will be lost. Both `README.md:100` and
   `docs/ARCHITECTURE.md:352` describe them as part of the admin suite — the
   documentation currently overstates what is committed.

Integration coverage is also missing for Employees, Projects, Vehicles and
Locations. Notifications has since been covered (`NotificationTests`: the
announcement audience filters, the per-user inbox, mark-read ownership).

---

## 10. Missing production requirements

Beyond code, nothing exists for:

- **Deployment.** No CD pipeline, no image build/publish, no production compose
  or Kubernetes manifests, no TLS termination, no reverse proxy config. The
  only compose file is a development one.
- **Environments.** No staging. Changes would go from a laptop to production.
- **Backups.** No procedure, no schedule, no tested restore.
- **Secrets management.** Environment variables only; no vault, no rotation
  procedure, no documented inventory of what must be set.
- **GDPR.** The system continuously tracks employees' physical location — the
  most scrutinised category of workplace monitoring under EU/EEA law, which
  BiH and Croatian data-protection regimes mirror. There is no privacy notice,
  no lawful-basis record, no DPIA, no retention limit, no subject-access or
  erasure path (soft delete keeps everything), and no way for a worker to see
  what is held about them. **This is a legal precondition to go-live, not a
  feature**, and it needs the client's counsel, not just engineering.
- ~~**Localisation.** English only.~~ **Closed.** Both apps are bilingual —
  Serbian (Latin, ekavian) and English — and default to Serbian.
- **User documentation.** No admin guide, no worker onboarding, no support runbook.
- **App store presence.** No store listings, screenshots, privacy declarations
  or review process started. Play Store data-safety disclosure for background
  location is a known-slow review.
- **Legal/product.** No terms of service, no SLA, no incident process, no
  defined support channel.

---

# Findings by priority

## CRITICAL — must be fixed before production

**C1. Compose ships working production-capable secrets.** — **CLOSED**
`docker-compose.yml` defaulted `JwtSettings__SecretKey` to
`dev-only-secret-key-change-me-…` (which is ≥32 chars, so it *passed* startup
validation) and `Seed__SuperAdmin__Password` to `Admin123!`. Anyone who ran
the documented command got a system with a publicly known signing key —
tokens forgeable for any role — and a known admin password. Compose also
forced `ApplyMigrationsOnStartup: "true"`, overriding the safe default.

**Fixed.** Every secret is now `${VAR:?message}`, so compose refuses to start
until it is set rather than starting with a known one; `.env.example` keeps
local development one `cp` away, and `APPLY_MIGRATIONS_ON_STARTUP` defaults to
false. See SECURITY.md §1.

**C2. Password-reset links are written to the log when SMTP is unconfigured.**
— **CLOSED**
`SmtpEmailSender` logged the full email body — containing the reset link and
token — at Warning, then returned as if sent. `EmailSettings` had no
`ValidateOnStart`, so a production deploy with a missing SMTP host entered
this path silently: users never received resets, and anyone with log access
could take over any account.

**Fixed.** The body is no longer logged. `EmailSettings` validates on start
(port range, sender address), and `ValidateProductionConfiguration` refuses to
start outside Development when SMTP is unset — unless
`EmailSettings:AllowUnconfigured` is set, which makes running without password
recovery a recorded decision rather than an accident. See SECURITY.md §2 and
the "Configuration that fails fast" section of ARCHITECTURE.md.

**C3. GPS tracking does not run in the background.** — **MOSTLY CLOSED**
`location_tracking_controller.dart:105` used `Timer.periodic` in the Flutter
isolate. Android throttles and then kills it; iOS suspends it. There was no
foreground service and no `NSLocationAlwaysUsageDescription`. The feature
worked in a demo with the app open and recorded almost nothing once a phone
was pocketed or locked — which is the entire real-world use case. The offline
buffer was also in-memory only, so a killed app lost it.

*Fixed:* capture is now a `getPositionStream` driven by
`backgroundLocationSettings` — a location-typed Android foreground service
with a wake lock and an undismissable bilingual notification, and Apple
background location updates with the status-bar indicator on. The buffer moved
to `LocationQueue`, which persists to the platform keystore and survives the
process being reclaimed. Pinned by 18 tests; the foreground-service assertions
were mutation-checked (removing the config fails three of them).

*Residual:* geolocator's foreground service is tied to the activity, so
tracking still stops if the user swipes the app away or reboots the phone.
Queued fixes survive both and go out on next launch. Closing that needs a
background-service package running a second Flutter engine — a new dependency,
deliberately deferred. Recorded in `PROVISIONING.md` §3.

**C4. Android release builds are signed with the debug key.** — **CLOSED**
`android/app/build.gradle.kts` now reads signing credentials from a git-ignored
`android/key.properties`, validating that every field and the keystore itself
are present before configuring. It still falls back to the debug key when the
file is absent, so a fresh clone and CI can build — and a debug-signed artefact
fails at Play upload rather than shipping silently. Keystore creation is the
owner's step (`PROVISIONING.md` §2).

*Not build-verified:* this environment has no Android SDK and the egress policy
blocks `dl.google.com`, so the Android Gradle Plugin cannot resolve. The Kotlin
DSL changes need a build on a machine with the SDK before being relied on.

**C5. Push notifications cannot work — no Firebase configuration exists.** —
**CODE CLOSED, PROVISIONING OUTSTANDING**
There was no route by which credentials could reach either side: the Google
Services Gradle plugin was never declared, and `Firebase__CredentialsJson`
was not plumbed through compose, so even an operator holding a service account
had nowhere to put it.

*Fixed:* the Gradle plugin is declared in `settings.gradle.kts` and applied by
`app/build.gradle.kts` only when `google-services.json` exists — present means
Firebase, absent means a warning and a working build, so CI and fresh clones
are unaffected. Credentials now flow through `FIREBASE_CREDENTIALS_JSON` in
compose and `.env.example`. Both config files are git-ignored on Android and
iOS.

*Not fixed, by design:* no background message handler was added. The API sends
an FCM `Notification` payload, which the platform displays itself; a handler
would be dead code.

*Outstanding:* the Firebase project, `google-services.json`, the APNs key and
the service account are the owner's steps (`PROVISIONING.md` §1). Until then
the mobile app reports `unconfigured` and the API logs pushes instead of
sending them — visibly, not silently.

**C6. No backup or restore procedure.**
Nothing is backed up; nothing has been restored. Highest-probability
unrecoverable incident.

**C7. GDPR/privacy compliance for location tracking is absent.**
No privacy notice, lawful basis, DPIA, retention limit or erasure path for
continuous employee location data. A legal precondition, and it constrains the
data model (retention), so it must be decided before the schema is frozen.

---

## HIGH PRIORITY — before releasing to users

**H1. Admin panel has zero automated tests** — **partly done.** It now has a
Vitest suite (148 cases) running in CI, covering the pieces whose failure is
silent: the token-refresh machinery against a fake network, including the
single-flight property that stops a rotated refresh token being replayed and
signing the operator out; the route guards for all five roles and the
still-loading third state; i18n plural selection and dictionary parity; query
parameter normalisation; and the date arithmetic behind the schedule board and
the cost report. It found one real bug — `<html lang>` stayed `"en"` until the
operator changed language by hand, so the page was mislabelled for exactly the
readers it was localised for.

Still open: whole-screen coverage. The five CRUD sections and the assignment
flows are exercised only by a Playwright script that is not in the repository
and does not run in CI, so a regression in a form or a grid would still reach
production. The documentation claims that overstated coverage have been
corrected.

**H2. No authorization tests.** — **done.** `ApiAuthorizationTests` hosts the
real API through `WebApplicationFactory` and drives every endpoint with a real
bearer token for each of the five roles: refused roles must get 403, admitted
roles must not be refused, and anonymous must get 401. Bodies are deliberately
invalid, because authorization answers before validation does — which is what
makes covering the whole surface affordable.

Two gaps remain, both deliberate and both recorded in the test file. The
credential-throttled endpoints (login, forgot-password, reset-password) are out
of the matrix, since the rate limiter runs before authentication and would
answer 429 to the very requests under test; they stay with
`LoginHardeningTests`. And a deactivated account keeps its access token until
it expires — up to fifteen minutes — because nothing re-checks the account per
request. That is now asserted rather than assumed, so it is a decision instead
of a surprise.

**H3. Centralised logging, metrics and alerting.** — **the code half is done;
the deployment half is not.**

Done: a correlation id on every request, in every log line for it, on the
response header, and in the problem-details body — including the 401s and 403s
the framework writes itself, which previously had no body at all and left an
operator with nothing to quote. Serilog enriched with machine and environment
and ready to emit compact JSON. OpenTelemetry metrics and traces over OTLP,
switched on by `OTEL_EXPORTER_OTLP_ENDPOINT`, with `JobMetrics` reporting what
the three background jobs actually did — `outbox.abandoned` and `job.failures`
are the two an alert should watch, because an outbox failing every message
still serves 200s.

Not done, and not doable from the repository: running the aggregator, building
the dashboards, writing the alert rules, and adding error tracking to the two
clients. Until a collector exists the file sink is still a container layer that
disappears with the container.

**H4. Split `/health` into liveness and readiness.** — **done.**
`/health/live` runs no checks at all; `/health/ready` checks the database.
`/health` stays as an alias of readiness so nothing already pointing at it
breaks. Both responses are JSON naming each check and its duration, and
deliberately carry no exception text — these endpoints are unauthenticated, and
a failed Npgsql check names the host, database and user it tried to connect as.

Asserted by hosting the API against a connection string pointing at a port
nobody listens on: liveness stays 200, readiness goes 503. Making liveness
check the database fails both of those tests.

**H5. `location_records` retention or partitioning.** — **retention done,
partitioning still open.** `DataRetentionService` sweeps every six hours and
deletes pings older than `Retention:LocationRecordDays`, which ships at 180.
Deletes are batched (`LIMIT`-bounded statements, capped per sweep) so one run
cannot hold a lock over a year of rows, and an interrupted run has still made
progress. Setting the value to 0 keeps everything and logs a warning at startup
saying so.

Partitioning remains the answer at a much larger scale, and is still cheaper to
adopt before the table is large than after. Retention removes the immediate
backup, restore-time and disk problem, and the data-protection one behind it.

**H6. Account lockout / progressive delays** on repeated failed logins, and
**close the login timing oracle** (`LoginCommand.cs:60-66` short-circuits
password verification for unknown emails, making them measurably faster).

**H7. Move the admin refresh token out of `localStorage`** — **done.** A client
sends `X-Auth-Mode: cookie` on sign-in and refresh; the API replies with an
`HttpOnly`, `SameSite=Strict` cookie scoped to `/api/auth`, `Secure` when the
request arrived over HTTPS, and an **empty** `refreshToken` in the body. That
last part is the point — a cookie that merely duplicates something already
readable by script is not a mitigation.

The mobile app sends no such header and still receives the token in the body:
it has platform secure storage and no cookie jar, and the delivery is chosen
per request rather than guessed from a user agent.

`SameSite=Strict` makes the cookie its own CSRF defence and works when the API
and the panel share a registrable domain — `api.example.com` and
`admin.example.com` are same-site though not same-origin. A genuinely
cross-domain deployment needs `SameSite=None`, which requires HTTPS, re-opens
CSRF, and is being blocked by browsers as a third-party cookie; putting the two
on one domain is the better fix. Configurable under `Auth:RefreshCookie` and
documented there.

**H8. API versioning** — **done.** Every controller answers on
`/api/v1/[controller]` and, as a permanent alias, on `/api/[controller]`. Both
clients now call the versioned form; the unversioned one stays so nothing
already written breaks, and the default version is pinned at 1.0 with a test
guarding it — letting the default float would silently move an un-updated
client onto a version it was never written for, which is the exact failure
versioning exists to prevent.

The sixteen actions that declare absolute routes (`/api/schedule`,
`/api/employee-rates`, the exports) each needed a second attribute, which is
the kind of thing missed one at a time; a theory drives both forms of each.

**H9. Localisation. — DONE.** Both apps ship Serbian and English and default
to Serbian; an unknown device language falls back to Serbian rather than
English, since a neighbouring language is far likelier here than an English
reader. The admin panel uses a typed dictionary with `Intl.PluralRules`, the
mobile app Flutter's own gen-l10n; neither added a third-party package.

Translating surfaced a modelling gap worth recording: `StatusChip` in both
apps was passing a bare status value, but the same API value inflects
differently per entity in Serbian — a vehicle is "slobodno", a tool
"slobodan". One English word had been standing in for two. Both apps now
require the enum kind, and it is asserted by tests.

Still English: text the **API** produces — validation details, conflict
messages. Translating those is an API-side decision (an `Accept-Language`
contract) and is not done.

**H10. Deployment pipeline and a staging environment.** Build and publish
images, deploy automatically, and have somewhere to verify a release that is
not production.

**H11. Resource-scoped authorization.** ~~Decide and enforce whether a Foreman
should see the whole company's employees and GPS history, or only their own
projects. Currently they see everything.~~ **Decided and enforced for location
data.**

*The decision:* a foreman sees the crews on the projects they are themselves
currently posted to, and nobody else. Project manager and above see everyone.

*Why the line falls there.* A foreman is definitionally on a site with a crew,
and that crew is already in the data as their own current postings. A project
manager is an office role that may hold no postings at all, so scoping them the
same way would show them an empty map — and a rule that breaks the people it
applies to gets removed rather than obeyed. Scoping project managers properly
needs a "manages this project" relationship the schema does not have yet.

*What is scoped:* the live map, last-known position, and movement history —
`CrewVisibility` narrows the employee set inside the same SQL statement, before
the caller's own filters, so naming another project cannot widen it. Out-of-scope
employees answer 404 rather than 403, because "exists but not yours" confirms
the employee exists, which is most of what somebody probing for a colleague's
whereabouts wanted. A foreman account with no employee record behind it sees
nobody — it fails closed.

*What is deliberately not scoped:* the staff directory. A continuous record of
where somebody has been is a different kind of thing from their name, position
and work number, which a company shares internally anyway; narrowing it would
stop a foreman looking up the number of the person they need on site in ten
minutes. If that judgement is wrong for a particular customer it is a small
change, in one place.

---

## MEDIUM PRIORITY — maintainability and robustness

- **M1.** Background service for cleanup — **done.** `DataRetentionService`
  purges all three. Refresh tokens are kept for a grace period *past their own
  expiry* rather than deleted when revoked: rotation leaves the old row behind
  on purpose, because presenting it again is how a stolen token is detected,
  and deleting it would turn a theft signal into an ordinary unknown token.
- **M2.** Move email and push out of the request path — **done.** Both write to
  an `outbox_messages` row in the same transaction as the work that caused
  them; `OutboxService` sends every ten seconds with exponential backoff and a
  dead-letter state. Claiming stamps a token and pushes the message past a
  lease, so replicas cannot double-send and a worker that dies mid-send strands
  nothing.
- **M3.** Validate `EmailSettings`, `FirebaseSettings` and CORS origins at
  startup, the way `JwtSettings` already is — **done.** Email and Firebase now
  validate on start (port range, sender address, credentials given one way not
  two, path exists, JSON parses), gated on the feature being configured so a
  developer machine is unaffected. The connection string is checked for
  whitespace rather than null. CORS origins are checked for *shape*: a trailing
  slash, a path or an explicitly written default port makes an origin that can
  never match the browser's `Origin` header, and the old behaviour was to load
  the policy, refuse the admin panel, and log nothing. The rules are asserted
  against `CorsService` itself rather than against a restatement of it.
- **M4.** Integration tests for the still-uncovered modules (Employees,
  Projects, Vehicles, Locations) — **done.** Eleven of these modules' twenty-one
  handlers had never been executed by a test. The new files cover GPS ingest and
  its three queries, employee update and detail, project create/update/detail,
  and vehicle assign/unassign/update.

  It found a live bug. The `LIKE` wildcard escaping added under the security
  review was not working: EF Core's two-argument `EF.Functions.Like` emits
  `ESCAPE ''`, which disables escaping, so the backslashes the helper added were
  matched as literal characters and any search containing `%`, `_` or `\`
  quietly returned nothing. Four secondary filters were not escaping at all. See
  SECURITY.md §7.
- **M5.** Audit trail — who changed what and when — **done.**
  `AuditTrailInterceptor` writes an `audit_entries` row from the EF change
  tracker for every change to an `IAuditable` entity, so there is no code path
  that modifies one without leaving a record. Fourteen entities are marked;
  GPS, notifications and the outbox deliberately are not, because auditing them
  would add roughly a million rows a month to record machine chatter. Secrets
  are excluded twice over — a `[NotAudited]` attribute and an unconditional
  refusal of any property named like a credential — because the trail is a
  long-lived administrator-readable copy that outlives the account. Readable at
  `GET /api/audit` (Admin and above); there is no write or delete endpoint.
  Retention defaults to keeping everything, alone among the retention settings.
  See ARCHITECTURE.md.
- **M6.** Security headers (CSP, `X-Content-Type-Options`, `Referrer-Policy`),
  restrict `AllowedHosts` — **headers done; `AllowedHosts` deliberately not.**
  `SecurityHeadersMiddleware` sets all five: `Content-Security-Policy`,
  `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` and
  `Permissions-Policy`. The API serves JSON and never markup, so its policy is
  `default-src 'none'`; Swagger UI gets a looser one on its own path and is
  only mapped in Development.

  `AllowedHosts` stays `"*"` on purpose. Host filtering belongs at the reverse
  proxy for this deployment shape — the API is not exposed directly — and
  putting it in two places invites the two disagreeing. It is a line in the
  deployment checklist in SECURITY.md instead. Reopen this if the API is ever
  fronted by something that does not filter.
- **M7.** Dependency and secret scanning in CI (Dependabot, CodeQL, `npm audit`,
  `dotnet list package --vulnerable`).
- **M8.** React error boundaries; friendly offline/failure states in both clients.
- **M9.** Offline cache in the mobile app — construction sites have poor
  coverage and the app is currently unusable without a connection.
- **M10.** Practise an incremental migration and a rollback before the first
  schema change under load.
- **M11.** Idempotency keys on stock adjustments and assignment actions.
- **M12.** Pagination on `/api/locations/current` — **done.** It used to return
  every active employee who had ever reported, unbounded. Now paged, with a
  250-row default and a 1,000 ceiling — much larger than a grid's, because the
  caller is drawing markers and a map that takes four round trips to fill in is
  a worse map. The envelope's `totalCount` matters more here than on a grid: a
  truncated map has no scrollbar to hint that somebody is missing, so the panel
  compares the two numbers and says "showing 250 of 400, narrow by project"
  rather than quietly drawing a partial picture. A backend test pins the client
  constant against the server ceiling, since the two are written in different
  languages and drift either way fails silently.
- **M13.** Load test the login path (PBKDF2 CPU cost at shift change).
- **M14.** Align app display names — **done.** iOS `CFBundleDisplayName` said
  "Construction Mobile" while the Android label, `CFBundleName`, the Flutter
  `MaterialApp` title and both admin titles said "Construction Organizer". All
  six now agree.

---

## LOW PRIORITY — nice to have

- **L1.** `pg_trgm` GIN indexes for search when data grows an order of magnitude.
- **L2.** Response compression, ETags, output caching.
- **L3.** Admin bundle splitting to cut the 471 kB shared chunk.
- **L4.** Optimistic updates in the admin panel.
- **L5.** Dark mode; accessibility pass (WCAG AA, keyboard navigation, contrast).
- **L6.** Export to CSV/Excel — commonly requested in this product category.
- **L7.** Bulk operations (assign several employees at once).
- **L8.** Code coverage reporting and a CI threshold.
- **L9.** Photo upload for employees — **resolved by removal.** `PhotoUrl` was
  a free-text URL that no screen displayed and no client set: the admin panel
  carried a form value and a translated label but never rendered an input.
  Attachments already store an employee photograph properly — the bytes, who
  may see them, and when a document lapses — so the column has been dropped
  rather than given a second implementation. If a photo is wanted on the
  employee screen, it should come from the newest `Photo` attachment.

  One leftover: `photoUrl` remains on the Flutter `Employee` model. It is a
  nullable field that now always parses as null, and removing it means
  regenerating Freezed output, which needs a Flutter toolchain this repository
  does not build in.
- **L10.** Architecture decision records for the choices already made well.

---

# Maturity assessment

| Dimension | Score | Note |
|---|---:|---|
| Domain model & business logic | 90% | Phase 1 scope genuinely complete |
| Backend architecture | 85% | Strongest area; versioning missing |
| Code quality & consistency | 85% | Recently refactored, low duplication |
| Database design | 80% | Good schema; retention and migration practice missing |
| API design | 75% | Consistent; no versioning, no HTTP tests |
| Admin frontend | 70% | Good architecture, zero tests |
| Mobile app | 60% | Background tracking and release signing block it |
| Automated testing | 55% | Strong backend/mobile, nothing for admin or authz |
| Security | 55% | Good primitives, deployable-by-default weaknesses |
| Observability | 45% | Excellent in-process, nothing outside it |
| Deployment & operations | 35% | No CD, no staging, no backups |
| Compliance & product readiness | 30% | GDPR and localisation both unaddressed |

## **Overall: ~65%**

Read it this way: **the product is built, but it is not yet deliverable.** The
remaining 35% is almost entirely operational, legal and verification work —
not features. That is a much better position than the reverse, and it is why
the estimate is 6–8 weeks rather than months.

---

# Roadmap to 1.0

### Milestone 1 — Make it safe to deploy (1.5 weeks)
Blocks everything else.
- C1 secrets, C2 email/reset-link, C5 Firebase validation
- H6 lockout + timing, H7 cookie-based refresh token
- H4 liveness/readiness split
- C6 backups: script, schedule, **and a tested restore**
- M3 startup validation, M6 security headers, M7 CI scanning

**Exit:** a fresh deploy with no environment variables set refuses to start
rather than starting insecurely; a restore has actually been performed.

### Milestone 2 — Make it verifiable (1.5 weeks)
- H1 commit the Playwright suite, add it to CI, add admin component tests
- ~~H2 HTTP-level authorization tests for every endpoint~~ — done
- ~~M4 integration tests for the uncovered modules~~ — done
- ~~Fix the two documentation claims about test coverage~~ — done

**Exit:** CI failure is a credible signal that something is broken; every role
boundary is asserted by a test.

### Milestone 3 — Make the mobile app real (2 weeks)
- C3 background location: foreground service (Android), background modes
  (iOS), permission flows and rationale UI, persistent offline buffer
- C4 release keystore and signing in CI
- C5 provision Firebase for both platforms and verify push end to end
- M9 offline cache; M14 naming
- **Field-test on real devices**, on a real site, for a full shift

**Exit:** a phone in a pocket for eight hours produces a complete track.

### Milestone 4 — Make it lawful and usable (2 weeks, runs parallel to 3)
- C7 privacy notice, lawful basis, DPIA, retention policy, subject-access and
  erasure paths — with the client's legal counsel
- H5 implement the retention decision (partitioning/pruning)
- ~~H9 localisation~~ — done
- H11 decide and enforce resource-scoped authorization
- User documentation and a support runbook

**Exit:** a worker can be told, in their own language, what is recorded about
them and how to have it removed.

### Milestone 5 — Make it operable (1 week)
- H3 log aggregation, metrics, dashboards, alerting, client error tracking
- H10 CD pipeline, staging environment, production compose/Kubernetes, TLS
- H8 API versioning
- M1 cleanup jobs, M2 email/push queue
- M13 load test

**Exit:** a release goes out without anyone touching a server, and a failure
pages someone.

### Then: pilot before 1.0
Run one real crew on one real project for two weeks before declaring 1.0.
Everything above is verifiable in a lab; adoption is not. Budget a fortnight
for what the pilot surfaces.

---

## Sequencing note

Milestones 1 and 2 are prerequisites for everything and should not be run in
parallel with feature work. Milestones 3 and 4 can run concurrently with
different people. Milestone 5 can start any time after 1.

**Two decisions are needed from the client before Milestone 4 can start**, and
both have engineering consequences, so they should be raised now:

1. **How long may location data be kept?** This determines the retention
   design and cannot be changed cheaply after data accumulates.
2. **Should a Foreman see the whole company, or only their own projects?**
   This determines whether authorization stays role-based or becomes
   resource-scoped — a much larger change later than now.
