import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/tool.dart';

class ToolRepository {
  ToolRepository(this._dio);

  final Dio _dio;

  Future<PagedList<Tool>> fetchTools({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/tools',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          'status': ?status,
          'sortBy': ?sortBy,
          if (sortDescending) 'sortDescending': true,
        },
      );

      return PagedList<Tool>.fromJson(response.data!, Tool.fromJson);
    });
  }

  Future<Tool> fetchTool(String id) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>('/api/tools/$id');

      return Tool.fromJson(response.data!);
    });
  }

  /// Looks a tool up by its QR label. Open to every authenticated employee,
  /// including roles without directory access.
  Future<Tool> fetchToolByQrCode(String qrCode) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/tools/by-qr/${Uri.encodeComponent(qrCode)}',
      );

      return Tool.fromJson(response.data!);
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

final toolRepositoryProvider = Provider<ToolRepository>((ref) {
  return ToolRepository(ref.watch(apiClientProvider));
});
