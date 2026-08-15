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
    this.onIdentityChanged,
  });

  /// Called when the person signed in stops being the person signed in —
  /// signing out, being signed out by a rejected refresh, or a different
  /// account signing in on the same device.
  ///
  /// Not called when a token is refreshed: that is the same person, and
  /// treating it as a change would throw away the offline cache several times
  /// a shift, which on a site is exactly when it is needed.
  final Future<void> Function()? onIdentityChanged;

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
      // Through `clear` rather than straight to storage: a session that
      // expired while the app was closed still ends a session, and anything
      // hanging off that session — the offline cache above all — has to go
      // with it. Nothing is listening to `changes` this early, so the emitted
      // null costs nothing.
      await clear();
      return null;
    }

    _session = stored;
    _changes.add(stored);
    return stored;
  }

  Future<void> start(AuthSession session) async {
    // A refresh calls this too, with the same person's new tokens. Only a
    // different person is a change.
    if (_session?.user.id != session.user.id) {
      try {
        await onIdentityChanged?.call();
      } catch (_) {
        // Guarded for the same reason as in `clear`, and with the same
        // reasoning about what is actually being risked: the wipe itself
        // swallows filesystem failures, so the only way to arrive here is a
        // cache that could not be opened at all — which is a session with
        // nothing cached to leak into it. Refusing the sign-in over it would
        // strand somebody on the sign-in screen to protect an empty directory.
      }
    }

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

  /// Ends the session on this device.
  ///
  /// The one method here that is not allowed to fail. Everything it touches is
  /// somebody else's — a platform keystore, a directory on disk — and any of
  /// it can refuse on a given handset. None of that is a reason to leave the
  /// app signed in: a sign-out that threw halfway used to clear the session
  /// from memory and then stop before telling anybody, which left the operator
  /// looking at a home screen belonging to a session that no longer existed,
  /// every request on it answering 401.
  Future<void> clear() async {
    _session = null;

    try {
      await _storage.clear();
    } catch (_) {
      // The keystore would not forget. [SecureSessionStorage.clear] has
      // already tried its fallback, and there is nothing further to attempt
      // from here — but the app still signs out.
    }

    try {
      await onIdentityChanged?.call();
    } catch (_) {
      // Emptying the offline cache is a courtesy to whoever picks the phone up
      // next, not a precondition for signing the current user out.
    }

    if (!_changes.isClosed) {
      _changes.add(null);
    }
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
      final refreshed = await _exchange(current.refreshToken);

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

  /// Trades a captured refresh token for a usable pair, without adopting the
  /// result as this device's session.
  ///
  /// For sign-out, which ends the session locally before telling the API — so
  /// by the time it does, there is nothing here left to authenticate with, and
  /// the access token it captured on the way out may already have expired. A
  /// worker who opens the app after lunch and signs out immediately is holding
  /// one that died an hour ago; without this the revoke would be refused and
  /// the refresh token would stay live for a week on a handset whose owner
  /// believes they have signed out of it.
  ///
  /// Returns null when there is nothing left to revoke with, which is not an
  /// error: the session had already ended by itself.
  Future<AuthSession?> refreshDetached(AuthSession captured) async {
    if (captured.isRefreshTokenExpired) {
      return null;
    }

    try {
      return await _exchange(captured.refreshToken);
    } on DioException {
      return null;
    }
  }

  /// The refresh call itself, in one place: both the session's own refresh and
  /// [refreshDetached] go through here.
  Future<AuthSession> _exchange(String refreshToken) async {
    final response = await _refreshClient.post<Map<String, dynamic>>(
      '/api/v1/auth/refresh',
      data: {'refreshToken': refreshToken},
    );

    return AuthSession.fromResponse(AuthResponse.fromJson(response.data!));
  }

  Future<void> dispose() async {
    await _changes.close();
  }
}
