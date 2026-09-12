import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/app_notification.dart';
import 'notification_detail_screen.dart';
import 'notification_text.dart';
import 'notification_type_icon.dart';
import 'notifications_controller.dart';
import 'send_announcement_sheet.dart';

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(notificationsControllerProvider.notifier);
    final state = ref.watch(notificationsControllerProvider);
    final unread = ref.watch(unreadNotificationCountProvider).value ?? 0;
    final l10n = context.l10n;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.navNotifications),
        actions: [
          if (ref.watch(currentUserProvider)?.isAdminAndAbove ?? false)
            IconButton(
              icon: const Icon(Icons.campaign_outlined),
              tooltip: l10n.announceTitle,
              onPressed: () => showSendAnnouncementSheet(context, ref),
            ),
          if (unread > 0)
            TextButton(
              onPressed: () => _markAllRead(context, ref),
              child: Text(l10n.notificationsMarkAllRead),
            ),
        ],
      ),
      body: SafeArea(
        child: PagedListView<AppNotification>(
          state: state,
          onRefresh: () async {
            ref.invalidate(unreadNotificationCountProvider);
            await controller.refresh();
          },
          onLoadMore: controller.loadMore,
          emptyMessage: controller.unreadOnly
              ? l10n.notificationsUnreadEmpty
              : l10n.notificationsEmpty,
          emptyIcon: Icons.notifications_none,
          header: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Row(
              children: [
                FilterChip(
                  label: Text(
                    unread > 0 ? l10n.notificationsUnreadCount(unread) : l10n.notificationsUnread,
                  ),
                  selected: controller.unreadOnly,
                  onSelected: (selected) => controller.showUnreadOnly(selected),
                ),
              ],
            ),
          ),
          itemBuilder: (context, notification) => _NotificationCard(
            notification: notification,
            onTap: () => Navigator.of(context).push(
              MaterialPageRoute<void>(
                builder: (_) => NotificationDetailScreen(notification: notification),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _markAllRead(BuildContext context, WidgetRef ref) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = context.l10n;

    try {
      await ref.read(notificationsControllerProvider.notifier).markAllRead();
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}

/// One row in the inbox — a preview only. Reading the rest means opening it;
/// see [NotificationDetailScreen].
///
/// Unread and read are meant to look obviously different at a glance, not
/// just to a careful look: unread carries the app's own accent color as a
/// left bar and a tinted background, read fades to the plain surface with
/// dimmed text — the same "this still needs you" signal a phone's own inbox
/// apps use.
class _NotificationCard extends StatelessWidget {
  const _NotificationCard({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final unread = !notification.isRead;
    final accent = theme.colorScheme.primary;
    final text = resolveNotificationText(context.l10n, notification);

    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: unread ? 1 : 0,
      color: unread
          ? theme.colorScheme.primaryContainer.withValues(alpha: 0.35)
          : theme.colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: unread ? accent.withValues(alpha: 0.4) : theme.colorScheme.outlineVariant,
        ),
      ),
      child: InkWell(
        onTap: onTap,
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // The accent bar is the one element that never depends on a
              // careful read of the row's text — it is legible from across a
              // room, which the bold-vs-regular weight difference is not.
              Container(width: 4, color: unread ? accent : Colors.transparent),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      CircleAvatar(
                        radius: 20,
                        backgroundColor: unread
                            ? theme.colorScheme.primaryContainer
                            : theme.colorScheme.surfaceContainerHighest,
                        child: Icon(
                          notificationTypeIcon(notification.type),
                          size: 20,
                          color: unread
                              ? theme.colorScheme.onPrimaryContainer
                              : theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.7),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              text.title,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: theme.textTheme.titleSmall?.copyWith(
                                fontWeight: unread ? FontWeight.w700 : FontWeight.w500,
                                color: unread
                                    ? theme.colorScheme.onSurface
                                    : theme.colorScheme.onSurfaceVariant,
                              ),
                            ),
                            const SizedBox(height: 4),
                            // A preview, not the message: seeing the whole
                            // thing here would make opening it pointless, and
                            // the point is that opening it is what "read"
                            // means now.
                            Text(
                              text.body,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: theme.textTheme.bodyMedium?.copyWith(
                                color: unread
                                    ? theme.colorScheme.onSurfaceVariant
                                    : theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.7),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              formatRelative(notification.createdAt),
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.7),
                              ),
                            ),
                          ],
                        ),
                      ),
                      if (unread)
                        Container(
                          width: 9,
                          height: 9,
                          margin: const EdgeInsets.only(left: 8, top: 4),
                          decoration: BoxDecoration(color: accent, shape: BoxShape.circle),
                        ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
