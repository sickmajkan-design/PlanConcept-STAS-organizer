import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../../../core/outbox/outbox_queue.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../outbox/outbox_controller.dart';
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
          employeeId: ref.read(currentUserProvider)?.employeeId,
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          status: filter == absencePendingFilter ? 'Requested' : null,
        );
  }

  /// Asks for time off and reloads, so the new request appears in the list.
  ///
  /// With no signal the request is kept on the phone and sent later, and the
  /// result says so. A refusal is thrown as before.
  Future<SubmitResult> request({
    required String type,
    required DateTime startDate,
    required DateTime endDate,
    String? reason,
  }) async {
    final result = await ref.read(outboxControllerProvider.notifier).submit(
      OutboxKind.absenceRequest,
      <String, dynamic>{
        'type': type,
        'startDate': startDate.toIso8601String(),
        'endDate': endDate.toIso8601String(),
        'reason': reason,
      },
    );

    if (!result.queued) {
      await refresh();
    }

    return result;
  }

  /// Takes back an unanswered request.
  Future<void> withdraw(Absence absence) async {
    await ref.read(absenceRepositoryProvider).withdraw(absence.id);
    await refresh();
  }

  /// Proposes new dates for one of the employee's own already-approved
  /// absences. Waits on management to confirm — see [Absence.hasPendingEdit].
  Future<void> proposeEdit({
    required Absence absence,
    required DateTime startDate,
    required DateTime endDate,
    String? reason,
  }) async {
    await ref.read(absenceRepositoryProvider).proposeEdit(
          id: absence.id,
          startDate: startDate,
          endDate: endDate,
          reason: reason,
        );

    await refresh();
  }

  /// Confirms or declines a change management proposed on one of the
  /// employee's own absences.
  Future<void> confirmEdit({required Absence absence, required bool approve}) async {
    await ref
        .read(absenceRepositoryProvider)
        .confirmEdit(id: absence.id, approve: approve);

    await refresh();
  }
}

final myAbsencesControllerProvider =
    AsyncNotifierProvider<MyAbsencesController, PagedState<Absence>>(
  MyAbsencesController.new,
);
