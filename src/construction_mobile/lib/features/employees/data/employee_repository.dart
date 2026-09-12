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

  Future<EmployeeDetail> create({
    required String employeeNumber,
    required String firstName,
    required String lastName,
    String? phone,
    String? email,
    String? address,
    DateTime? dateOfBirth,
    required DateTime employmentDate,
    required String position,
    String status = 'Active',
    String type = 'Employee',
  }) {
    return postJson(
      '/api/v1/employees',
      EmployeeDetail.fromJson,
      data: {
        'employeeNumber': employeeNumber,
        'firstName': firstName,
        'lastName': lastName,
        'phone': ?phone,
        'email': ?email,
        'address': ?address,
        'dateOfBirth': ?dateOfBirth?.toIso8601String(),
        'employmentDate': employmentDate.toIso8601String(),
        'position': position,
        'status': status,
        'type': type,
      },
    );
  }

  Future<EmployeeDetail> update(
    String id, {
    required String employeeNumber,
    required String firstName,
    required String lastName,
    String? phone,
    String? email,
    String? address,
    DateTime? dateOfBirth,
    required DateTime employmentDate,
    required String position,
    required String status,
    required String type,
  }) {
    return putJson(
      '/api/v1/employees/$id',
      EmployeeDetail.fromJson,
      data: {
        'employeeNumber': employeeNumber,
        'firstName': firstName,
        'lastName': lastName,
        'phone': ?phone,
        'email': ?email,
        'address': ?address,
        'dateOfBirth': ?dateOfBirth?.toIso8601String(),
        'employmentDate': employmentDate.toIso8601String(),
        'position': position,
        'status': status,
        'type': type,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/employees/$id');
  }
}

final employeeRepositoryProvider = Provider<EmployeeRepository>((ref) {
  return EmployeeRepository(ref.watch(apiClientProvider));
});
