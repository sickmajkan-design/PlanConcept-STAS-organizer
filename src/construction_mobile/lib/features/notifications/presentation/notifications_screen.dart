import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/app_notification.dart';
import 'notification_deep_link.dart';
import 'notifications_controller.dart';

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(notificationsControllerProvider.notifier);
    final state = ref.watch(notificationsControllerProvider);
    final unread = ref.watch(unreadNotificationCountProvider).value ?? 0;

    return Scaffold(
      appBar: AppBar(
        title: Text(context.l10n.navNotifications),
        actions: [
          if (unread > 0)
            TextButton(
              onPressed: () => _markAllRead(context, ref),
              child: Text(context.l10n.notificationsMarkAllRead),
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
              ? 'Nothing unread.'
              : 'No notifications yet.',
          emptyIcon: Icons.notifications_none,
          header: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Row(
              children: [
                FilterChip(
                  label: Text(unread > 0 ? 'Unread ($unread)' : 'Unread'),
                  selected: controller.unreadOnly,
                  onSelected: (selected) => controller.showUnreadOnly(selected),
                ),
              ],
            ),
          ),
          itemBuilder: (context, notification) => _NotificationCard(
            notification: notification,
            onTap: () => _open(context, ref, notification),
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

  Future<void> _open(
    BuildContext context,
    WidgetRef ref,
    AppNotification notification,
  ) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = context.l10n;
    final target = deepLinkFor(
      notification,
      canViewDirectory: ref.read(currentUserProvider)?.canViewDirectory ?? false,
    );

    try {
      await ref
          .read(notificationsControllerProvider.notifier)
          .markRead(notification);
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }

    if (target != null && context.mounted) {
      context.push(target);
    }
  }

}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  static IconData _iconFor(String type) => switch (type) {
        'ProjectAssigned' => Icons.apartment,
        'EmployeeAssigned' => Icons.person_add_alt,
        'VehicleAssigned' => Icons.local_shipping_outlined,
        'ToolAssigned' => Icons.handyman_outlined,
        _ => Icons.campaign_outlined,
      };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final unread = !notification.isRead;

    return Card(
      clipBehavior: Clip.antiAlias,
      color: unread ? theme.colorScheme.surfaceContainerHigh : null,
      child: InkWell(
        onTap: onTap,
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
                  _iconFor(notification.type),
                  size: 20,
                  color: unread
                      ? theme.colorScheme.onPrimaryContainer
                      : theme.colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            style: theme.textTheme.titleSmall?.copyWith(
                              fontWeight:
                                  unread ? FontWeight.w700 : FontWeight.w500,
                            ),
                          ),
                        ),
                        if (unread)
                          Container(
                            width: 8,
                            height: 8,
                            margin: const EdgeInsets.only(left: 8, top: 4),
                            decoration: BoxDecoration(
                              color: theme.colorScheme.primary,
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      notification.body,
                      style: theme.textTheme.bodyMedium,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      formatRelative(notification.createdAt),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
