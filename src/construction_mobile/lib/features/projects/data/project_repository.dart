import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/project.dart';

class ProjectRepository extends ApiRepository {
  const ProjectRepository(super.dio);

  Future<PagedList<Project>> fetchProjects({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? employeeId,
    String? sortBy,
    bool sortDescending = false,
  }) {
    return getPaged(
      '/api/v1/projects',
      Project.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        sortBy: sortBy,
        sortDescending: sortDescending,
        filters: {'status': status, 'employeeId': employeeId},
      ),
    );
  }

  Future<ProjectDetail> fetchProject(String id) {
    return getJson('/api/v1/projects/$id', ProjectDetail.fromJson);
  }

  /// Every project, for a picker — Material's own project field, and
  /// Project's own Parent-Project field (client-filtered to `kind == 'Main'`
  /// by the caller, since a sub-project cannot itself be a parent).
  Future<List<Project>> fetchAll() async {
    final page = await getPaged(
      '/api/v1/projects',
      Project.fromJson,
      query: pagedQuery(pageNumber: 1, pageSize: 200),
    );

    return page.items;
  }

  Future<ProjectDetail> create({
    required String name,
    String? description,
    String? customerId,
    String? parentProjectId,
    String? address,
    double? latitude,
    double? longitude,
    String? countryCode,
    String? shiftStartTime,
    String? startDate,
    String? endDate,
    String status = 'Planned',
    double? contractValue,
  }) {
    return postJson(
      '/api/v1/projects',
      ProjectDetail.fromJson,
      data: {
        'name': name,
        'description': ?description,
        'customerId': ?customerId,
        'parentProjectId': ?parentProjectId,
        'address': ?address,
        if (latitude != null && longitude != null) ...{
          'latitude': latitude,
          'longitude': longitude,
        },
        'countryCode': ?countryCode,
        'shiftStartTime': ?shiftStartTime,
        'startDate': ?startDate,
        'endDate': ?endDate,
        'status': status,
        'contractValue': ?contractValue,
      },
    );
  }

  Future<ProjectDetail> update(
    String id, {
    required String name,
    String? description,
    String? customerId,
    String? parentProjectId,
    String? address,
    double? latitude,
    double? longitude,
    String? countryCode,
    String? shiftStartTime,
    String? startDate,
    String? endDate,
    required String status,
    double? contractValue,
  }) {
    return putJson(
      '/api/v1/projects/$id',
      ProjectDetail.fromJson,
      data: {
        'name': name,
        'description': ?description,
        'customerId': ?customerId,
        'parentProjectId': ?parentProjectId,
        'address': ?address,
        if (latitude != null && longitude != null) ...{
          'latitude': latitude,
          'longitude': longitude,
        },
        'countryCode': ?countryCode,
        'shiftStartTime': ?shiftStartTime,
        'startDate': ?startDate,
        'endDate': ?endDate,
        'status': status,
        'contractValue': ?contractValue,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/projects/$id');
  }
}

final projectRepositoryProvider = Provider<ProjectRepository>((ref) {
  return ProjectRepository(ref.watch(apiClientProvider));
});
