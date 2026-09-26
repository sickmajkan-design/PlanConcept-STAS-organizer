import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'article_order.dart';

class ArticleOrderRepository extends ApiRepository {
  const ArticleOrderRepository(super.dio);

  /// What the caller may see: the office sees every request, everyone else their own.
  Future<PagedList<ArticleOrder>> fetch({int pageNumber = 1, int pageSize = 100}) {
    return getPaged(
      '/api/v1/articleorders',
      ArticleOrder.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'createdAt',
        sortDescending: true,
      ),
    );
  }

  Future<ArticleOrder> create({
    required List<Map<String, dynamic>> items,
    required bool urgent,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/articleorders',
      ArticleOrder.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'urgent': urgent,
        'note': ?note,
        'items': items,
      },
    );
  }

  /// Moves a request one step on. Declining needs a [note].
  Future<ArticleOrder> setStatus(String id, String status, {String? note}) {
    return postJson(
      '/api/v1/articleorders/$id/status',
      ArticleOrder.fromJson,
      data: <String, dynamic>{'status': status, 'note': ?note},
    );
  }
}

final articleOrderRepositoryProvider = Provider<ArticleOrderRepository>((ref) {
  return ArticleOrderRepository(ref.watch(apiClientProvider));
});
