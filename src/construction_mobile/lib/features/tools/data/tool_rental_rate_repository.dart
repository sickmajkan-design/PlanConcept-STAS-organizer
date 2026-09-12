import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/tool_rental_rate.dart';

/// The rent/lease rate history for a tool — see
/// `VehicleRentalRateRepository` for the pattern this mirrors.
class ToolRentalRateRepository extends ApiRepository {
  const ToolRentalRateRepository(super.dio);

  Future<PagedList<ToolRentalRate>> fetch({
    required String toolId,
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    return getPaged(
      '/api/v1/tool-rental-rates',
      ToolRentalRate.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'startDate',
        sortDescending: true,
        filters: {'toolId': toolId},
      ),
    );
  }

  Future<ToolRentalRate> set({
    required String toolId,
    required double monthlyAmount,
    String? provider,
    DateTime? startDate,
    DateTime? endDate,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/tool-rental-rates',
      ToolRentalRate.fromJson,
      idempotencyKey: idempotencyKey,
      data: {
        'toolId': toolId,
        'monthlyAmount': monthlyAmount,
        'provider': ?provider,
        'startDate': ?startDate?.toIso8601String(),
        'endDate': ?endDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<ToolRentalRate> update(
    String id, {
    required double monthlyAmount,
    String? provider,
    required DateTime startDate,
    DateTime? endDate,
    String? note,
  }) {
    return putJson(
      '/api/v1/tool-rental-rates/$id',
      ToolRentalRate.fromJson,
      data: {
        'monthlyAmount': monthlyAmount,
        'provider': ?provider,
        'startDate': startDate.toIso8601String(),
        'endDate': ?endDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/tool-rental-rates/$id');
  }
}

final toolRentalRateRepositoryProvider = Provider<ToolRentalRateRepository>((ref) {
  return ToolRentalRateRepository(ref.watch(apiClientProvider));
});
