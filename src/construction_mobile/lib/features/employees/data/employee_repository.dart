import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/employee.dart';

class EmployeeRepository {
  EmployeeRepository(this._dio);

  final Dio _dio;

  Future<PagedList<Employee>> fetchEmployees({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? projectId,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/employees',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          'status': ?status,
          'projectId': ?projectId,
          'sortBy': ?sortBy,
          if (sortDescending) 'sortDescending': true,
        },
      );

      return PagedList<Employee>.fromJson(response.data!, Employee.fromJson);
    });
  }

  Future<EmployeeDetail> fetchEmployee(String id) async {
    return _guard(() async {
      final response =
          await _dio.get<Map<String, dynamic>>('/api/employees/$id');

      return EmployeeDetail.fromJson(response.data!);
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

final employeeRepositoryProvider = Provider<EmployeeRepository>((ref) {
  return EmployeeRepository(ref.watch(apiClientProvider));
});
