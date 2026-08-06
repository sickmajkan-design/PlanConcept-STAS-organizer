# Production readiness audit — STAS Organizer

**Date:** 2026-08-01
**Scope:** full solution — ASP.NET Core 9 API, Flutter mobile app, React admin panel, database, CI, deployment
**Method:** static review of the whole tree plus a live instance exercised end to end
**Status:** analysis only. No code was changed.

---

## Executive summary

The engineering is genuinely good. Clean Architecture is applied consistently
rather than decoratively, the domain model is sound, the database schema is
better than most projects at this stage, and 245 automated tests pass. Nothing
in this report is a rewrite.

The gap is not code quality — it is the distance between *"the features are
built"* and *"real people can be given this"*. Three findings stand out
because they are invisible from the demo that has been shown to the client:

1. **`docker compose up` produces a system anyone can sign into as SuperAdmin
   and forge tokens for.** Every secret has a working development default.
2. **GPS tracking — the flagship feature — stops when the phone is locked or
   pocketed.** It runs on a foreground timer. In a demo it works perfectly; on
   a site it records almost nothing.
3. **The Android release build is signed with the debug key**, so it cannot be
   published to the Play Store at all.

Alongside those: the admin panel (7,000 lines, the client's primary interface)
has **zero automated tests in the repository**. *(The English-only finding
below is now closed — both apps ship Serbian and English, Serbian by default.)*

**Maturity: ~65%.** A strong Phase 1 build. Roughly 6–8 weeks of focused work
from a defensible 1.0.

---

## 1. Codebase structure

| Area | Lines | Assessment |
|---|---|---|
| `Construction.Domain` | 444 | Clean. 12 entities, 8 enums, no framework leakage. |
| `Construction.Application` | 5,223 | CQRS via MediatR, feature-foldered, consistent. |
| `Construction.Infrastructure` | 3,296 | EF Core, JWT issuing, SMTP, FCM. |
| `Construction.API` | 1,381 | 53 endpoints across 8 controllers. Thin. |
| `tests` | 2,252 | 128 backend tests (93 unit + 35 integration). |
| `construction_admin/src` | 6,997 | React 19 + MUI 9 + TanStack Query 5. |
| `construction_mobile/lib` | 6,447 | Flutter, feature-first, Riverpod. |
| `construction_mobile/test` | 1,473 | 100 tests. |

**~27,500 lines total.** Layer boundaries are respected — no reference cycles,
no EF types in Domain, no ASP.NET types below the API. Feature folders are
consistent across all three codebases, which makes the project navigable by a
developer who has never seen it. Zero TODO/FIXME markers outside the one
deliberate Android signing note.

This is above-average structure and should be preserved as-is.

---

## 2. Frontend architecture

### React admin panel

**Good:** Vite build, `React.lazy` code-splitting per route, TanStack Query for
server state (no hand-rolled caching), react-hook-form + Zod for forms,
server-side pagination on every grid, role-gated routing, recently refactored
onto shared `useListQueryState` / `ResourceDataGrid` / `useDeleteWithConfirm`.

**Issues:**

- **No tests at all.** `package.json` has no test runner — no Vitest, no
  Testing Library, no Playwright. `npm run lint` is oxlint only. CI runs lint +
  build. A regression in any of the 5 CRUD sections, the login flow, or the
  role gating would reach the client.
- **Session in `localStorage`** (`src/api/session.ts:16`). The 7-day refresh
  token is readable by any script on the page. One XSS — from a dependency, a
  future rich-text field, an injected map widget — is a persistent account
  takeover, not a session-length one.
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

- **No API versioning.** Routes are `/api/employees`, not `/api/v1/employees`.
  Once the mobile app is in an app store you cannot force everyone to update,
  so the first breaking change has nowhere to go. This is cheap now and
  expensive later — it belongs in 1.0.
- **No background jobs at all.** No `IHostedService` anywhere. Consequences:
  expired refresh tokens and password-reset tokens are never purged;
  `location_records` is never pruned; email and push have no retry.
- **Email is sent inside the HTTP request** (`ForgotPasswordCommand`). A slow
  or hung SMTP server blocks a request thread — MailKit's default timeout is
  two minutes.
- **`EmailSettings` and `FirebaseSettings` are bound without validation**
  (`DependencyInjection.cs:74-79`), unlike `JwtSettings` which uses
  `ValidateOnStart`. Misconfiguration is silent. See Critical #2.
- Empty-string connection string passes the null guard and fails later with an
  opaque Npgsql error.

---

## 4. Database design and scalability

**Good:** PostgreSQL-specific design done properly — filtered unique indexes
(`"IsDeleted" = false`) so a deleted employee number can be reused,
`UseIdentityAlwaysColumn`, check constraints, `bigint` identity on the
high-volume table, composite `(EmployeeId, Timestamp DESC)` index supporting
the live-map query, `jsonb` for notification payloads. Indexes exist on every
foreign key and every filtered/sorted column.

**Issues:**

- **`location_records` grows without bound.** One ping per employee per minute
  is ~1M rows/month for 100 workers, ~12M/year. Reads stay fast; backups,
  restore time, disk and vacuum do not. Needs a retention window or monthly
  partitioning **before** go-live, because retrofitting partitioning onto a
  large live table is a maintenance window, not a migration.
- **One migration in the entire history** (`InitialCreate`). The schema has
  never been evolved incrementally, so the team has never exercised the
  process that every future change depends on. No down-migration has been
  tested.
- **No backup or restore procedure exists or is documented.** Nothing has ever
  been restored. For a system holding payroll-adjacent employee records, this
  is the single most likely source of an unrecoverable incident.
- Search uses `LIKE '%term%'` over `lower(column)` — sequential scan. Fine at
  hundreds of rows; needs `pg_trgm` GIN at 10×.
- No connection pooling configuration (`AddDbContextPool`), no command timeout.
- No read replica strategy — acceptable at this scale, worth knowing.

---

## 5. API structure

53 endpoints, consistently RESTful, correct verbs and status codes, RFC 7807
problem details with a `traceId`, `JsonStringEnumConverter` so clients see
names rather than integers, Swagger documented with JWT support (dev only —
correct).

**Issues:**

- No versioning (above).
- **No HTTP-level tests.** No `WebApplicationFactory`. Authentication,
  authorization policies, rate limiting, CORS and exception→status mapping are
  exercised by no automated test — only by the ad-hoc Playwright script.
- No pagination on `GET /api/locations/current` — returns every employee's
  latest position in one response. Fine at 100, not at 1,000.
- No `ETag` / conditional requests; no response compression configured.
- No idempotency keys on POSTs — a retried "adjust stock" double-applies.
- `AllowedHosts: "*"` — host header not restricted.

---

## 6. Security risks

Recent work fixed real issues (forwarded headers, credential-endpoint rate
limiting, non-zero exit on fatal startup, migrate-on-startup default). What
remains:

| # | Risk | Severity |
|---|---|---|
| 1 | Compose ships working defaults for JWT secret and SuperAdmin password | **Critical** |
| 2 | Password-reset link written to logs when SMTP unconfigured | **Critical** |
| 3 | Login timing reveals whether an email is registered | High |
| 4 | Refresh token in `localStorage` (XSS → persistent takeover) | High |
| 5 | No account lockout — unlimited guesses at 20/min per IP | High |
| 6 | No resource-scoped authorization: any Foreman reads every employee's GPS history | High |
| 7 | Deactivating a user leaves their access token valid up to 15 min | Medium |
| 8 | No dependency/secret scanning in CI | Medium |
| 9 | `AllowedHosts: "*"`, no security headers (HSTS only in non-dev, no CSP) | Medium |
| 10 | No audit trail of who changed what | Medium |

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

**C1. Compose ships working production-capable secrets.**
`docker-compose.yml:30-32` defaults `JwtSettings__SecretKey` to
`dev-only-secret-key-change-me-…` (which is ≥32 chars, so it *passes* startup
validation) and `Seed__SuperAdmin__Password` to `Admin123!`. Anyone who runs
the documented command gets a system with a publicly known signing key —
tokens can be forged for any role — and a known admin password. Compose also
forces `ApplyMigrationsOnStartup: "true"`, overriding the safe default.
*Fix: remove the fallbacks so startup fails loudly when they are unset.*

**C2. Password-reset links are written to the log when SMTP is unconfigured.**
`SmtpEmailSender.cs:28-34` logs the full email body — containing the reset
link and token — at Warning, then returns as if sent. `EmailSettings` has no
`ValidateOnStart`, so a production deploy with a missing SMTP host silently
enters this path: users never receive resets, and anyone with log access can
take over any account.
*Fix: validate email configuration at startup in non-development; never log the body.*

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

**H1. Admin panel has zero automated tests** and the 46 end-to-end checks are
not in the repository. Commit the Playwright suite, add it to CI, add
component tests for the five CRUD sections and the auth flow. Correct the two
documentation claims that currently overstate coverage.

**H2. No authorization tests.** Add HTTP-level tests
(`WebApplicationFactory`) asserting every endpoint's role policy, plus the
401/403/429 paths. The security model is currently unverified by CI.

**H3. Centralised logging, metrics and alerting.** File logs in an ephemeral
container are effectively no logs. Ship to an aggregator, expose metrics,
add uptime and error-rate alerts, and add error tracking to both clients.

**H4. Split `/health` into liveness and readiness.** A database blip should
not cause a restart loop.

**H5. `location_records` retention or partitioning.** Must land before
production data accumulates. Ties directly to C7.

**H6. Account lockout / progressive delays** on repeated failed logins, and
**close the login timing oracle** (`LoginCommand.cs:60-66` short-circuits
password verification for unknown emails, making them measurably faster).

**H7. Move the admin refresh token out of `localStorage`** to an httpOnly,
`Secure`, `SameSite` cookie.

**H8. API versioning** (`/api/v1/…`). Cheap now; a breaking change against
installed mobile apps is very expensive later.

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

**H11. Resource-scoped authorization.** Decide and enforce whether a Foreman
should see the whole company's employees and GPS history, or only their own
projects. Currently they see everything.

---

## MEDIUM PRIORITY — maintainability and robustness

- **M1.** Background service for cleanup: expired refresh tokens, used reset
  tokens, old location records.
- **M2.** Move email and push out of the request path onto a queue with retry.
- **M3.** Validate `EmailSettings`, `FirebaseSettings` and CORS origins at
  startup, the way `JwtSettings` already is.
- **M4.** Integration tests for the still-uncovered modules (Employees,
  Projects, Vehicles, Locations). Notifications is done.
- **M5.** Audit trail — who changed what and when. Expected in workforce
  systems and hard to add retroactively.
- **M6.** Security headers (CSP, `X-Content-Type-Options`, `Referrer-Policy`),
  restrict `AllowedHosts`.
- **M7.** Dependency and secret scanning in CI (Dependabot, CodeQL, `npm audit`,
  `dotnet list package --vulnerable`).
- **M8.** React error boundaries; friendly offline/failure states in both clients.
- **M9.** Offline cache in the mobile app — construction sites have poor
  coverage and the app is currently unusable without a connection.
- **M10.** Practise an incremental migration and a rollback before the first
  schema change under load.
- **M11.** Idempotency keys on stock adjustments and assignment actions.
- **M12.** Pagination on `/api/locations/current`.
- **M13.** Load test the login path (PBKDF2 CPU cost at shift change).
- **M14.** Align app display names — "Construction Organizer" (Android) vs
  "Construction Mobile" (iOS).

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
- **L9.** Photo upload for employees — `PhotoUrl` exists on the entity but
  nothing populates it.
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
- H2 HTTP-level authorization tests for all 53 endpoints
- M4 integration tests for the uncovered modules
- Fix the two documentation claims about test coverage

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
