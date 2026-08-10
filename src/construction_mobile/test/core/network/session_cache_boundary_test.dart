import 'package:construction_mobile/core/network/auth_session_manager.dart';
import 'package:construction_mobile/core/storage/secure_session_storage.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// The cache belongs to whoever is signed in.
///
/// Site phones are shared — one handset in the site hut, three foremen through
/// the week. Everything the offline cache holds was fetched with somebody's
/// token, and if it outlives their session the next person can read it by
/// turning on aeroplane mode and opening the app. So the rule is that a change
/// of person empties it, and the tricky half is the other direction: a token
/// refresh happens several times a shift and is *not* a change of person, so
/// treating it as one would throw the cache away exactly when a phone in poor
/// coverage needs it.
class _FakeStorage implements SecureSessionStorage {
  AuthSession? stored;

  @override
  Future<AuthSession?> read() async => stored;

  @override
  Future<void> write(AuthSession session) async => stored = session;

  @override
  Future<void> clear() async => stored = null;
}

User _user(String id) => User(
      id: id,
      email: '$id@construction.local',
      role: 'Foreman',
    );

AuthSession _session(
  String userId, {
  String accessToken = 'access',
  Duration refreshTokenLifetime = const Duration(days: 7),
}) {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: accessToken,
    accessTokenExpiresAt: now.add(const Duration(minutes: 15)),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: now.add(refreshTokenLifetime),
    user: _user(userId),
  );
}

void main() {
  late _FakeStorage storage;
  late int wipes;
  late AuthSessionManager manager;

  setUp(() {
    storage = _FakeStorage();
    wipes = 0;

    manager = AuthSessionManager(
      storage: storage,
      refreshClient: Dio(),
      onIdentityChanged: () async => wipes++,
    );
  });

  test('signing out empties it', () async {
    await manager.start(_session('ivan'));
    expect(wipes, 1, reason: 'signing in on a device is itself a change');

    await manager.clear();

    expect(wipes, 2);
  });

  test('a token refresh does not', () async {
    await manager.start(_session('ivan'));
    final before = wipes;

    // What `_performRefresh` does: same person, new tokens.
    await manager.start(_session('ivan', accessToken: 'access-2'));

    expect(wipes, before);
    expect(manager.session!.accessToken, 'access-2');
  });

  test('a different person does', () async {
    await manager.start(_session('ivan'));
    await manager.clear();
    final before = wipes;

    await manager.start(_session('marko'));

    expect(wipes, before + 1);
  });

  test('a session that expired while the app was closed does', () async {
    // The app is opened on Monday with Friday's session still on disk. Nobody
    // signs out, nobody signs in — the session simply ends, and what it left
    // in the cache has to end with it.
    storage.stored = _session(
      'ivan',
      refreshTokenLifetime: const Duration(days: -1),
    );

    final restored = await manager.restore();

    expect(restored, isNull);
    expect(storage.stored, isNull);
    expect(wipes, 1);
  });

  test('a session still good is restored without emptying anything', () async {
    storage.stored = _session('ivan');

    final restored = await manager.restore();

    expect(restored, isNotNull);
    expect(wipes, 0, reason: 'the cache is this user\'s and they are back');
  });
}
