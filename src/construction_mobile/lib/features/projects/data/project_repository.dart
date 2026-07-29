import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/project.dart';

class ProjectRepository {
  ProjectRepository(this._dio);

  final Dio _dio;

  Future<PagedList<Project>> fetchProjects({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? employeeId,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/projects',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          'status': ?status,
          'employeeId': ?employeeId,
          'sortBy': ?sortBy,
          if (sortDescending) 'sortDescending': true,
        },
      );

      return PagedList<Project>.fromJson(response.data!, Project.fromJson);
    });
  }

  Future<ProjectDetail> fetchProject(String id) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>('/api/projects/$id');

      return ProjectDetail.fromJson(response.data!);
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

final projectRepositoryProvider = Provider<ProjectRepository>((ref) {
  return ProjectRepository(ref.watch(apiClientProvider));
});
