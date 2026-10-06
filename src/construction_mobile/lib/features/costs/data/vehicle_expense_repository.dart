import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import '../../auth/presentation/auth_controller.dart';
import 'models/vehicle_expense.dart';

class VehicleExpenseRepository extends ApiRepository {
  /// [driverOnly] is for a Worker, who is served the driver endpoints instead of the cost module:
  /// their own fill-ups to read and fuel for the vehicle in their hands to record. Everyone above
  /// Worker uses the full cost endpoints.
  const VehicleExpenseRepository(super.dio, {this.driverOnly = false});

  final bool driverOnly;

  String get _path => driverOnly ? '/api/v1/vehicle-fuel' : '/api/v1/vehicle-expenses';

  Future<PagedList<VehicleExpense>> fetch({
    int pageNumber = 1,
    int pageSize = 20,
    String? vehicleId,
    String? kind,
  }) {
    return getPaged(
      _path,
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
    String? idempotencyKey,
  }) {
    return postJson(
      _path,
      VehicleExpense.fromJson,
      idempotencyKey: idempotencyKey,
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
  final role = ref.watch(currentUserProvider.select((user) => user?.role));

  return VehicleExpenseRepository(ref.watch(apiClientProvider), driverOnly: role == 'Worker');
});
