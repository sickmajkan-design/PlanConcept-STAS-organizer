import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/app_notification.dart';
import '../data/notification_repository.dart';

class NotificationsController extends PagedListNotifier<AppNotification> {
  bool _unreadOnly = false;

  bool get unreadOnly => _unreadOnly;

  @override
  Future<PagedList<AppNotification>> loadPage({
    required int pageNumber,
    // The notifications endpoint has no free-text search.
    required String search,
  }) {
    return ref.read(notificationRepositoryProvider).fetchNotifications(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          unreadOnly: _unreadOnly,
        );
  }

  void showUnreadOnly(bool value) {
    if (_unreadOnly == value) {
      return;
    }

    _unreadOnly = value;
    ref.invalidateSelf();
  }

  Future<void> markRead(AppNotification notification) async {
    if (notification.isRead) {
      return;
    }

    await ref.read(notificationRepositoryProvider).markRead(notification.id);

    _applyRead({notification.id});
    ref.invalidate(unreadNotificationCountProvider);
  }

  Future<void> markAllRead() async {
    await ref.read(notificationRepositoryProvider).markAllRead();

    final current = state.value;

    if (current != null) {
      _applyRead(current.items.map((item) => item.id).toSet());
    }

    ref.invalidate(unreadNotificationCountProvider);
  }

  /// Updates the rows in place so the list does not jump while the user is
  /// reading it.
  void _applyRead(Set<String> ids) {
    final current = state.value;

    if (current == null) {
      return;
    }

    final now = DateTime.now().toUtc();

    final updated = current.items
        .map(
          (item) => ids.contains(item.id) && !item.isRead
              ? item.copyWith(isRead: true, readAt: now)
              : item,
        )
        .where((item) => !_unreadOnly || !item.isRead)
        .toList();

    state = AsyncData(current.copyWith(items: updated));
  }
}

final notificationsControllerProvider =
    AsyncNotifierProvider<NotificationsController, PagedState<AppNotification>>(
  NotificationsController.new,
);

/// Unread badge count. Returns 0 while signed out so the badge disappears
/// immediately on sign-out instead of erroring.
final unreadNotificationCountProvider = FutureProvider<int>((ref) async {
  final user = ref.watch(currentUserProvider);

  if (user == null) {
    return 0;
  }

  return ref.read(notificationRepositoryProvider).fetchUnreadCount();
});
