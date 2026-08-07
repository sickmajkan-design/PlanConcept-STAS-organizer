import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle_expense.dart';

class VehicleExpenseRepository extends ApiRepository {
  const VehicleExpenseRepository(super.dio);

  Future<PagedList<VehicleExpense>> fetch({
    int pageNumber = 1,
    int pageSize = 20,
    String? vehicleId,
    String? kind,
  }) {
    return getPaged(
      '/api/v1/vehicle-expenses',
      VehicleExpense.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        filters: {'vehicleId': vehicleId, 'kind': kind},
      ),
    );
  }

  /// Records a cost against a vehicle, from wherever it was incurred.
  ///
  /// [litres] belongs to a fill-up and nothing else — the API and the database
  /// both refuse it on any other kind, so the caller must not send it.
  Future<VehicleExpense> record({
    required String vehicleId,
    required String kind,
    required double amount,
    double? litres,
    int? odometerKm,
    String? supplier,
    String? note,
  }) {
    return postJson(
      '/api/v1/vehicle-expenses',
      VehicleExpense.fromJson,
      data: <String, dynamic>{
        'vehicleId': vehicleId,
        'kind': kind,
        'amount': amount,
        'litres': ?litres,
        'odometerKm': ?odometerKm,
        'supplier': ?supplier,
        'note': ?note,
      },
    );
  }
}

final vehicleExpenseRepositoryProvider = Provider<VehicleExpenseRepository>((ref) {
  return VehicleExpenseRepository(ref.watch(apiClientProvider));
});
