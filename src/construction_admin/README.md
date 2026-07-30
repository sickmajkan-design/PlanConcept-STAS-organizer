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
│   └── projects/              same, for Projects
└── pages/
    ├── auth/                  sign in, forgot/reset/change password
    ├── employees/              list, detail, create/edit form
    ├── projects/                list, detail, create/edit form
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

The employee/project directory is only served by the API to
`SuperAdmin` / `Admin` / `ProjectManager` / `Foreman`. The admin app mirrors
this rather than discovering it via 403s:

- The navigation drawer omits the Employees/Projects links for a `Worker`.
- `RequireDirectoryAccess` redirects a direct visit to `/employees` or
  `/projects` back to the home page for a role that cannot use them.

## Live map

`/map` shows every employee's last reported position (from
`GET /api/locations/current`, refreshed every 30 s) with an optional project
filter. Without `VITE_GOOGLE_MAPS_API_KEY` configured, the page explains
what is missing instead of crashing — the rest of the app is fully usable
without a Maps key.

## Verification

Type-checked with `tsc -b` (part of `npm run build`), linted with `oxlint`,
and driven end-to-end with a Playwright script covering sign-in, employee and
project CRUD, project assignment, role-gated navigation, and the live map's
graceful no-API-key state — against both the dev server and the production
build.
