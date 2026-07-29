import 'package:construction_mobile/app.dart';
import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/core/storage/secure_session_storage.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Stands in for the platform keystore, which is unavailable in widget tests.
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

AuthSession _storedSession() {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: 'access',
    accessTokenExpiresAt: now.add(const Duration(minutes: 15)),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: now.add(const Duration(days: 7)),
    user: const User(
      id: '019fad65-d635-76f2-880f-d8d25aea67d0',
      email: 'ivan@construction.local',
      role: 'ProjectManager',
      employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
      firstName: 'Ivan',
      lastName: 'Horvat',
    ),
  );
}

Future<void> _pumpApp(WidgetTester tester, SecureSessionStorage storage) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [sessionStorageProvider.overrideWithValue(storage)],
      child: const ConstructionApp(),
    ),
  );

  // The splash screen animates forever, so settle explicitly instead of
  // waiting for the frame queue to drain.
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 50));
}

void main() {
  testWidgets('routes to sign-in when no session is stored', (tester) async {
    await _pumpApp(tester, _InMemorySessionStorage());

    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Forgot password?'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Email'), findsOneWidget);
  });

  testWidgets('routes straight to the home screen for a stored session',
      (tester) async {
    await _pumpApp(tester, _InMemorySessionStorage(_storedSession()));

    expect(find.text('Ivan Horvat'), findsOneWidget);
    expect(find.text('ivan@construction.local'), findsOneWidget);
    // Role names arrive in PascalCase and are humanised for display.
    expect(find.text('Project Manager'), findsOneWidget);
    expect(find.text('Change password'), findsOneWidget);
  });

  testWidgets('discards a session whose refresh token has already expired',
      (tester) async {
    final expired = _storedSession().copyWith(
      refreshTokenExpiresAt:
          DateTime.now().toUtc().subtract(const Duration(days: 1)),
    );

    await _pumpApp(tester, _InMemorySessionStorage(expired));

    expect(find.text('Sign in'), findsOneWidget);
  });

  testWidgets('validates the sign-in form before calling the API',
      (tester) async {
    await _pumpApp(tester, _InMemorySessionStorage());

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();

    expect(find.text('Email is required.'), findsOneWidget);
    expect(find.text('Password is required.'), findsOneWidget);
  });

  testWidgets('rejects a malformed email address without a request',
      (tester) async {
    await _pumpApp(tester, _InMemorySessionStorage());

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Email'),
      'not-an-email',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Password'),
      'Gradnja123',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();

    expect(find.text('Enter a valid email address.'), findsOneWidget);
  });

  testWidgets('opens the password reset screen from the sign-in screen',
      (tester) async {
    await _pumpApp(tester, _InMemorySessionStorage());

    await tester.tap(find.text('Forgot password?'));
    await tester.pumpAndSettle();

    expect(find.text('Reset password'), findsOneWidget);
    expect(find.text('Send reset link'), findsOneWidget);
  });
}
