import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/app_notification.dart';

class NotificationRepository extends ApiRepository {
  const NotificationRepository(super.dio);

  Future<PagedList<AppNotification>> fetchNotifications({
    int pageNumber = 1,
    int pageSize = 20,
    bool unreadOnly = false,
  }) {
    return getPaged(
      '/api/v1/notifications',
      AppNotification.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        filters: {'unreadOnly': unreadOnly ? true : null},
      ),
    );
  }

  Future<int> fetchUnreadCount() {
    return guard(() async {
      final response = await dio.get<int>('/api/v1/notifications/unread-count');

      return response.data ?? 0;
    });
  }

  Future<void> markRead(String id) {
    return postVoid('/api/v1/notifications/$id/read');
  }

  Future<int> markAllRead() {
    return guard(() async {
      final response = await dio.post<int>('/api/v1/notifications/read-all');

      return response.data ?? 0;
    });
  }

  Future<void> registerDeviceToken({
    required String token,
    required String platform,
  }) {
    return postVoid(
      '/api/v1/notifications/device-tokens',
      data: {'token': token, 'platform': platform},
    );
  }

  /// Removes this handset's push token.
  ///
  /// [accessToken] is passed explicitly by sign-out, which has already ended
  /// the local session by the time it gets here — see `AuthController.signOut`.
  Future<void> unregisterDeviceToken(String token, {String? accessToken}) {
    return postVoid(
      '/api/v1/notifications/device-tokens/unregister',
      data: {'token': token},
      accessToken: accessToken,
    );
  }
}

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepository(ref.watch(apiClientProvider));
});
