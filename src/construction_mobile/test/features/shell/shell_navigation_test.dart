import 'package:construction_mobile/app.dart';
import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/core/storage/secure_session_storage.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/notifications/presentation/notifications_controller.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _InMemorySessionStorage implements SecureSessionStorage {
  _InMemorySessionStorage([this._session]);

  AuthSession? _session;

  @override
  Future<AuthSession?> read() async => _session;

  @override
  Future<void> write(AuthSession session) async => _session = session;

  @override
  Future<void> clear() async => _session = null;
}

AuthSession _sessionFor(String role, {bool linkedToEmployee = true}) {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: 'access',
    accessTokenExpiresAt: now.add(const Duration(minutes: 15)),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: now.add(const Duration(days: 7)),
    user: User(
      id: '019fad65-d635-76f2-880f-d8d25aea67d0',
      email: 'user@construction.local',
      role: role,
      employeeId:
          linkedToEmployee ? '019fad73-e894-791b-a6c3-715bddf61164' : null,
      firstName: 'Ivan',
      lastName: 'Horvat',
    ),
  );
}

Future<void> _pumpSignedIn(
  WidgetTester tester,
  AuthSession session, {
  int unread = 0,
}) async {
  // Home is a lazily built ListView, so anything past the default 600pt test
  // surface is simply not constructed and a finder reports it missing whether
  // the screen offers it or not. A tall surface makes "offered" and "not
  // offered" mean what the assertions below say they mean.
  tester.view.physicalSize = const Size(1080, 3600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        sessionStorageProvider
            .overrideWithValue(_InMemorySessionStorage(session)),
        // The badge count would otherwise reach for the network.
        unreadNotificationCountProvider.overrideWith((ref) async => unread),
      ],
      child: const ConstructionApp(),
    ),
  );

  await tester.pump();
  await tester.pump(const Duration(milliseconds: 50));
}

void main() {
  testWidgets('offers the directory tabs to a Foreman', (tester) async {
    await _pumpSignedIn(tester, _sessionFor('Foreman'));

    expect(find.widgetWithText(NavigationDestination, 'Home'), findsOneWidget);
    expect(
      find.widgetWithText(NavigationDestination, 'Employees'),
      findsOneWidget,
    );
    expect(
      find.widgetWithText(NavigationDestination, 'Projects'),
      findsOneWidget,
    );
    expect(find.widgetWithText(NavigationDestination, 'Alerts'), findsOneWidget);
  });

  testWidgets('hides the directory tabs from a Worker', (tester) async {
    // The API answers 403 for these endpoints, so the app must not offer them.
    await _pumpSignedIn(tester, _sessionFor('Worker'));

    expect(find.widgetWithText(NavigationDestination, 'Home'), findsOneWidget);
    expect(find.widgetWithText(NavigationDestination, 'Alerts'), findsOneWidget);
    expect(find.widgetWithText(NavigationDestination, 'Employees'), findsNothing);
    expect(find.widgetWithText(NavigationDestination, 'Projects'), findsNothing);
  });

  testWidgets('shows the unread badge on the alerts tab', (tester) async {
    await _pumpSignedIn(tester, _sessionFor('Admin'), unread: 4);
    await tester.pump();

    expect(find.text('4'), findsOneWidget);
  });

  testWidgets('does not offer location sharing to a non-employee account',
      (tester) async {
    // Admin accounts have no employee link, so the API would refuse their
    // pings; the app does not even ask for the permission.
    await _pumpSignedIn(
      tester,
      _sessionFor('Admin', linkedToEmployee: false),
    );

    expect(find.textContaining('Location sharing'), findsNothing);
  });

  testWidgets('offers vehicles, tools and materials to a Foreman on Home',
      (tester) async {
    await _pumpSignedIn(tester, _sessionFor('Foreman'));

    expect(find.text('Vehicles'), findsOneWidget);
    expect(find.text('Tools'), findsOneWidget);
    expect(find.text('Materials'), findsOneWidget);
    expect(find.text('Scan or look up'), findsOneWidget);
  });

  testWidgets('offers vehicle costs to a Foreman but not to a Worker',
      (tester) async {
    // Matches the API's CanRecordSpending: the person filling the tank is the
    // one who knows what it cost, and a Worker is not that person.
    await _pumpSignedIn(tester, _sessionFor('Foreman'));
    expect(find.text('Vehicle costs'), findsOneWidget);
  });

  testWidgets('hides vehicle costs from a Worker', (tester) async {
    await _pumpSignedIn(tester, _sessionFor('Worker'));
    expect(find.text('Vehicle costs'), findsNothing);
  });

  testWidgets('offers a Worker their own schedule and time off',
      (tester) async {
    // Both endpoints are open to every employee-linked account: the API
    // narrows them to the caller's own line rather than refusing them.
    await _pumpSignedIn(tester, _sessionFor('Worker'));

    expect(find.text('My schedule'), findsOneWidget);
    expect(find.text('Time off'), findsOneWidget);
  });

  testWidgets('offers no schedule to an account with no employee record',
      (tester) async {
    // Nothing to show, and the same reasoning as the shift screen: an account
    // with no employee has no schedule and no leave to ask for.
    await _pumpSignedIn(
      tester,
      _sessionFor('Admin', linkedToEmployee: false),
    );

    expect(find.text('My schedule'), findsNothing);
    expect(find.text('Time off'), findsNothing);
  });

  testWidgets(
      'hides vehicles, tools and materials from a Worker, but keeps scanning',
      (tester) async {
    // The API answers 403 for the directory-gated list endpoints, but
    // `by-qr` is intentionally open to every employee.
    await _pumpSignedIn(tester, _sessionFor('Worker'));

    expect(find.text('Vehicles'), findsNothing);
    expect(find.text('Tools'), findsNothing);
    expect(find.text('Materials'), findsNothing);

    // Scanning replaced the typed-code lookup, and stays open to everyone for
    // the same reason that did: the API's `by-qr` serves any employee, and a
    // worker holding a tool is exactly who needs to ask what it is.
    expect(find.text('Scan or look up'), findsOneWidget);
  });
}
