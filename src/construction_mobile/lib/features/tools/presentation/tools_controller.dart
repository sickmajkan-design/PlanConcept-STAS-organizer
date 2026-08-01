import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/tool.dart';
import '../data/tool_repository.dart';

/// Tool statuses offered as filter chips, mirroring the API's enum.
const toolStatusFilters = <String>[
  'Available',
  'Assigned',
  'UnderRepair',
  'Lost',
  'Retired',
];

class ToolsController extends FilteredPagedListNotifier<Tool> {
  @override
  Future<PagedList<Tool>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(toolRepositoryProvider).fetchTools(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          status: filter,
        );
  }
}

final toolsControllerProvider =
    AsyncNotifierProvider<ToolsController, PagedState<Tool>>(
  ToolsController.new,
);

final toolDetailProvider =
    FutureProvider.autoDispose.family<Tool, String>((ref, id) {
  return ref.watch(toolRepositoryProvider).fetchTool(id);
});
