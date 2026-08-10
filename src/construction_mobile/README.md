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
| Vehicles / Tools / Materials | read-only list + detail, reachable from the Home screen's Resources section |
| Tool lookup by QR code | manual code entry, open to every signed-in employee (not just the directory roles) |

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
│   ├── network/              Dio setup, auth interceptor, session manager, error mapping, offline cache
│   ├── router/               GoRouter and route constants
│   ├── storage/              secure session persistence
│   ├── theme/                Material 3 theme
│   ├── validation/           form rules mirroring the API's policies
│   └── widgets/              small shared UI pieces
└── features/
    ├── auth/                 models, repository, controller, screens
    ├── employees/            directory list + detail
    ├── projects/             project list + detail
    ├── vehicles/              vehicle list + detail, with employee assignment shown
    ├── tools/                 tool list + detail, dual assignment shown, QR lookup screen
    ├── materials/             stock list + detail
    ├── location/             position reporting and its status card
    ├── notifications/        inbox, FCM registration, deep links
    └── shell/                splash, home, bottom-navigation frame
```

Each backend module gets its own folder under `features/`, following the same
`data/` + `presentation/` split. Paging, search debouncing and list rendering
live once in `core/pagination` and `core/widgets`, so every list behaves the
same way.

## Roles

The API serves the employee, project, vehicle, tool and material directories
to Foreman and above. The app mirrors that: a Worker is not shown those
sections, and the router refuses the routes, so the app never presents
something that would answer 403.

The one exception is tool lookup by QR code: the API opens `GET
/api/tools/by-qr/{code}` to every authenticated employee (`AllEmployees`
policy) so a crew member without directory access can still identify a tool
on site. The app mirrors that too — "Look up a tool" stays on the Home screen
for every role, including a Worker.

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

Unit tests cover the validators, the problem-details error mapping, the
session/token expiry rules and every model's JSON parsing; widget tests drive
the real router and screens (session restore, redirects, form validation,
role-gated navigation) with an in-memory session store.

To check the models against a running API:

```bash
dart run tool/api_contract_check.dart
```

The contract check covers authentication, employees, projects, vehicles,
tools (including the QR lookup), materials, role gating, notifications and
GPS reporting against a live API instance.

## Writes that must not happen twice

Recording a vehicle expense sends an `Idempotency-Key`. The key is created with
the sheet and kept until the send succeeds, deliberately not regenerated on a
retry: a failed send may well have reached the server and lost its answer on
the way back, and pressing the button again with a fresh key would book the
fuel twice.

`newIdempotencyKey()` in `core/network/idempotency.dart` produces one;
`postJson`/`postVoid` take it as an optional argument. Add it to any write
whose effect is additive. See the README at the repository root for what the
API does with it.

## Failure states

A failure carries an `ApiFailureKind` — offline, timeout, forbidden, conflict
and the rest — rather than a finished sentence. The network layer has no
`BuildContext` and no locale, so anything it wrote would be English in front of
a Serbian foreman; `describe(l10n)` in `core/l10n/api_failure_text.dart` turns
the kind into a sentence where the language is known, and a failure sitting in
a controller's state re-reads in the new language if the operator switches it.

Two rules follow the kind. The icon: being out of signal looks like being out
of signal, which is the state a site phone spends most of its day in. And
whether "try again" appears at all — it does not for a 403, because the same
request will be refused again and a button that does nothing twice is worse
than no button.

The one exception is text the server wrote itself. A `detail` naming the
employee number that clashed is worth more than a translated generality, so it
survives; a `title` ("Conflict", "An unexpected error occurred") is a status
phrase and does not count as the server having said anything.

## Working without a signal

Sites have holes in their coverage — a lift shaft, a retaining wall, half the
villages the crew drives through. `OfflineCacheInterceptor` keeps the last good
answer to every read and serves it when the phone cannot reach the server, so
"which four people are on my crew today" still has an answer in a place where
nothing else does.

It sits **below** the repositories and **after** the auth interceptor. Below,
because there are twelve repositories and one rule about what a screen may show
with no signal, and a rule repeated twelve times is a rule forgotten on the
thirteenth. After, because a 401 is the auth interceptor's business — it
refreshes and replays, and a request about to succeed must not be answered from
a file.

Four rules keep it honest:

* **Only connectivity failures fall back.** A 403 is not a coverage hole, it is
  the server saying no; answering it from a copy taken while the permission
  still applied would be a way of ignoring that. Same for 404 after a deletion
  and for 500.
* **Reads only, and not all of them.** GET, never `/auth/*` — who is signed in
  is not a question a file gets to answer — and never attachment content, which
  is bytes and would evict every list in one photograph.
* **The user is told.** `OfflineDataBanner` shows the moment the oldest thing on
  screen was saved. "Offline" alone leaves a foreman guessing whether a roster
  is from the yard this morning or from Tuesday.
* **It belongs to a session.** Site phones get handed around, so the cache is
  emptied on every change of user — sign-out, expiry, a different account. A
  token refresh is not a change of user and does not empty it; doing so would
  throw the cache away several times a shift, which is exactly when a phone in
  poor coverage needs it.

Bounds: 7 days, 200 entries, 512 KB each; personal data is discussed in
`docs/PRIVACY.md` §1.2. If the platform will not hand over a directory — or
does not answer within five seconds — the app runs with no cache rather than
not at all.
