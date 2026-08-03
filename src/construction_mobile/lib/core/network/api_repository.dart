import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../models/paged_list.dart';
import 'api_exception.dart';

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
  @protected
  Future<void> postVoid(String path, {Object? data}) {
    return guard(() async {
      await dio.post<void>(path, data: data);
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
  @protected
  Future<T> postJson<T>(
    String path,
    T Function(Map<String, dynamic> json) fromJson, {
    Object? data,
  }) {
    return guard(() async {
      final response = await dio.post<Map<String, dynamic>>(path, data: data);

      return fromJson(response.data!);
    });
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
