import 'package:dio/dio.dart';

import '../../features/auth/data/models/auth_session.dart';
import 'auth_session_manager.dart';

/// Attaches the bearer token to outgoing requests and transparently refreshes
/// it: proactively when it has already expired, reactively when the server
/// answers 401. A refreshed request is replayed exactly once.
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required this._sessionManager,
    required this._retryClient,
  });

  static const _retriedFlag = 'auth_retried';

  /// Endpoints that must never carry (or wait for) a bearer token.
  static const _anonymousPaths = <String>[
    '/api/auth/login',
    '/api/auth/refresh',
    '/api/auth/forgot-password',
    '/api/auth/reset-password',
  ];

  final AuthSessionManager _sessionManager;

  /// Plain client used to replay a request after refreshing, so the replay
  /// cannot re-enter this interceptor.
  final Dio _retryClient;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (_isAnonymous(options.path)) {
      return handler.next(options);
    }

    final session = await _sessionManager.validSession();

    if (session != null) {
      options.headers['Authorization'] = 'Bearer ${session.accessToken}';
    }

    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final options = err.requestOptions;

    final shouldAttemptRefresh = err.response?.statusCode == 401 &&
        !_isAnonymous(options.path) &&
        options.extra[_retriedFlag] != true &&
        _sessionManager.isAuthenticated;

    if (!shouldAttemptRefresh) {
      return handler.next(err);
    }

    final refreshed = await _refreshQuietly();

    if (refreshed == null) {
      // Refresh failed; the manager has already cleared the session and the
      // app will route back to the sign-in screen.
      return handler.next(err);
    }

    try {
      final response = await _retryClient.fetch<dynamic>(
        options.copyWith(
          extra: {...options.extra, _retriedFlag: true},
          headers: {
            ...options.headers,
            'Authorization': 'Bearer ${refreshed.accessToken}',
          },
        ),
      );

      return handler.resolve(response);
    } on DioException catch (retryError) {
      return handler.next(retryError);
    }
  }

  Future<AuthSession?> _refreshQuietly() async {
    try {
      return await _sessionManager.refresh();
    } on DioException {
      return null;
    }
  }

  bool _isAnonymous(String path) =>
      _anonymousPaths.any((anonymous) => path.contains(anonymous));
}
