import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'refund.dart';

class RefundRepository extends ApiRepository {
  const RefundRepository(super.dio);

  /// What the caller may see: the office sees every request, everyone else their own.
  Future<PagedList<Refund>> fetch({int pageNumber = 1, int pageSize = 100}) {
    return getPaged(
      '/api/v1/refunds',
      Refund.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'createdAt',
        sortDescending: true,
      ),
    );
  }

  Future<Refund> create({
    required double amount,
    required String currency,
    required DateTime expenseDate,
    required String description,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/refunds',
      Refund.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'amount': amount,
        'currency': currency,
        'expenseDate': _asDate(expenseDate),
        'description': description,
      },
    );
  }

  /// Approves, declines (with a [note]) or withdraws a request.
  Future<Refund> review(String id, String status, {String? note}) {
    return postJson(
      '/api/v1/refunds/$id/review',
      Refund.fromJson,
      data: <String, dynamic>{'status': status, 'note': ?note},
    );
  }

  static String _asDate(DateTime value) {
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '${value.year}-$month-$day';
  }
}

final refundRepositoryProvider = Provider<RefundRepository>((ref) {
  return RefundRepository(ref.watch(apiClientProvider));
});
