import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/app_notification.dart';

class NotificationRepository {
  NotificationRepository(this._dio);

  final Dio _dio;

  Future<PagedList<AppNotification>> fetchNotifications({
    int pageNumber = 1,
    int pageSize = 20,
    bool unreadOnly = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/notifications',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (unreadOnly) 'unreadOnly': true,
        },
      );

      return PagedList<AppNotification>.fromJson(
        response.data!,
        AppNotification.fromJson,
      );
    });
  }

  Future<int> fetchUnreadCount() async {
    return _guard(() async {
      final response =
          await _dio.get<int>('/api/notifications/unread-count');

      return response.data ?? 0;
    });
  }

  Future<void> markRead(String id) async {
    return _guard(() async {
      await _dio.post<void>('/api/notifications/$id/read');
    });
  }

  Future<int> markAllRead() async {
    return _guard(() async {
      final response = await _dio.post<int>('/api/notifications/read-all');
      return response.data ?? 0;
    });
  }

  Future<void> registerDeviceToken({
    required String token,
    required String platform,
  }) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/notifications/device-tokens',
        data: {'token': token, 'platform': platform},
      );
    });
  }

  Future<void> unregisterDeviceToken(String token) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/notifications/device-tokens/unregister',
        data: {'token': token},
      );
    });
  }

  Future<T> _guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }
}

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepository(ref.watch(apiClientProvider));
});
