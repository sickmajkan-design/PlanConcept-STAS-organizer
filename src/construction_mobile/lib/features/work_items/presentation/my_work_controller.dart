import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/work_item.dart';
import '../data/work_item_repository.dart';

/// Chip offered above the list: everything, including what is already done.
const workIncludeFinishedFilter = 'Include finished';

/// The signed-in employee's own tasks and defects.
class MyWorkController extends FilteredPagedListNotifier<WorkItem> {
  @override
  Future<PagedList<WorkItem>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(workItemRepositoryProvider).fetchMine(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          // The default view is what is still to do; the chip widens it.
          openOnly: filter != workIncludeFinishedFilter,
        );
  }

  /// Moves an item on and reloads, so the list and the row cannot disagree.
  Future<void> changeStatus(WorkItem item, String status) async {
    await ref.read(workItemRepositoryProvider).changeStatus(item.id, status);
    await refresh();
  }
}

final myWorkControllerProvider =
    AsyncNotifierProvider<MyWorkController, PagedState<WorkItem>>(
  MyWorkController.new,
);
