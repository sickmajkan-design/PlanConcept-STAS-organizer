# construction_mobile

Flutter app for field staff of the Construction Workforce Management System.
Talks to `Construction.API`.

## Stack

| Concern | Choice |
|---|---|
| State management | Riverpod (hand-written `Notifier`s — no codegen) |
| Navigation | GoRouter, with an auth-driven redirect |
| HTTP | Dio, with a token-refresh interceptor |
| Models | Freezed + json_serializable |
| Token storage | flutter_secure_storage (Keychain / EncryptedSharedPreferences) |

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
    ├── auth/
    │   ├── data/             models + repository
    │   └── presentation/     controller + screens
    └── shell/presentation/   splash and home
```

Each backend module gets its own folder under `features/`, following the same
`data/` + `presentation/` split.

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
