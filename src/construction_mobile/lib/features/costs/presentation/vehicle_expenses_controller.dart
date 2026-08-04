import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/vehicle_expense.dart';
import '../data/vehicle_expense_repository.dart';

/// Chip offered above the list: fill-ups only.
const vehicleExpenseFuelFilter = 'Fuel';

/// What the fleet has cost lately, newest first.
class VehicleExpensesController extends FilteredPagedListNotifier<VehicleExpense> {
  @override
  Future<PagedList<VehicleExpense>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    // The endpoint has no text search; the base class supplies one anyway and
    // sending it would filter on a parameter the API ignores.
    return ref.read(vehicleExpenseRepositoryProvider).fetch(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          kind: filter == vehicleExpenseFuelFilter ? 'Fuel' : null,
        );
  }

  /// Records a cost and reloads, so the new row is on screen straight away.
  Future<void> record({
    required String vehicleId,
    required String kind,
    required double amount,
    double? litres,
    int? odometerKm,
    String? supplier,
    String? note,
  }) async {
    await ref.read(vehicleExpenseRepositoryProvider).record(
          vehicleId: vehicleId,
          kind: kind,
          amount: amount,
          litres: litres,
          odometerKm: odometerKm,
          supplier: supplier,
          note: note,
        );

    await refresh();
  }
}

final vehicleExpensesControllerProvider =
    AsyncNotifierProvider<VehicleExpensesController, PagedState<VehicleExpense>>(
  VehicleExpensesController.new,
);
