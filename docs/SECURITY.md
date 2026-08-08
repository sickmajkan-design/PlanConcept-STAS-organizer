# Security review — STAS Organizer

**Date:** 2026-08-02
**Scope:** authentication, authorization, application security, configuration
**Method:** source review of all three codebases, plus exploitation against a
running instance. Every finding marked *proven* was demonstrated before it was
fixed and re-tested afterwards.

---

## What was already sound

Recording this first, because it shaped where the effort went:

- **Password storage** — PBKDF2-HMAC-SHA256, 100,000 iterations, per-password
  random salt, iteration count stored with the hash so it can be raised later.
  Comparison uses `CryptographicOperations.FixedTimeEquals`.
- **Refresh tokens** — 64 random bytes, stored only as a SHA-256 hash, rotated
  on every use, with reuse detection that revokes every session for the account.
- **Password reset** — token hashed at rest, bound to the email, single-use,
  one-hour expiry, revokes all sessions on use, and the request endpoint is
  enumeration-safe (always 202).
- **JWT validation** — issuer, audience, lifetime and signing key all
  validated, 30-second clock skew, `MapInboundClaims = false`.
- **SQL injection** — no raw SQL anywhere in the solution. All queries go
  through EF Core with parameters.
- **XSS** — no `dangerouslySetInnerHTML`, no `innerHTML`, no `eval` in the
  admin panel. React escapes by default.
- **CSRF** — not applicable: the API authenticates with a bearer header, never
  a cookie, so a cross-site form post carries no credentials.
- **Object-level authorization** — notification and device-token operations are
  scoped to the calling user's id, so one user cannot read or delete another's.
- **Mass assignment** — commands map named fields explicitly; `Role` and
  `IsActive` are not bindable from any request body.
- **Mobile token storage** — Keychain / EncryptedSharedPreferences via
  `flutter_secure_storage`.

---

## Vulnerabilities found and fixed

### 1. Compose shipped a working signing key and admin password — critical

`docker-compose.yml` defaulted `JwtSettings__SecretKey` to
`dev-only-secret-key-change-me-0123456789abcdef` and
`Seed__SuperAdmin__Password` to `Admin123!`. The key is 44 characters, so it
**passed** the ≥32-character startup validation. Anyone running the documented
command got a system whose signing key is public: an attacker can mint a valid
token for any user in any role without touching the login endpoint. Compose
also forced `ApplyMigrationsOnStartup: "true"`, overriding the safe default.

**Fixed.** Every secret now uses `${VAR:?message}`, so compose refuses to start
until it is set. `.env.example` keeps local development one `cp` away, and
`APPLY_MIGRATIONS_ON_STARTUP` defaults to false.

### 2. Password-reset links were written to the log — critical

`SmtpEmailSender` logged the full HTML body at warning level when no SMTP host
was configured, then returned as though the mail had been sent. The body
contains the reset link, which is equivalent to the account's password for the
next hour. Nothing forced SMTP to be configured, so a production deploy could
enter this path silently: users never receive resets, and anyone with log
access can take over any account.

**Fixed.** The body is no longer logged. `StartupValidationExtensions` now
fails startup outside Development when SMTP is unconfigured, unless
`EmailSettings:AllowUnconfigured` is explicitly set — so running without
password recovery becomes a recorded decision rather than an accident. The same
check rejects a reset URL that is missing, non-absolute, non-HTTPS, or still
pointing at localhost.

### 3. Rate limiting could be bypassed with one header — high, *proven*

The sign-in limiter partitions on the client address. Forwarded headers were
configured with `KnownNetworks.Clear()` and `KnownProxies.Clear()`, which meant
any caller could set `X-Forwarded-For` and choose its own partition.

Demonstrated against a running instance: after exhausting the limit from one
address, 25 further attempts with a rotating header were **all accepted**,
giving unlimited password guessing. The same header also poisoned the
refresh-token audit trail — a login sent with `X-Forwarded-For:
198.51.100.77` was recorded under that address.

**Root cause worth knowing.** ASP.NET Core's middleware cannot express "trust
nobody". Its check is:

```csharp
if (KnownProxies.Count + KnownNetworks.Count > 0 && !CheckKnownAddress(remoteIp))
    break;   // reject the header
```

With both lists empty the first term is false, so the guard never runs and the
header is applied to **every** caller. Clearing the lists does the exact
opposite of what it looks like — which is how this shipped.

**Fixed.** `Network:TrustedProxies` lists the addresses whose header may be
believed. When it is empty the middleware is **left out of the pipeline
entirely**, which is the only way to get "trust nobody". Startup logs which
mode is active. After the fix the same attack produces 20 × 401 then 10 × 429,
and the audit trail records the connection address.

### 4. Sign-in timing revealed which addresses have accounts — high, *proven*

`user is null || !Verify(...)` short-circuited, so an unknown address skipped
the 100,000-iteration derivation entirely. Measured on a live instance:
**81.1 ms for a real account vs 6.1 ms for an unknown one — a 13.3× difference**,
reliable enough to enumerate the whole staff directory from single samples.

**Fixed.** `IPasswordHasher.DummyHash` is a real hash of a value nobody knows;
the handler verifies against it when no account matched, so both paths cost a
full derivation. Re-measured after the fix: **83.1 ms vs 85.0 ms — 0.98×.**

### 5. No account lockout — high

Guessing was bounded only by the (bypassable) per-address rate limit. Nothing
tracked failures per account.

**Fixed.** `FailedLoginAttempts` and `LockoutEndsAt` on `users` (migration
`AddLoginLockout`): 10 consecutive failures lock the account for 15 minutes.
Tracked per account on purpose — an attacker picks their own source address but
not the account they are attacking. The lockout is short and self-clearing so
the denial-of-service it enables against a known user is bounded, and a
successful password reset clears it so the recovery path still works.

Lockout is reported with the **same message** as a wrong password. A distinct
"account locked" message would confirm that an address exists, reopening the
enumeration hole fix 4 closes. This is asserted by a test.

### 6. No security response headers — high

No CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` or
`Permissions-Policy`. This matters more than usual because the admin panel
keeps its refresh token in `localStorage`, where any script on the page can
read it.

**Fixed.** `SecurityHeadersMiddleware` sets all five. The API serves JSON and
never markup, so its policy is `default-src 'none'; frame-ancestors 'none';
base-uri 'none'; form-action 'none'`. Swagger UI is a real page and gets a
looser policy on its own path; it is only mapped in Development.

### 7. Search terms were interpreted as LIKE patterns — medium

`$"%{search}%"` passed user input straight into a `LIKE` pattern. Not SQL
injection — the value is a parameter — but it *is* user-controlled pattern
syntax: searching for `%` returned every row regardless of intent, and a term
of many wildcards turns an already-sequential scan into a far more expensive
one.

**Fixed, then fixed properly.** `SearchPattern.Contains` escapes `\`, `%` and
`_` (backslash first, or the escape characters would themselves be escaped).

The first fix was incomplete in a way the original check could not see. EF
Core's two-argument `EF.Functions.Like` translates to
`LIKE @pattern ESCAPE ''` — an empty escape clause, which turns escaping
*off*. Every backslash the helper added reached PostgreSQL as an ordinary
character to be matched literally. `?search=%` did return 0 rows instead of the
whole table, which is what was checked at the time and why this passed as
done — but it returned 0 because the pattern had become "contains a
backslash", not because the `%` was escaped. Legitimate searches for a term
containing `%`, `_` or `\` returned nothing at all.

The call sites now pass `SearchPattern.Escape` explicitly, so the SQL reads
`ESCAPE '\'`. Four secondary filters — project client, tool category, employee
position, material warehouse — were also building patterns inline without
escaping anything, and now go through the same helper.

Verified against a real database rather than by reading the pattern: a search
for `%` matches only rows containing a literal `%`, a search for `A_B` does not
match `AXB`, and a plain term is unchanged. The observable symptom of the
regression was a search that quietly found nothing, so a test that only checked
"fewer rows than everything" would have kept passing.

---

## Accepted risks and deliberate trade-offs

These were considered and **not** changed. Each is a decision, not an oversight.

| Risk | Why it stands |
|---|---|
| **Refresh token in `localStorage`** (admin) | Moving it to an http-only cookie changes the client/server auth contract and needs CSRF defences added alongside. It is the right end state but is a coordinated change, not a patch. The CSP in fix 6 is the control that stops injected script running in the first place. **Recommended as the next security work item.** |
| **Any Foreman can read every employee's GPS history** | Role-based only, no per-project scoping. This is a product decision about what a foreman should see, and it needs the client's answer before the authorization model changes. Raised in the readiness audit. |
| **A device token can be re-registered to another user** | Deliberate: shared phones are normal on site, and handoff must work. Worst case is misdirected notifications, not data theft. |
| **Deactivated accounts get a distinct message** | Only reachable *after* the correct password is supplied, so it tells an attacker nothing they do not already have. |
| **Access token stays valid up to 15 minutes after deactivation** | Inherent to stateless JWTs. A revocation list would trade a database read per request for it; 15 minutes is an acceptable window at this scale. |
| **`AllowedHosts: "*"`** | Host filtering belongs at the reverse proxy for this deployment shape. Listed in the checklist below instead. |

## Offboarding — closed

This review originally recorded that accounts existed only via seeding, so a
departing employee could not be deprovisioned except through direct database
access. `/api/users` now covers it, and deactivation is a real revocation
rather than a flag:

| Step | Why it is part of offboarding |
|---|---|
| `IsActive = false` | Blocks sign-in. |
| Every active refresh token revoked | The refresh token is what outlives the 15-minute access token. Without this, "offboarded" would mean nothing for another seven days. |
| Outstanding password-reset tokens marked used | A link already sitting in their inbox must not become a way back in. |
| Device registrations deleted | Push is delivered to a device, not through an access check, so a leftover registration keeps sending project notifications to someone who has left. |

Verified end to end against a running instance: after deactivation, sign-in and
refresh both return 401, the device-token row is gone, and no active refresh
token remains.

**Residual, by design.** An access token already issued stays valid until it
expires — measured at up to 15 minutes. This is inherent to stateless JWTs;
removing it means a revocation check per request. For a same-day departure
where minutes matter, revoke at the proxy or shorten
`JwtSettings:AccessTokenLifetimeMinutes`.

**Privilege escalation is bounded by rank.** `RoleAdministration` allows acting
only strictly below your own role, with Super Admin able to act on peers so a
compromised Super Admin can still be removed. Verified live: an Admin creating
a Super Admin or another Admin is refused with 403, while Project Manager,
Foreman and Worker succeed; an Admin deactivating a Super Admin is refused; a
Worker cannot reach the endpoints at all. The full role matrix is asserted
exhaustively in `RoleAdministrationTests`.

**Two lockout protections.** Nobody can deactivate their own account, and the
last active Super Admin cannot be deactivated or demoted — otherwise an
administrator can lock everyone out and the only repair is database access.

---

## Production security checklist

### Before first deploy

- [ ] `JWT_SECRET_KEY` generated per environment (`openssl rand -base64 48`), never committed, never shared between environments
- [ ] `SUPERADMIN_PASSWORD` set to a unique value, and **changed at first sign-in**
- [ ] `POSTGRES_PASSWORD` set to a unique value; database not reachable from the public internet
- [ ] `.env` present and git-ignored; confirm `.env.example` contains no real secret
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (this is what enables the startup checks, HSTS and HTTPS redirection, and disables Swagger)
- [ ] `Database__ApplyMigrationsOnStartup=false`; migrations run as a deploy step
- [ ] `Cors__AllowedOrigins__0` set to the admin panel's real origin — never `*`, and written exactly as the browser sends it: `https://admin.example.com`, with no trailing slash and no path. The API refuses to start on anything else, and says what to write instead.
- [ ] `ClientApp__PasswordResetUrl` is an HTTPS URL on the real domain
- [ ] SMTP configured, or `EmailSettings__AllowUnconfigured=true` set as a conscious decision
- [ ] `Network__TrustedProxies` set to the reverse proxy's address — and **left empty if there is no proxy**
- [ ] TLS terminated in front of the API; HTTP redirects to HTTPS
- [ ] Reverse proxy restricts `Host` to the real domain
- [ ] Firebase credentials provisioned, or push accepted as non-functional

### Verify after deploy

- [ ] `curl -I https://<api>/health` returns `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`
- [ ] `/swagger` returns 404
- [ ] 21 rapid sign-in attempts return `429` — **and still do when `X-Forwarded-For` is rotated**
- [ ] 10 wrong passwords lock the account for 15 minutes
- [ ] A sign-in for an unknown address takes the same time as one for a known address
- [ ] The refresh-token audit trail records real client addresses, not the proxy and not a spoofed value
- [ ] A password-reset email actually arrives, and the link opens the real admin panel
- [ ] Logs contain no reset links, passwords or tokens
- [ ] The seeded Super Admin password has been changed

### Ongoing

- [ ] Database backups running, and a restore actually tested
- [ ] Dependency scanning in CI (`dotnet list package --vulnerable`, `npm audit`, Dependabot)
- [ ] Log aggregation with alerting on the 5xx rate and on repeated lockouts
- [ ] Secret rotation procedure written down, including what breaks when the JWT key rotates (all sessions)
- [ ] GDPR obligations for continuous location tracking addressed — lawful basis, privacy notice, retention limit, erasure path (see the readiness audit)
- [ ] Offboarding procedure written down: deactivate in **User accounts**, which revokes sessions, reset links and device registrations (an already-issued access token still lasts up to 15 minutes)

---

## Test coverage for these controls

| Control | Covered by |
|---|---|
| Failure counting, lockout threshold, lockout blocks a correct password | `LoginHardeningTests` |
| Lockout and unknown-address responses are indistinguishable from a wrong password | `LoginHardeningTests` |
| Successful sign-in and password reset clear a lockout | `LoginHardeningTests` |
| `DummyHash` is a real 100,000-iteration hash | `LoginHardeningTests` |
| LIKE wildcard escaping (`%`, `_`, `\`), against a real database | `EmployeeTests`, `ProjectTests` |
| Erasure removes the GPS track, coordinates and absence reasons; keeps hours and rates | `ErasureTests` |
| Erasing one person leaves every other person's data untouched | `ErasureTests` |
| A foreman sees only their own crews' positions and movement history | `LocationTests` |
| An out-of-scope employee is 404, not 403, so existence is not confirmed | `LocationTests` |
| Password hashes and anything credential-shaped stay out of the audit trail | `AuditTrailTests` |
| The audit trail records who acted, as they were at the time, and outlives the record | `AuditTrailTests` |
| Empty proxy configuration yields no trusted proxy; invalid entries fail loudly | `TrustedProxyConfigurationTests` |
| Refresh rotation, reuse detection, session revocation | `AuthenticationTests` |
| The role matrix for who may administer whom | `RoleAdministrationTests` |
| Offboarding revokes sessions, reset links and device tokens | `UserManagementTests` |
| Self-deactivation and last-Super-Admin protection | `UserManagementTests` |
| A role change ends sessions carrying the old role | `UserManagementTests` |

The pipeline-level controls — rate limiting, the security headers, and the
forwarded-header decision — have **no automated coverage**, because the
solution has no HTTP-level test host. They were verified by hand against a
running instance for this review. Adding `WebApplicationFactory` tests is the
way to keep them verified, and is the second item in the readiness audit.
