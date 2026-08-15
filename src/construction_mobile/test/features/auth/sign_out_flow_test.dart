import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:construction_mobile/app.dart';
import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:construction_mobile/core/storage/secure_session_storage.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Pressing Sign out signs you out.
///
/// It did not. On a real handset the button did nothing at all: `signOut`
/// asked the push controller for this device's token, the push controller
/// watches who is signed in, and Riverpod refused the resulting cycle with a
/// `CircularDependencyError` thrown on the first line — before the session was
/// cleared, before anything was revoked, into an `await` nobody was watching.
/// Every test in this file presses the button; none of them existed before.
///
/// The rest of the file is the other half of the problem. This app is used
/// inside buildings, in basements and in villages with one bar of signal,
/// against a server that is often a machine in an office the phone cannot
/// reach. Signing out must not depend on that server being there, so these
/// tests take it away in each way it can go: silent, refusing, and — for the
/// keystore underneath — refusing to forget.

/// A server that accepted the connection and then never said anything.
///
/// The worst case for a client and a common one on a site: not a refusal,
/// which is instant, but a socket that stays open answering nothing until the
/// timeout runs out fifteen seconds later.
class _NeverAnswers implements HttpClientAdapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    return Completer<ResponseBody>().future;
  }

  @override
  void close({bool force = false}) {}
}

/// A server that answered, badly.
class _Refuses implements HttpClientAdapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    return ResponseBody.fromString('{"title":"Server error"}', 500);
  }

  @override
  void close({bool force = false}) {}
}

/// A server that works, and keeps the receipts.
///
/// Signing out is allowed to be quiet about failure, which makes it easy for
/// it to be quiet about never having happened. These tests read the requests.
class _Records implements HttpClientAdapter {
  final List<RequestOptions> requests = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);

    if (options.path.endsWith('/auth/refresh')) {
      return ResponseBody.fromString(
        jsonEncode({
          'accessToken': 'access-2',
          'accessTokenExpiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(minutes: 15))
              .toIso8601String(),
          'refreshToken': 'refresh-2',
          'refreshTokenExpiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(days: 7))
              .toIso8601String(),
          'user': {
            'id': '019fad65-d635-76f2-880f-d8d25aea67d0',
            'email': 'ivan@construction.local',
            'role': 'ProjectManager',
            'employeeId': '019fad73-e894-791b-a6c3-715bddf61164',
            'firstName': 'Ivan',
            'lastName': 'Horvat',
          },
        }),
        200,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType],
        },
      );
    }

    return ResponseBody.fromString('', 204);
  }

  @override
  void close({bool force = false}) {}

  RequestOptions? sentTo(String path) {
    for (final request in requests) {
      if (request.path.endsWith(path)) {
        return request;
      }
    }

    return null;
  }
}

/// Stands in for the platform keystore, which is unavailable in widget tests.
class _InMemorySessionStorage implements SecureSessionStorage {
  _InMemorySessionStorage([this._session]);

  AuthSession? _session;

  /// Android's EncryptedSharedPreferences does this when the key behind it has
  /// gone — after a reinstall, a restored backup, or a screen lock removed.
  bool refuseToForget = false;

  AuthSession? get stored => _session;

  @override
  Future<AuthSession?> read() async => _session;

  @override
  Future<void> write(AuthSession session) async => _session = session;

  @override
  Future<void> clear() async {
    if (refuseToForget) {
      throw StateError('keystore unavailable');
    }

    _session = null;
  }
}

AuthSession _storedSession({
  Duration accessTokenLife = const Duration(minutes: 15),
}) {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: 'access',
    accessTokenExpiresAt: now.add(accessTokenLife),
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

Dio _client(HttpClientAdapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'http://localhost:5000'));
  dio.httpClientAdapter = adapter;
  return dio;
}

Future<void> _pumpSignedIn(
  WidgetTester tester,
  SecureSessionStorage storage,
  HttpClientAdapter adapter,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        sessionStorageProvider.overrideWithValue(storage),
        offlineCacheProvider.overrideWithValue(
          Future<OfflineCache?>.value(null),
        ),
        apiClientProvider.overrideWithValue(_client(adapter)),
        plainClientProvider.overrideWithValue(_client(adapter)),
      ],
      child: const ConstructionApp(),
    ),
  );

  // Settled explicitly rather than with pumpAndSettle, which never returns
  // here: the splash screen animates forever, and so does the location card's
  // spinner once the home screen is up. The last two pumps are the route
  // transition — until it finishes the home screen is laid out off to one
  // side, present to every finder and reachable by no tap.
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 50));
  await tester.pump(const Duration(milliseconds: 50));
  await tester.pump(const Duration(seconds: 1));
  await tester.pump(const Duration(seconds: 1));

  expect(find.text('Ivan Horvat'), findsOneWidget);
}

/// Presses Sign out in the Account section and confirms.
Future<void> _signOut(WidgetTester tester) async {
  final tile = find.widgetWithText(ListTile, 'Sign out');

  // Dragged first to build it, then `ensureVisible`: a ListView builds a
  // little beyond the viewport, so a finder that matches is not yet a tile
  // anybody could press.
  await tester.dragUntilVisible(
    tile,
    find.byType(ListView).first,
    const Offset(0, -200),
  );
  await tester.ensureVisible(tile);
  await tester.pump(const Duration(milliseconds: 500));

  await tester.tap(tile);
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 300));

  await tester.tap(find.widgetWithText(FilledButton, 'Sign out'));

  // Only the dialog's dismissal and the route change. Deliberately far less
  // than a connect timeout: signing out waits for nothing on the network, and
  // a test that pumped for fifteen seconds would not notice if it did.
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
}

void main() {
  testWidgets('signs out without waiting for a server that never answers', (
    tester,
  ) async {
    final storage = _InMemorySessionStorage(_storedSession());

    await _pumpSignedIn(tester, storage, _NeverAnswers());
    await _signOut(tester);

    expect(find.text('Sign in'), findsOneWidget);
    expect(storage.stored, isNull, reason: 'the session must not survive');
  });

  testWidgets('signs out when the server refuses the call', (tester) async {
    final storage = _InMemorySessionStorage(_storedSession());

    await _pumpSignedIn(tester, storage, _Refuses());
    await _signOut(tester);

    expect(find.text('Sign in'), findsOneWidget);
    expect(storage.stored, isNull);
  });

  testWidgets('signs out when the keystore refuses to forget the session', (
    tester,
  ) async {
    final storage = _InMemorySessionStorage(_storedSession())
      ..refuseToForget = true;

    await _pumpSignedIn(tester, storage, _Refuses());
    await _signOut(tester);

    // The handset could not be made to forget, which is bad and outside the
    // app's control. Leaving the operator signed in as well would be worse.
    expect(find.text('Sign in'), findsOneWidget);
  });

  /// Not waiting for the server is not the same as not telling it.
  ///
  /// The refresh token stays usable for a week, so a sign-out that skipped the
  /// revoke would leave a live credential behind on a handset its owner
  /// believes they have signed out of — the phone that gets left in a site hut
  /// or handed to the next shift.
  testWidgets('revokes the session afterwards, with the captured token', (
    tester,
  ) async {
    final server = _Records();

    await _pumpSignedIn(
      tester,
      _InMemorySessionStorage(_storedSession()),
      server,
    );
    await _signOut(tester);

    final logout = server.sentTo('/api/v1/auth/logout');

    expect(logout, isNotNull, reason: 'the server was never told');
    expect(logout!.headers['Authorization'], 'Bearer access');
    expect((logout.data as Map)['refreshToken'], 'refresh');
    expect(server.sentTo('/auth/refresh'), isNull, reason: 'it was still good');
  });

  testWidgets('refreshes an access token that expired before revoking', (
    tester,
  ) async {
    final server = _Records();

    // What a phone unlocked after lunch is holding: the session is fine, the
    // access token on it died an hour ago.
    final stale = _storedSession(accessTokenLife: const Duration(hours: -1));

    await _pumpSignedIn(tester, _InMemorySessionStorage(stale), server);
    await _signOut(tester);

    expect(server.sentTo('/auth/refresh'), isNotNull);

    final logout = server.sentTo('/api/v1/auth/logout');

    expect(logout, isNotNull);
    expect(logout!.headers['Authorization'], 'Bearer access-2');
    expect((logout.data as Map)['refreshToken'], 'refresh-2');
  });
}
