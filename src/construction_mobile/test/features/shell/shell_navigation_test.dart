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
}
