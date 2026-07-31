import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/material.dart';

class MaterialRepository {
  MaterialRepository(this._dio);

  final Dio _dio;

  Future<PagedList<MaterialItem>> fetchMaterials({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool? unassignedOnly,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/materials',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          if (unassignedOnly == true) 'unassignedOnly': true,
          'sortBy': ?sortBy,
          if (sortDescending) 'sortDescending': true,
        },
      );

      return PagedList<MaterialItem>.fromJson(
          response.data!, MaterialItem.fromJson);
    });
  }

  Future<MaterialItem> fetchMaterial(String id) async {
    return _guard(() async {
      final response =
          await _dio.get<Map<String, dynamic>>('/api/materials/$id');

      return MaterialItem.fromJson(response.data!);
    });
  }

  Future<T> _guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }
}

final materialRepositoryProvider = Provider<MaterialRepository>((ref) {
  return MaterialRepository(ref.watch(apiClientProvider));
});
