import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../features/auth/data/models/auth_session.dart';

/// Persists the signed-in session in the platform keystore
/// (Android EncryptedSharedPreferences / iOS Keychain).
class SecureSessionStorage {
  SecureSessionStorage(this._storage);

  static const _sessionKey = 'auth_session';

  final FlutterSecureStorage _storage;

  Future<AuthSession?> read() async {
    final String? raw;

    try {
      raw = await _storage.read(key: _sessionKey);
    } catch (_) {
      // Android's EncryptedSharedPreferences throws rather than returning null
      // when the key behind it has gone — after an app reinstall, a restored
      // device backup, or a screen lock being removed. All of those mean the
      // stored session cannot be read, which is the same thing as not having
      // one. Letting it escape instead would leave the app on the splash
      // screen for good, because the router waits there until the restore
      // answers, and an exception is not an answer.
      await clear();
      return null;
    }

    if (raw == null || raw.isEmpty) {
      return null;
    }

    try {
      return AuthSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } on FormatException {
      // Stored payload from an incompatible build — drop it and sign in again.
      await clear();
      return null;
    }
  }

  Future<void> write(AuthSession session) =>
      _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));

  /// Removes the stored session, and tries twice.
  ///
  /// A keystore that refuses the delete must not be left holding a usable
  /// session, so the value is overwritten with an empty one — which [read]
  /// already treats as nobody being signed in.
  Future<void> clear() async {
    try {
      await _storage.delete(key: _sessionKey);
    } catch (_) {
      await _storage.write(key: _sessionKey, value: '');
    }
  }
}
