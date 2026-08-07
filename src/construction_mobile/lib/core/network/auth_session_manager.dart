import 'dart:async';

import 'package:dio/dio.dart';

import '../../features/auth/data/models/auth_response.dart';
import '../../features/auth/data/models/auth_session.dart';
import '../../features/auth/data/models/user.dart';
import '../storage/secure_session_storage.dart';

/// Single source of truth for the signed-in session.
///
/// Holds the tokens in memory, mirrors them into secure storage, and owns
/// token refresh. Refresh is single-flight: however many requests hit a 401
/// at once, only one refresh call is sent and all of them await its result.
class AuthSessionManager {
  AuthSessionManager({
    required this._storage,
    required this._refreshClient,
  });

  final SecureSessionStorage _storage;

  /// Plain client without the auth interceptor — refreshing must never
  /// recurse back into the interceptor that triggered it.
  final Dio _refreshClient;

  final StreamController<AuthSession?> _changes =
      StreamController<AuthSession?>.broadcast();

  AuthSession? _session;
  Future<AuthSession?>? _refreshInFlight;

  /// Emits on every session change; `null` means the user is signed out.
  Stream<AuthSession?> get changes => _changes.stream;

  AuthSession? get session => _session;

  bool get isAuthenticated => _session != null;

  /// Loads a persisted session at app start. A session whose refresh token
  /// has already expired is discarded — it can no longer be revived.
  Future<AuthSession?> restore() async {
    final stored = await _storage.read();

    if (stored == null) {
      return null;
    }

    if (stored.isRefreshTokenExpired) {
      await _storage.clear();
      return null;
    }

    _session = stored;
    _changes.add(stored);
    return stored;
  }

  Future<void> start(AuthSession session) async {
    _session = session;
    await _storage.write(session);
    _changes.add(session);
  }

  Future<void> updateUser(User user) async {
    final current = _session;

    if (current == null) {
      return;
    }

    final updated = current.copyWith(user: user);
    _session = updated;
    await _storage.write(updated);
    _changes.add(updated);
  }

  Future<void> clear() async {
    _session = null;
    await _storage.clear();
    _changes.add(null);
  }

  /// Returns a session with a usable access token, refreshing it first when
  /// the current one has expired. Returns `null` when the user must sign in
  /// again.
  Future<AuthSession?> validSession() async {
    final current = _session;

    if (current == null) {
      return null;
    }

    if (!current.isAccessTokenExpired) {
      return current;
    }

    return refresh();
  }

  /// Exchanges the refresh token for a new token pair. Concurrent callers
  /// share one in-flight request.
  Future<AuthSession?> refresh() {
    return _refreshInFlight ??= _performRefresh().whenComplete(() {
      _refreshInFlight = null;
    });
  }

  Future<AuthSession?> _performRefresh() async {
    final current = _session;

    if (current == null || current.isRefreshTokenExpired) {
      await clear();
      return null;
    }

    try {
      final response = await _refreshClient.post<Map<String, dynamic>>(
        '/api/v1/auth/refresh',
        data: {'refreshToken': current.refreshToken},
      );

      final refreshed = AuthSession.fromResponse(
        AuthResponse.fromJson(response.data!),
      );

      await start(refreshed);
      return refreshed;
    } on DioException catch (exception) {
      final status = exception.response?.statusCode;

      // The refresh token was rejected (expired, revoked, or replayed after
      // rotation) — the session is unrecoverable.
      if (status == 401 || status == 400) {
        await clear();
        return null;
      }

      // Transport problem: keep the session so the user can retry once the
      // network recovers.
      rethrow;
    }
  }

  Future<void> dispose() async {
    await _changes.close();
  }
}
