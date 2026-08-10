import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../config/app_config.dart';
import '../storage/secure_session_storage.dart';
import 'auth_interceptor.dart';
import 'auth_session_manager.dart';
import 'offline_cache.dart';
import 'offline_cache_interceptor.dart';
import 'offline_data_status.dart';

final secureStorageProvider = Provider<FlutterSecureStorage>((ref) {
  return const FlutterSecureStorage();
});

/// The offline cache, opened once for the life of the app.
///
/// A `Future` held in a plain `Provider` rather than a `FutureProvider`: every
/// consumer is an interceptor that already awaits things, and none of them
/// should make a request wait on a widget rebuild.
///
/// Null means the device would not give us a directory to write in. That is
/// rare and not fatal — without a cache the app behaves exactly as it did
/// before there was one.
final offlineCacheProvider = Provider<Future<OfflineCache?>>((ref) {
  return openOfflineCache();
});

/// How long the app waits for the platform to hand over a directory.
///
/// There is a deadline because everything downstream waits on this: a
/// request that wants the cache, and — more to the point — signing out, which
/// empties it. A platform call that never answers would leave a worker
/// looking at a splash screen with no way forward, to protect a cache the app
/// is perfectly able to run without.
const Duration offlineCacheOpenTimeout = Duration(seconds: 5);

/// Opens the cache, or gives up and returns null.
///
/// Null is a supported state, not an error path to be fixed later: no
/// directory, no permission, no disk, or a platform that simply does not
/// answer. Every caller treats it as "there is no cache", which is how the
/// app behaved before there was one.
@visibleForTesting
Future<OfflineCache?> openOfflineCache({
  Future<BlobStore> Function() open = FileBlobStore.open,
  Duration timeout = offlineCacheOpenTimeout,
}) async {
  try {
    return OfflineCache(await open().timeout(timeout));
  } catch (_) {
    return null;
  }
}

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
    // Site phones get handed around. Everything the cache holds belongs to
    // whoever was signed in when it was written, so it goes when they do.
    onIdentityChanged: () async {
      final cache = await ref.read(offlineCacheProvider);
      await cache?.clear();
    },
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

  // After the auth interceptor, deliberately. A 401 is that one's business: it
  // refreshes and replays, and the cache must not answer a request that is
  // about to succeed. What reaches here is what auth could not rescue, and a
  // request that never left the phone is the case worth rescuing.
  dio.interceptors.add(
    OfflineCacheInterceptor(
      cache: ref.watch(offlineCacheProvider),
      onServedFromCache: (savedAt) =>
          ref.read(offlineDataProvider.notifier).servedFromCache(savedAt),
      onServedLive: () => ref.read(offlineDataProvider.notifier).servedLive(),
    ),
  );

  ref.onDispose(dio.close);
  return dio;
});
