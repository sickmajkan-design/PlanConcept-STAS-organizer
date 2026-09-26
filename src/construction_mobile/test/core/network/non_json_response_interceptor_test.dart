import 'dart:typed_data';

import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/network/non_json_response_interceptor.dart';
import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:construction_mobile/core/network/offline_cache_interceptor.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// The network of a site wireless that has not been signed into: it is
/// "connected", and every request is answered with its login page.
class _Adapter implements HttpClientAdapter {
  String body = '{"ok":true}';
  String contentType = Headers.jsonContentType;
  int statusCode = 200;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    return ResponseBody.fromString(
      body,
      statusCode,
      headers: {
        Headers.contentTypeHeader: [contentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

class _MemoryStore implements BlobStore {
  final Map<String, String> entries = <String, String>{};

  @override
  Future<String?> read(String key) async => entries[key];

  @override
  Future<void> write(String key, String value) async => entries[key] = value;

  @override
  Future<void> delete(String key) async => entries.remove(key);

  @override
  Future<List<String>> keys() async => entries.keys.toList();

  @override
  Future<void> clear() async => entries.clear();
}

void main() {
  late _Adapter adapter;
  late Dio dio;

  setUp(() {
    adapter = _Adapter();

    dio = Dio(BaseOptions(
      baseUrl: 'http://api.test',
      responseType: ResponseType.json,
      validateStatus: (status) => status != null && status < 400,
    ))
      ..httpClientAdapter = adapter
      ..interceptors.add(const NonJsonResponseInterceptor());
  });

  Future<ApiException> failureOf(Future<Object?> request) async {
    try {
      await request;
    } on DioException catch (exception) {
      return ApiException.fromDioException(exception);
    }

    throw StateError('the request was expected to fail');
  }

  test('a real answer passes through untouched', () async {
    final response = await dio.get<Map<String, dynamic>>('/api/v1/x');

    expect(response.data, {'ok': true});
  });

  test('a login page with a cheerful 200 is a lost connection', () async {
    adapter
      ..body = '<html><body>Sign in to the guest network</body></html>'
      ..contentType = 'text/html; charset=utf-8';

    final failure = await failureOf(dio.get<Map<String, dynamic>>('/api/v1/x'));

    // Offline, not "something went wrong": that is the kind the clock queue,
    // the outbox and the cached screens all act on.
    expect(failure.kind, ApiFailureKind.offline);
  });

  test('so is one that forgot to say what it is', () async {
    adapter
      ..body = 'Please accept the terms'
      ..contentType = 'text/plain';

    final failure = await failureOf(dio.get<Map<String, dynamic>>('/api/v1/x'));

    expect(failure.kind, ApiFailureKind.offline);
  });

  test('an empty body is not a web page', () async {
    // 204 and friends: nothing to parse is what the API says for "done".
    adapter
      ..body = ''
      ..contentType = 'text/plain';

    final response = await dio.post<void>('/api/v1/x');

    expect(response.statusCode, 200);
  });

  test('a JSON answer with an unusual content type is still JSON', () async {
    adapter.contentType = 'application/problem+json';

    final response = await dio.get<Map<String, dynamic>>('/api/v1/x');

    expect(response.data, {'ok': true});
  });

  test('a download is left alone: bytes are not a web page', () async {
    adapter
      ..body = 'not json at all'
      ..contentType = 'image/jpeg';

    final response = await dio.get<List<int>>(
      '/api/v1/attachments/1/content',
      options: Options(responseType: ResponseType.bytes),
    );

    expect(response.data, isNotEmpty);
  });

  test('the cache answers for a captive portal as it does for no signal',
      () async {
    final store = _MemoryStore();

    dio.interceptors.add(
      OfflineCacheInterceptor(cache: Future<OfflineCache?>.value(OfflineCache(store))),
    );

    // Signal, once: the roster is kept.
    await dio.get<Map<String, dynamic>>('/api/v1/employees');

    // Then the phone joins the site wifi and gets its login page.
    adapter
      ..body = '<html>login</html>'
      ..contentType = 'text/html';

    final response = await dio.get<Map<String, dynamic>>('/api/v1/employees');

    expect(response.data, {'ok': true});
    expect(response.extra[fromCacheExtra], isA<DateTime>());
  });
}
