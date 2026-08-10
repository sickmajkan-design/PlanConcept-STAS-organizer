import 'dart:convert';
import 'dart:typed_data';

import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:construction_mobile/core/network/offline_cache_interceptor.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// An adapter with a switch on it: answers like a server, or fails the way a
/// phone in a lift shaft fails.
class _Adapter implements HttpClientAdapter {
  _Adapter(this.body);

  Object? body;
  int statusCode = 200;

  /// When true, nothing leaves the phone.
  bool offline = false;

  int calls = 0;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    calls++;

    if (offline) {
      throw DioException(
        requestOptions: options,
        type: DioExceptionType.connectionError,
        error: 'Network is unreachable',
      );
    }

    return ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
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
  late _MemoryStore store;
  late Dio dio;
  late List<DateTime> servedFromCache;
  late int servedLive;

  setUp(() {
    adapter = _Adapter({'items': <dynamic>[], 'totalCount': 0});
    store = _MemoryStore();
    servedFromCache = <DateTime>[];
    servedLive = 0;

    dio = Dio(BaseOptions(
      baseUrl: 'http://api.test',
      validateStatus: (status) => status != null && status < 400,
    ))
      ..httpClientAdapter = adapter
      ..interceptors.add(
        OfflineCacheInterceptor(
          cache: Future<OfflineCache?>.value(OfflineCache(store)),
          onServedFromCache: servedFromCache.add,
          onServedLive: () => servedLive++,
        ),
      );
  });

  group('with a signal', () {
    test('the answer is the server\'s, and it is kept', () async {
      final response = await dio.get<Map<String, dynamic>>('/api/v1/employees');

      expect(response.data, {'items': <dynamic>[], 'totalCount': 0});
      expect(store.entries, hasLength(1));
      expect(servedLive, 1);
      expect(response.extra[fromCacheExtra], isNull);
    });

    test('a fresh answer replaces the kept one', () async {
      await dio.get<Map<String, dynamic>>('/api/v1/employees');

      adapter.body = {'items': <dynamic>[], 'totalCount': 7};
      await dio.get<Map<String, dynamic>>('/api/v1/employees');

      adapter.offline = true;
      final response = await dio.get<Map<String, dynamic>>('/api/v1/employees');

      expect(response.data!['totalCount'], 7);
    });
  });

  group('without one', () {
    test('the last good answer is served, and said to be old', () async {
      await dio.get<Map<String, dynamic>>('/api/v1/employees');
      adapter.offline = true;

      final response = await dio.get<Map<String, dynamic>>('/api/v1/employees');

      expect(response.data, {'items': <dynamic>[], 'totalCount': 0});
      expect(response.extra[fromCacheExtra], isA<DateTime>());

      // The screen has to be able to say *when*, not just that it is offline.
      expect(servedFromCache, hasLength(1));
      expect(
        servedFromCache.single,
        equals(response.extra[fromCacheExtra] as DateTime),
      );
    });

    test('a request never made before still fails', () async {
      adapter.offline = true;

      await expectLater(
        dio.get<Map<String, dynamic>>('/api/v1/projects'),
        throwsA(
          isA<DioException>().having(
            (exception) => ApiException.fromDioException(exception).kind,
            'kind',
            ApiFailureKind.offline,
          ),
        ),
      );

      expect(servedFromCache, isEmpty);
    });

    test('the query decides which answer comes back', () async {
      adapter.body = {'items': <dynamic>[], 'totalCount': 1};
      await dio.get<Map<String, dynamic>>(
        '/api/v1/employees',
        queryParameters: {'pageNumber': 1},
      );

      adapter.body = {'items': <dynamic>[], 'totalCount': 2};
      await dio.get<Map<String, dynamic>>(
        '/api/v1/employees',
        queryParameters: {'pageNumber': 2},
      );

      adapter.offline = true;

      final first = await dio.get<Map<String, dynamic>>(
        '/api/v1/employees',
        queryParameters: {'pageNumber': 1},
      );

      expect(first.data!['totalCount'], 1, reason: 'page 2 was served for page 1');
    });
  });

  group('what the cache stays out of', () {
    test('a refusal, which is the server speaking', () async {
      await dio.get<Map<String, dynamic>>('/api/v1/employees');

      adapter.statusCode = 403;
      adapter.body = {'title': 'Forbidden'};

      // A permission that has been taken away must not be worked around by
      // showing a copy taken while it still applied.
      await expectLater(
        dio.get<Map<String, dynamic>>('/api/v1/employees'),
        throwsA(isA<DioException>()),
      );

      expect(servedFromCache, isEmpty);
    });

    test('a 500, which also reached the server', () async {
      await dio.get<Map<String, dynamic>>('/api/v1/employees');

      adapter.statusCode = 500;
      adapter.body = {'title': 'Server error'};

      await expectLater(
        dio.get<Map<String, dynamic>>('/api/v1/employees'),
        throwsA(isA<DioException>()),
      );

      expect(servedFromCache, isEmpty);

      // And it still counts as being on the network, so the stale-data notice
      // comes down.
      expect(servedLive, 2);
    });

    test('a POST, in both directions', () async {
      adapter.body = {'id': 'created'};
      await dio.post<Map<String, dynamic>>('/api/v1/timeentries/clock-in');

      expect(store.entries, isEmpty, reason: 'an instruction is not an answer');

      adapter.offline = true;

      await expectLater(
        dio.post<Map<String, dynamic>>('/api/v1/timeentries/clock-in'),
        throwsA(isA<DioException>()),
      );
    });

    test('the session endpoints', () async {
      adapter.body = {'id': 'u1', 'email': 'foreman@example.test'};
      await dio.get<Map<String, dynamic>>('/api/v1/auth/me');

      // Who is signed in is not a question a file on disk gets to answer.
      expect(store.entries, isEmpty);
    });

    test('attachment content, which is bytes and would evict everything',
        () async {
      adapter.body = {'ignored': true};
      await dio.get<Map<String, dynamic>>('/api/v1/attachments/abc/content');

      expect(store.entries, isEmpty);
    });
  });

  test('the notice comes down as soon as anything gets through', () async {
    await dio.get<Map<String, dynamic>>('/api/v1/employees');
    adapter.offline = true;
    await dio.get<Map<String, dynamic>>('/api/v1/employees');

    expect(servedFromCache, hasLength(1));

    adapter.offline = false;
    await dio.get<Map<String, dynamic>>('/api/v1/employees');

    expect(servedLive, 2);
  });

  test('a device with no cache at all still works', () async {
    final bare = Dio(BaseOptions(baseUrl: 'http://api.test'))
      ..httpClientAdapter = adapter
      ..interceptors.add(
        OfflineCacheInterceptor(cache: Future<OfflineCache?>.value(null)),
      );

    final response = await bare.get<Map<String, dynamic>>('/api/v1/employees');

    expect(response.data, isNotNull);

    adapter.offline = true;

    await expectLater(
      bare.get<Map<String, dynamic>>('/api/v1/employees'),
      throwsA(isA<DioException>()),
    );
  });
}
