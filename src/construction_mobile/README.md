# construction_mobile

Flutter app for field staff of the Construction Workforce Management System.
Talks to `Construction.API`.

## Stack

| Concern | Choice |
|---|---|
| State management | Riverpod (hand-written `Notifier`s — no codegen) |
| Navigation | GoRouter, with an auth-driven redirect and a bottom-nav shell |
| HTTP | Dio, with a token-refresh interceptor |
| Models | Freezed + json_serializable |
| Token storage | flutter_secure_storage (Keychain / EncryptedSharedPreferences) |
| Location | geolocator |
| Push | firebase_core + firebase_messaging |

## Modules

| Module | Status |
|---|---|
| Authentication | sign in/out, refresh, change & forgot password |
| Employees | paged list with debounced search, status filters, detail with project assignments |
| Projects | paged list with search and status filters, detail with the crew |
| GPS tracking | position reported every 60 s, buffered while offline |
| Notifications | FCM registration, in-app inbox with unread badge, deep links |
| Vehicles / Tools / Materials | API is ready; no mobile screens yet |

## Running

```bash
flutter pub get
dart run build_runner build          # regenerate *.freezed.dart / *.g.dart
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

`10.0.2.2` is the Android emulator's alias for the host machine, and is the
default when `API_BASE_URL` is not supplied. Use your machine's LAN address
for a physical device, and an `https://` URL in production.

Development credentials seeded by the API: `admin@construction.local` /
`Admin123!` (an admin account — it has no employee link, so GPS reporting will
not be available to it once that module lands).

## Structure

```
lib/
├── main.dart                 ProviderScope + app entry
├── app.dart                  MaterialApp.router
├── core/
│   ├── config/               compile-time settings (--dart-define)
│   ├── network/              Dio setup, auth interceptor, session manager, error mapping
│   ├── router/               GoRouter and route constants
│   ├── storage/              secure session persistence
│   ├── theme/                Material 3 theme
│   ├── validation/           form rules mirroring the API's policies
│   └── widgets/              small shared UI pieces
└── features/
    ├── auth/                 models, repository, controller, screens
    ├── employees/            directory list + detail
    ├── projects/             project list + detail
    ├── location/             position reporting and its status card
    ├── notifications/        inbox, FCM registration, deep links
    └── shell/                splash, home, bottom-navigation frame
```

Each backend module gets its own folder under `features/`, following the same
`data/` + `presentation/` split. Paging, search debouncing and list rendering
live once in `core/pagination` and `core/widgets`, so every list behaves the
same way.

## Roles

The API serves the employee and project directories to Foreman and above. The
app mirrors that: a Worker is not shown those tabs, and the router refuses the
routes, so the app never presents something that would answer 403.

## Location sharing

While an **employee-linked** account is signed in, the app sends its position
once a minute. Admin accounts have no employee record, so the app never asks
them for the permission — the API would refuse their pings anyway.

Fixes that cannot be delivered (no coverage on site) are buffered and go out
with the next batch; the buffer is capped at one batch (120 fixes) so a long
outage cannot grow it without bound. The home screen always shows whether
sharing is on and when the last position reached the office.

## Push notifications

On sign-in the app registers its FCM token with the API, refreshes it when the
OS rotates it, and unregisters it during sign-out while the session is still
valid. Assignment notifications deep-link into the relevant project or
employee.

Firebase is optional for a developer build: without `google-services.json` /
`GoogleService-Info.plist` the app reports that push is not configured and the
in-app inbox keeps working.

## Authentication behaviour

- The session (access token, refresh token, user) is stored encrypted on the
  device and restored at launch; the splash screen is shown while that runs.
- `AuthInterceptor` attaches the bearer token, refreshes it proactively when it
  has expired, and on a `401` refreshes and replays the request exactly once.
- Refresh is **single-flight**: concurrent 401s share one refresh call.
- A rejected refresh token clears the session, and the router redirects to
  sign-in from wherever the user was.
- Changing the password revokes every session server-side, so the app confirms
  this in a dialog and then signs out.
- `forgot-password` always reports the same message, matching the API's
  deliberate refusal to reveal whether an address exists.

## Tests

```bash
flutter test
```

Unit tests cover the validators, the problem-details error mapping and the
session/token expiry rules; widget tests drive the real router and screens
(session restore, redirects, form validation) with an in-memory session store.

To check the models against a running API:

```bash
dart run tool/api_contract_check.dart
```
