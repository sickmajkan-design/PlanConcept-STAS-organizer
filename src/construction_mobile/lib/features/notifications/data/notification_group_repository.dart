import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/notification_group.dart';

class NotificationGroupRepository extends ApiRepository {
  const NotificationGroupRepository(super.dio);

  /// Every group, for the announcement form's audience picker — realistically
  /// small enough company-wide that one page beats search here too.
  Future<List<NotificationGroup>> fetchAll() async {
    final page = await getPaged(
      '/api/v1/notificationgroups',
      NotificationGroup.fromJson,
      query: pagedQuery(pageNumber: 1, pageSize: 200),
    );

    return page.items;
  }
}

final notificationGroupRepositoryProvider =
    Provider<NotificationGroupRepository>((ref) {
  return NotificationGroupRepository(ref.watch(apiClientProvider));
});
