import 'package:dio/dio.dart';

import 'api_exception.dart';
import 'offline_cache.dart';

/// Told whenever the app answers a screen from the cache instead of the API.
///
/// A callback rather than a Riverpod reference so the interceptor can be
/// tested with nothing but a list.
typedef ServedFromCache = void Function(DateTime savedAt);

/// Told whenever a request reaches the server, so whatever was said about
/// stale data can be taken back.
typedef ServedLive = void Function();

/// Extra key set on a response the cache produced, carrying the moment it was
/// stored. Present only on cached responses, so `options.extra` is also the
/// answer to "was this live?".
const String fromCacheExtra = 'construction.fromCache';

/// Keeps the last good answer to every read, and serves it when the network
/// is gone.
///
/// It sits below the repositories rather than inside them, which is the whole
/// point: there are twelve repositories and one rule, and a rule about what a
/// screen may show when the phone has no signal should not be repeated twelve
/// times and forgotten on the thirteenth.
///
/// Only connectivity failures fall back. A 403 is not a coverage hole — it is
/// the server saying no, and answering it from a copy taken before the
/// permission was withdrawn would be a way of ignoring that. Same for a 404
/// after a deletion, and for a 500: the request arrived, and the server's
/// answer is the answer.
class OfflineCacheInterceptor extends Interceptor {
  OfflineCacheInterceptor({
    required this.cache,
    this.onServedFromCache,
    this.onServedLive,
    bool Function(RequestOptions options)? isCacheable,
  }) : _isCacheable = isCacheable ?? defaultIsCacheable;

  /// Opened once and awaited on each use. A device that cannot open its own
  /// support directory is a device the app still has to work on, so a failure
  /// here degrades to "no cache" rather than to a broken client.
  final Future<OfflineCache?> cache;

  final ServedFromCache? onServedFromCache;
  final ServedLive? onServedLive;
  final bool Function(RequestOptions options) _isCacheable;

  /// Reads that are worth keeping.
  ///
  /// GET only — a POST is an instruction, and replaying yesterday's answer to
  /// one would say something happened that did not.
  ///
  /// Not `/auth`: the token endpoints have nothing to serve offline, and
  /// `/auth/me` deciding who is signed in from a file is the kind of thing
  /// that turns a sign-out into a suggestion.
  ///
  /// Not attachment content: it is bytes, not JSON, and one site photograph
  /// would evict every list the app has.
  static bool defaultIsCacheable(RequestOptions options) {
    if (options.method.toUpperCase() != 'GET') {
      return false;
    }

    final path = options.path;

    if (path.contains('/auth/')) {
      return false;
    }

    if (path.contains('/attachments/') && path.endsWith('/content')) {
      return false;
    }

    return true;
  }

  @override
  Future<void> onResponse(
    Response<dynamic> response,
    ResponseInterceptorHandler handler,
  ) async {
    // Anything that came back over the wire means the phone is on the network,
    // whatever this particular request returned.
    onServedLive?.call();

    final options = response.requestOptions;
    final status = response.statusCode ?? 0;

    if (_isCacheable(options) && status >= 200 && status < 300) {
      final opened = await _openCache();

      await opened?.write(
        _keyFor(options),
        statusCode: status,
        body: response.data,
      );
    }

    handler.next(response);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final kind = ApiException.fromDioException(err).kind;
    final reachedTheServer =
        kind != ApiFailureKind.offline && kind != ApiFailureKind.timeout;

    if (reachedTheServer) {
      onServedLive?.call();
      handler.next(err);
      return;
    }

    if (!_isCacheable(err.requestOptions)) {
      handler.next(err);
      return;
    }

    final entry = await (await _openCache())?.read(_keyFor(err.requestOptions));

    if (entry == null) {
      handler.next(err);
      return;
    }

    onServedFromCache?.call(entry.savedAt);

    handler.resolve(
      Response<dynamic>(
        requestOptions: err.requestOptions,
        statusCode: entry.statusCode,
        data: entry.body,
        extra: <String, dynamic>{
          ...err.requestOptions.extra,
          fromCacheExtra: entry.savedAt,
        },
      ),
    );
  }

  static String _keyFor(RequestOptions options) {
    return OfflineCache.keyFor(
      options.method,
      options.path,
      options.queryParameters,
    );
  }

  Future<OfflineCache?> _openCache() async {
    try {
      return await cache;
    } catch (_) {
      // No support directory, no permission, no disk. The app carries on
      // without a cache; it did before this existed.
      return null;
    }
  }
}
