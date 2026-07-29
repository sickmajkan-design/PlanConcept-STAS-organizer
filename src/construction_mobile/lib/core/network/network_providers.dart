import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../config/app_config.dart';
import '../storage/secure_session_storage.dart';
import 'auth_interceptor.dart';
import 'auth_session_manager.dart';

final secureStorageProvider = Provider<FlutterSecureStorage>((ref) {
  return const FlutterSecureStorage();
});

final sessionStorageProvider = Provider<SecureSessionStorage>((ref) {
  return SecureSessionStorage(ref.watch(secureStorageProvider));
});

BaseOptions _baseOptions() => BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: AppConfig.connectTimeout,
      receiveTimeout: AppConfig.receiveTimeout,
      contentType: Headers.jsonContentType,
      responseType: ResponseType.json,
      // Let the interceptor and repositories decide what an error is instead
      // of Dio throwing before we can read the problem-details body.
      validateStatus: (status) => status != null && status < 400,
    );

/// Client without the auth interceptor. Used for token refresh and for
/// replaying a request after refresh, so neither can recurse.
final plainClientProvider = Provider<Dio>((ref) {
  final dio = Dio(_baseOptions());
  ref.onDispose(dio.close);
  return dio;
});

final authSessionManagerProvider = Provider<AuthSessionManager>((ref) {
  final manager = AuthSessionManager(
    storage: ref.watch(sessionStorageProvider),
    refreshClient: ref.watch(plainClientProvider),
  );

  ref.onDispose(manager.dispose);
  return manager;
});

/// The authenticated client every feature repository uses.
final apiClientProvider = Provider<Dio>((ref) {
  final dio = Dio(_baseOptions());

  dio.interceptors.add(
    AuthInterceptor(
      sessionManager: ref.watch(authSessionManagerProvider),
      retryClient: ref.watch(plainClientProvider),
    ),
  );

  ref.onDispose(dio.close);
  return dio;
});
