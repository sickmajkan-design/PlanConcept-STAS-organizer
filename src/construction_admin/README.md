# construction_admin

React admin console for office staff of the Construction Workforce Management
System. Talks to `Construction.API`.

## Stack

| Concern | Choice |
|---|---|
| Build tool | Vite (React + TypeScript template) |
| UI | Material UI v9 |
| Data fetching / caching | TanStack Query |
| Routing | React Router v7, with an auth-driven redirect |
| Forms | react-hook-form + Zod |
| HTTP | Axios, with a token-refresh interceptor |
| Live map | `@vis.gl/react-google-maps` |
| Lint | oxlint |

## Dependency notes

`npm audit` reports a high-severity React Router advisory
([GHSA-qwww-vcr4-c8h2](https://github.com/advisories/GHSA-qwww-vcr4-c8h2))
against the installed 7.18.2. It is a **CSRF bypass in RSC mode**, and this
app is a plain client-rendered SPA that never enables RSC, so the vulnerable
code path is not reachable here.

The pin is deliberate rather than neglected: `npm audit fix --force`
downgrades to 7.11.0, which carries its own separate advisories, so the
downgrade trades a non-applicable finding for applicable ones. Revisit when
an upstream 7.x release fixes it without going backwards.

## Running

```bash
npm install
cp .env.example .env      # set VITE_API_BASE_URL and, optionally, a Maps key
npm run dev
```

The dev server listens on port 5173 by default, which matches the API's
development CORS allow-list (`Cors:AllowedOrigins` in
`Construction.API/appsettings.Development.json`). Using a different port
requires adding it there too.

Seeded development credentials: `admin@construction.local` / `Admin123!`
(or whatever the API's `Seed:SuperAdmin:*` configuration specifies).

## Scripts

```bash
npm run dev       # dev server with HMR
npm run build     # tsc -b && vite build (type-checks, then bundles)
npm run preview   # serves the production build locally
npm run lint       # oxlint
```

## Structure

```
src/
├── main.tsx                  ThemeProvider + QueryClientProvider + Router + AuthProvider
├── App.tsx                   route tree; heavy pages are code-split with React.lazy
├── theme.ts                  Material UI theme
├── queryClient.ts             TanStack Query defaults (no retry on 4xx)
├── config.ts                  VITE_* environment variables
├── api/                       Axios client, auth/session handling, typed endpoint calls
├── auth/                      AuthContext/AuthProvider, useAuth, role-check helpers
├── routes/                    route path constants, RequireAuth/RequireGuest/RequireDirectoryAccess
├── layout/                    AppLayout — responsive drawer + top bar
├── components/                shared widgets: StatusChip, ConfirmDialog, PagedList-agnostic bits
├── features/
│   ├── employees/             TanStack Query hooks + Zod validation for Employees
│   ├── projects/              same, for Projects
│   ├── vehicles/               same, for Vehicles
│   ├── tools/                  same, for Tools (dual employee + project assignment)
│   └── materials/              same, for Materials (plus the adjust-stock mutation)
└── pages/
    ├── auth/                  sign in, forgot/reset/change password
    ├── employees/              list, detail, create/edit form
    ├── projects/                list, detail, create/edit form
    ├── vehicles/                list, detail with assign/unassign, create/edit form
    ├── tools/                   list, detail with dual assign/unassign, create/edit form
    ├── materials/               list, detail with an adjust-stock dialog, create/edit form
    └── map/                    live employee locations
```

## Authentication behaviour

Mirrors the mobile app's approach on the web:

- Tokens are kept in `localStorage`, refreshed proactively (30 s before
  expiry) and reactively on a `401`, with a **single-flight** refresh so
  concurrent requests share one call.
- Changing the password revokes every session server-side; the UI explains
  this and signs the operator out.
- `forgot-password` always shows the same message, matching the API's
  refusal to reveal whether an address exists.

## Role-aware UI

The employee/project/vehicle/tool/material directory is only served by the
API to `SuperAdmin` / `Admin` / `ProjectManager` / `Foreman`. The admin app
mirrors this rather than discovering it via 403s:

- The navigation drawer omits those links for a `Worker`.
- `RequireDirectoryAccess` redirects a direct visit to any of those routes
  back to the home page for a role that cannot use them.

Create/edit/delete and the assignment actions are not further gated in the
UI beyond that — the API enforces the finer-grained roles for those
(`AdminAndAbove` for create/update/delete, `ProjectManagerAndAbove` or
`ForemanAndAbove` for assignment, depending on the resource) and a refusal
surfaces as the same error banner used everywhere else in the app.

## Tools' dual assignment

A tool can be held by an employee, placed on a project, both, or neither —
the detail page shows an assignment card for each and lets them be set or
cleared independently, matching the API's separate assign/unassign endpoints
for each target.

## Materials stock

The create/edit form sets an absolute quantity. Day-to-day stock movements
(deliveries, consumption) go through "Adjust stock" on the detail page
instead, which posts a relative change to `POST /api/materials/{id}/adjust`;
the API rejects an adjustment that would take the quantity negative with a
409, shown inline in the dialog.

## Live map

`/map` shows every employee's last reported position (from
`GET /api/locations/current`, refreshed every 30 s) with an optional project
filter. Without `VITE_GOOGLE_MAPS_API_KEY` configured, the page explains
what is missing instead of crashing — the rest of the app is fully usable
without a Maps key.

## Notifications

`/notifications` is the signed-in operator's own inbox — the same rows the
mobile app reads, since a notification is stored per recipient rather than per
device. It is reachable from the bell in the top bar rather than from the
navigation drawer, because it is personal and its unread count has to be
visible from any screen.

The browser has no push channel, so the badge polls
`GET /api/notifications/unread-count` once a minute and on window focus. That
is one integer over the wire; the alternative is a count that only moves on a
page reload.

Admin and above additionally get "Send an announcement", which posts to
`POST /api/notifications/announce` with optional role and project-crew
filters — they narrow together, so "the foremen on the Danube job" is one
send. The endpoint answers with the number of people reached and the screen
shows it: an announcement whose filters matched nobody otherwise looks exactly
like one that reached the whole company.

## Tests

```bash
npm test          # Vitest, once
npm run test:watch
```

Tests sit next to the code they cover, as `*.test.ts(x)`. They run in Node by
default; a file that needs a DOM opens with a `@vitest-environment jsdom`
docblock, so the pure-logic majority does not pay for one.

What is covered, chosen by where a mistake would be silent rather than by
counting files:

| Area | Why it is here |
|---|---|
| `api/client` | Token refresh against a fake axios adapter: the bearer header, the pre-emptive renewal, the 401-and-replay, and the single-flight property. The API rotates refresh tokens and treats a replayed one as theft, so a refresh that fired once per in-flight request would sign the operator out mid-task. |
| `api/session` | What survives a reload, and the 30-second skew that stops a request being sent with a token that dies in flight. |
| `api/apiError` | Which message wins. The API's own explanation must beat the generic fallback, or every conflict reads "The action conflicts with the current data". |
| `routes/RequireAuth` | Every guard against every role, plus the still-loading third state — treating it as signed-out bounces a returning operator to the login screen on each reload. |
| `i18n` | Serbian's three plural forms, interpolation, which language the app opens in, and a parity check that no translation lost a `{placeholder}`. |
| `api/resource` | Query-parameter normalisation. A dropped filter is a screen that quietly returns the wrong rows. |
| `pages/schedule`, `pages/costs` | Date arithmetic in `YYYY-MM-DD` strings, including the timezone and daylight-saving cases that would shift a bar or a report by a day. |
| `pages/employees` | Two whole screens — a form and a list — rendered under the real providers against a fake network: validation, the payload actually sent, a server field error reaching the right input, the edit-load, grid rows, debounced search, and the delete confirmation. This is what found the delete button that navigated instead of deleting. |
| `components/ErrorBoundary` | A component that throws on purpose, to prove the boundary shows the fallback, that the children are gone, that a reset re-renders them, and that a changed route clears the error. |
| `components/OfflineBanner` | `navigator.onLine` flipped under the component: the banner appears on a drop, appears when the screen opens already offline, confirms the reconnect, and does not congratulate a connection that never dropped. |

Not covered: anything only a browser can show. The screen tests run in jsdom,
where every element has zero height — enough for the assertions above, but
blind to layout, real scrolling and the grid's virtualisation. The CRUD forms
and grids have been driven end-to-end with Playwright scripts covering sign-in,
employee and project CRUD, project assignment, vehicle/tool/material CRUD, tool
dual assignment, material stock adjustment (including the 409 over-consumption
case), role-gated navigation and the live map's graceful no-API-key state — but
those scripts are not in this repository and do not run in CI.

## Verification

`npm run lint` (oxlint), `npm test` (Vitest) and `npm run build`, which
type-checks with `tsc -b` before bundling. `tsconfig.app.json` includes the
whole of `src`, so the tests are type-checked with everything else. CI runs all
three on every push.
