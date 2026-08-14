import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../models/paged_list.dart';
import 'api_exception.dart';
import 'idempotency.dart';

/// Base for every repository that talks to the API.
///
/// Three things used to be copied into each repository: the try/catch that
/// turns a [DioException] into an [ApiException], the unwrapping of a JSON
/// response, and the query map for a paged list endpoint. They live here now,
/// which keeps the guarantee callers rely on — a repository only ever throws
/// an [ApiException], never a transport type — in one place instead of eight.
abstract class ApiRepository {
  const ApiRepository(this.dio);

  @protected
  final Dio dio;

  /// Runs [request], converting any transport failure into an [ApiException].
  /// Every call in a repository goes through here.
  @protected
  Future<T> guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }

  /// GETs a JSON object and maps it with [fromJson].
  @protected
  Future<T> getJson<T>(
    String path,
    T Function(Map<String, dynamic> json) fromJson, {
    Map<String, dynamic>? query,
  }) {
    return guard(() async {
      final response =
          await dio.get<Map<String, dynamic>>(path, queryParameters: query);

      return fromJson(response.data!);
    });
  }

  /// GETs one page of a list endpoint.
  @protected
  Future<PagedList<T>> getPaged<T>(
    String path,
    T Function(Map<String, dynamic> json) fromJson, {
    required Map<String, dynamic> query,
  }) {
    return guard(() async {
      final response =
          await dio.get<Map<String, dynamic>>(path, queryParameters: query);

      return PagedList<T>.fromJson(response.data!, fromJson);
    });
  }

  /// POSTs and discards the response body.
  ///
  /// [accessToken] authorises the call with a token the caller is holding
  /// rather than the one the session manager would attach. Sign-out is the
  /// only caller: it ends the session first, so that by the time it tells the
  /// API to revoke anything there is no session left to authenticate with.
  @protected
  Future<void> postVoid(
    String path, {
    Object? data,
    String? idempotencyKey,
    String? accessToken,
  }) {
    return guard(() async {
      await dio.post<void>(
        path,
        data: data,
        options:
            _options(idempotencyKey: idempotencyKey, accessToken: accessToken),
      );
    });
  }

  /// DELETEs and discards the response body.
  @protected
  Future<void> deleteVoid(String path) {
    return guard(() async {
      await dio.delete<void>(path);
    });
  }

  /// POSTs and maps the JSON body of the response.
  ///
  /// [idempotencyKey] names the attempt. Pass one wherever running the request
  /// twice would mean two of something — a cost row, a stock movement — and
  /// keep the same value across retries of that attempt; see
  /// `newIdempotencyKey`.
  @protected
  Future<T> postJson<T>(
    String path,
    T Function(Map<String, dynamic> json) fromJson, {
    Object? data,
    String? idempotencyKey,
  }) {
    return guard(() async {
      final response = await dio.post<Map<String, dynamic>>(
        path,
        data: data,
        options: _options(idempotencyKey: idempotencyKey),
      );

      return fromJson(response.data!);
    });
  }

  /// Request options carrying whichever headers were asked for, or null when
  /// none were — Dio treats a null `options` as "use the defaults", so this
  /// stays out of the way of every call that needs neither.
  static Options? _options({String? idempotencyKey, String? accessToken}) {
    if (idempotencyKey == null && accessToken == null) {
      return null;
    }

    return Options(
      headers: <String, String>{
        idempotencyHeader: ?idempotencyKey,
        if (accessToken != null) 'Authorization': 'Bearer $accessToken',
      },
    );
  }

  /// Builds the query map the paged list endpoints accept. [filters] carries
  /// the parameters specific to one endpoint; an entry with a null value is
  /// dropped so the API applies its own default rather than seeing an empty
  /// filter.
  @protected
  Map<String, dynamic> pagedQuery({
    required int pageNumber,
    required int pageSize,
    String? search,
    String? sortBy,
    bool sortDescending = false,
    Map<String, dynamic> filters = const <String, dynamic>{},
  }) {
    return <String, dynamic>{
      'pageNumber': pageNumber,
      'pageSize': pageSize,
      if (search != null && search.isNotEmpty) 'search': search,
      'sortBy': ?sortBy,
      if (sortDescending) 'sortDescending': true,
      for (final entry in filters.entries)
        if (entry.value != null) entry.key: entry.value,
    };
  }
}
