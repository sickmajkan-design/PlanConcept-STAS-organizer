import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/time_entry.dart';
import '../data/time_entry_repository.dart';

/// The signed-in employee's own entries, newest first.
///
/// Plain [PagedListNotifier] rather than the filtered variant: the API has no
/// text search on this collection and there is nothing on the screen to filter
/// by — a worker's own timesheet is short enough to scroll.
class MyTimeEntriesController extends PagedListNotifier<TimeEntry> {
  @override
  Future<PagedList<TimeEntry>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(timeEntryRepositoryProvider).fetchMine(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
        );
  }
}

final myTimeEntriesControllerProvider =
    AsyncNotifierProvider<MyTimeEntriesController, PagedState<TimeEntry>>(
  MyTimeEntriesController.new,
);
