import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/tool_expense.dart';

class ToolExpenseRepository extends ApiRepository {
  const ToolExpenseRepository(super.dio);

  Future<PagedList<ToolExpense>> fetch({
    int pageNumber = 1,
    int pageSize = 20,
    String? toolId,
    String? kind,
  }) {
    return getPaged(
      '/api/v1/tool-expenses',
      ToolExpense.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        filters: {'toolId': toolId, 'kind': kind},
      ),
    );
  }

  /// Records a repair, a service, or any other cost against a tool.
  Future<ToolExpense> record({
    required String toolId,
    required String kind,
    required double amount,
    String? supplier,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/tool-expenses',
      ToolExpense.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'toolId': toolId,
        'kind': kind,
        'amount': amount,
        'supplier': ?supplier,
        'note': ?note,
      },
    );
  }
}

final toolExpenseRepositoryProvider = Provider<ToolExpenseRepository>((ref) {
  return ToolExpenseRepository(ref.watch(apiClientProvider));
});
