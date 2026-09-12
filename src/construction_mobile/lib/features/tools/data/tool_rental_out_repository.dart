import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/tool_rental_out.dart';

/// Loans of a tool out to another company or person — see
/// `VehicleRentalOutRepository` for the pattern this mirrors.
class ToolRentalOutRepository extends ApiRepository {
  const ToolRentalOutRepository(super.dio);

  Future<PagedList<ToolRentalOut>> fetch({
    required String toolId,
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    return getPaged(
      '/api/v1/tool-rentals-out',
      ToolRentalOut.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'startDate',
        sortDescending: true,
        filters: {'toolId': toolId},
      ),
    );
  }

  Future<ToolRentalOut> record({
    required String toolId,
    String? customerId,
    required String renterName,
    required double dailyRate,
    DateTime? startDate,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/tool-rentals-out',
      ToolRentalOut.fromJson,
      idempotencyKey: idempotencyKey,
      data: {
        'toolId': toolId,
        'customerId': ?customerId,
        'renterName': renterName,
        'dailyRate': dailyRate,
        'startDate': ?startDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<ToolRentalOut> returnRental(
    String id, {
    DateTime? endDate,
    String? idempotencyKey,
  }) {
    return putJson(
      '/api/v1/tool-rentals-out/$id/return',
      ToolRentalOut.fromJson,
      data: {'endDate': ?endDate?.toIso8601String()},
    );
  }

  Future<ToolRentalOut> update(
    String id, {
    String? customerId,
    required String renterName,
    required double dailyRate,
    required DateTime startDate,
    String? note,
  }) {
    return putJson(
      '/api/v1/tool-rentals-out/$id',
      ToolRentalOut.fromJson,
      data: {
        'customerId': ?customerId,
        'renterName': renterName,
        'dailyRate': dailyRate,
        'startDate': startDate.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/tool-rentals-out/$id');
  }
}

final toolRentalOutRepositoryProvider = Provider<ToolRentalOutRepository>((ref) {
  return ToolRentalOutRepository(ref.watch(apiClientProvider));
});
