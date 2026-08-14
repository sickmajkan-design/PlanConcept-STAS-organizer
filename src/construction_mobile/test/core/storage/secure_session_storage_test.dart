import 'package:construction_mobile/core/storage/secure_session_storage.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

/// What happens when the keystore under the session storage misbehaves.
///
/// This is not hypothetical on Android. `EncryptedSharedPreferences` is backed
/// by a key in the platform keystore, and that key can go: reinstalling the
/// app, restoring the handset from a backup, or removing the screen lock all
/// invalidate it. What comes back then is not null — it is a `PlatformException`
/// out of the platform channel, from a call that had no reason to fail.
///
/// Both places it can happen are on a path where an exception is expensive.
/// Reading happens during startup, and the router waits on the splash screen
/// until the restore answers — an exception is not an answer, so the app never
/// leaves the splash screen. Deleting happens during sign-out, where a failure
/// would mean the session stayed on the device.
class _Keystore implements FlutterSecureStorage {
  _Keystore({this.value, this.failOn = const <Symbol>{}});

  String? value;

  /// Which of `#read`, `#write` and `#delete` throw the way a keystore whose
  /// key has been invalidated throws.
  final Set<Symbol> failOn;

  final List<Symbol> calls = <Symbol>[];

  @override
  dynamic noSuchMethod(Invocation invocation) {
    final member = invocation.memberName;
    calls.add(member);

    if (failOn.contains(member)) {
      throw PlatformException(
        code: 'BAD_DECRYPT',
        message: 'Could not decrypt value; the key is no longer available',
      );
    }

    switch (member) {
      case #read:
        return Future<String?>.value(value);
      case #write:
        value = invocation.namedArguments[#value] as String?;
        return Future<void>.value();
      case #delete:
        value = null;
        return Future<void>.value();
      default:
        return super.noSuchMethod(invocation);
    }
  }
}

AuthSession _session() {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: 'access',
    accessTokenExpiresAt: now.add(const Duration(minutes: 15)),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: now.add(const Duration(days: 7)),
    user: const User(
      id: '019fad65-d635-76f2-880f-d8d25aea67d0',
      email: 'ivan@construction.local',
      role: 'Foreman',
    ),
  );
}

void main() {
  test('a session survives being written and read back', () async {
    final keystore = _Keystore();
    final storage = SecureSessionStorage(keystore);

    await storage.write(_session());
    final restored = await storage.read();

    expect(restored, isNotNull);
    expect(restored!.refreshToken, 'refresh');
    expect(restored.user.email, 'ivan@construction.local');
  });

  test(
    'a keystore that cannot be read is a handset with nobody signed in',
    () async {
      final storage = SecureSessionStorage(_Keystore(failOn: {#read}));

      // Null, not an exception. The alternative is an app that never gets past
      // its splash screen and cannot be recovered from except by reinstalling.
      await expectLater(storage.read(), completion(isNull));
    },
  );

  test('a keystore that refuses the delete is overwritten instead', () async {
    final keystore = _Keystore(value: '{"anything":true}', failOn: {#delete});
    final storage = SecureSessionStorage(keystore);

    await storage.clear();

    expect(keystore.calls, contains(#write));
    expect(keystore.value, isEmpty);

    // And the overwrite is not merely tidy — an empty value has to read back
    // as nobody being signed in, or the session would still be there.
    await expectLater(storage.read(), completion(isNull));
  });

  test('a payload from an incompatible build is dropped, not thrown', () async {
    final keystore = _Keystore(value: 'not json at all');
    final storage = SecureSessionStorage(keystore);

    await expectLater(storage.read(), completion(isNull));
    expect(
      keystore.value,
      isNull,
      reason: 'and it does not stay to fail again',
    );
  });
}
