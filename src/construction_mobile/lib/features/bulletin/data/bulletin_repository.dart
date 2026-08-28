import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/bulletin_post.dart';

class BulletinRepository extends ApiRepository {
  const BulletinRepository(super.dio);

  /// Every notice currently posted, newest first. Not paged — same reasoning
  /// as the admin panel: a board is a handful of live notices, not a report.
  Future<List<BulletinPost>> fetchAll() {
    return guard(() async {
      final response =
          await dio.get<List<dynamic>>('/api/v1/bulletin');

      return response.data!
          .cast<Map<String, dynamic>>()
          .map(BulletinPost.fromJson)
          .toList();
    });
  }

  /// Records that the caller has seen one post. Idempotent — safe to call
  /// again for a post already marked viewed.
  Future<void> markViewed(String id) {
    return postVoid('/api/v1/bulletin/$id/view');
  }
}

final bulletinRepositoryProvider = Provider<BulletinRepository>((ref) {
  return BulletinRepository(ref.watch(apiClientProvider));
});
