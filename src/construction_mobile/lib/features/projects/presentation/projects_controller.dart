import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/project.dart';
import '../data/project_repository.dart';

/// Project statuses offered as filter chips, mirroring the API's enum.
const projectStatusFilters = <String>[
  'Planned',
  'Active',
  'OnHold',
  'Completed',
  'Cancelled',
];

class ProjectsController extends FilteredPagedListNotifier<Project> {
  @override
  Future<PagedList<Project>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(projectRepositoryProvider).fetchProjects(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          status: filter,
        );
  }
}

final projectsControllerProvider =
    AsyncNotifierProvider<ProjectsController, PagedState<Project>>(
  ProjectsController.new,
);

final projectDetailProvider =
    FutureProvider.autoDispose.family<ProjectDetail, String>((ref, id) {
  return ref.watch(projectRepositoryProvider).fetchProject(id);
});
