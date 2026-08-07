import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/employee.dart';

class EmployeeRepository extends ApiRepository {
  const EmployeeRepository(super.dio);

  Future<PagedList<Employee>> fetchEmployees({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? projectId,
    String? sortBy,
    bool sortDescending = false,
  }) {
    return getPaged(
      '/api/v1/employees',
      Employee.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        sortBy: sortBy,
        sortDescending: sortDescending,
        filters: {'status': status, 'projectId': projectId},
      ),
    );
  }

  Future<EmployeeDetail> fetchEmployee(String id) {
    return getJson('/api/v1/employees/$id', EmployeeDetail.fromJson);
  }
}

final employeeRepositoryProvider = Provider<EmployeeRepository>((ref) {
  return EmployeeRepository(ref.watch(apiClientProvider));
});
