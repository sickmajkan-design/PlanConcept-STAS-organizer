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

  /// Notifications the signed-in user must confirm before doing anything else.
  Future<List<AppNotification>> fetchPendingAcknowledgments() {
    return guard(() async {
      final response = await dio.get<List<dynamic>>(
        '/api/v1/notifications/pending-acknowledgments',
      );

      return (response.data ?? [])
          .map((json) => AppNotification.fromJson(json as Map<String, dynamic>))
          .toList();
    });
  }

  Future<void> acknowledge(String id) {
    return postVoid('/api/v1/notifications/$id/acknowledge');
  }

  Future<int> markAllRead() {
    return guard(() async {
      final response = await dio.post<int>('/api/v1/notifications/read-all');

      return response.data ?? 0;
    });
  }

  /// Sends a free-typed message straight to one employee. The API refuses a
  /// Foreman's attempt to reach someone off their own site with a 403; the
  /// caller shows that through the same `ApiException`-in-a-snackbar pattern
  /// every other write in this app already uses.
  Future<void> sendDirect({
    required String employeeId,
    required String title,
    required String body,
    bool requiresAcknowledgment = false,
  }) {
    return postVoid(
      '/api/v1/notifications/notify-employee',
      data: {
        'employeeId': employeeId,
        'title': title,
        'body': body,
        'requiresAcknowledgment': requiresAcknowledgment,
      },
    );
  }

  /// Sends one message to everyone, or narrowed to a role, one project's
  /// crew, and/or one named group — any combination narrows together.
  /// Returns how many accounts it actually reached.
  Future<int> sendAnnouncement({
    required String title,
    required String body,
    String? role,
    String? projectId,
    String? groupId,
    bool requiresAcknowledgment = false,
  }) {
    return guard(() async {
      final response = await dio.post<int>(
        '/api/v1/notifications/announce',
        data: {
          'title': title,
          'body': body,
          'role': ?role,
          'projectId': ?projectId,
          'groupId': ?groupId,
          'requiresAcknowledgment': requiresAcknowledgment,
        },
      );

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
