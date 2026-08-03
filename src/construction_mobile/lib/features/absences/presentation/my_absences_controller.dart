import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/absence_repository.dart';
import '../data/models/absence.dart';

/// Chip offered above the list: only what nobody has answered yet.
const absencePendingFilter = 'Pending';

/// The signed-in employee's own time off.
class MyAbsencesController extends FilteredPagedListNotifier<Absence> {
  @override
  Future<PagedList<Absence>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    // The endpoint has no text search; the base class still supplies one and
    // sending it would filter on a parameter the API ignores.
    return ref.read(absenceRepositoryProvider).fetchMine(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          status: filter == absencePendingFilter ? 'Requested' : null,
        );
  }

  /// Asks for time off and reloads, so the new request appears in the list.
  Future<void> request({
    required String type,
    required DateTime startDate,
    required DateTime endDate,
    String? reason,
  }) async {
    await ref.read(absenceRepositoryProvider).request(
          type: type,
          startDate: startDate,
          endDate: endDate,
          reason: reason,
        );

    await refresh();
  }

  /// Takes back an unanswered request.
  Future<void> withdraw(Absence absence) async {
    await ref.read(absenceRepositoryProvider).withdraw(absence.id);
    await refresh();
  }
}

final myAbsencesControllerProvider =
    AsyncNotifierProvider<MyAbsencesController, PagedState<Absence>>(
  MyAbsencesController.new,
);
