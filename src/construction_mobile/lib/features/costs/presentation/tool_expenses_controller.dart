import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/tool_expense.dart';
import '../data/tool_expense_repository.dart';

/// What the tool fleet has cost lately, newest first.
class ToolExpensesController extends PagedListNotifier<ToolExpense> {
  @override
  Future<PagedList<ToolExpense>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    // The endpoint has no text search; the base class supplies one anyway and
    // sending it would filter on a parameter the API ignores.
    return ref.read(toolExpenseRepositoryProvider).fetch(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
        );
  }

  /// Records a cost and reloads, so the new row is on screen straight away.
  Future<void> record({
    required String toolId,
    required String kind,
    required double amount,
    String? supplier,
    String? note,
    required String idempotencyKey,
  }) async {
    await ref.read(toolExpenseRepositoryProvider).record(
          toolId: toolId,
          kind: kind,
          amount: amount,
          supplier: supplier,
          note: note,
          idempotencyKey: idempotencyKey,
        );

    await refresh();
  }
}

final toolExpensesControllerProvider =
    AsyncNotifierProvider<ToolExpensesController, PagedState<ToolExpense>>(
  ToolExpensesController.new,
);
