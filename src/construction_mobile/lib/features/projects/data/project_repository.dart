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
}

final projectRepositoryProvider = Provider<ProjectRepository>((ref) {
  return ProjectRepository(ref.watch(apiClientProvider));
});
