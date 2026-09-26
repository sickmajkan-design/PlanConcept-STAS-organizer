import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/idempotency.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/article_order.dart';
import '../data/article_order_repository.dart';

/// Requests for articles the caller may see: their own, and for the office everyone's.
///
/// Watches the signed-in user, so the next person to sign in on the phone never sees the
/// previous one's requests.
class ArticleOrdersController extends AsyncNotifier<List<ArticleOrder>> {
  @override
  Future<List<ArticleOrder>> build() async {
    ref.watch(currentUserProvider);

    final page = await ref.read(articleOrderRepositoryProvider).fetch();

    return page.items;
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(() async {
      final page = await ref.read(articleOrderRepositoryProvider).fetch();

      return page.items;
    });
  }

  Future<void> create({
    required List<Map<String, dynamic>> items,
    required bool urgent,
    String? note,
  }) async {
    await ref.read(articleOrderRepositoryProvider).create(
          items: items,
          urgent: urgent,
          note: note,
          idempotencyKey: newIdempotencyKey(),
        );
    await refresh();
  }

  Future<void> setStatus(ArticleOrder order, String status, {String? note}) async {
    await ref.read(articleOrderRepositoryProvider).setStatus(order.id, status, note: note);
    await refresh();
  }
}

final articleOrdersControllerProvider =
    AsyncNotifierProvider<ArticleOrdersController, List<ArticleOrder>>(
  ArticleOrdersController.new,
);
