import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/app_notification.dart';
import 'notification_deep_link.dart';
import 'notification_text.dart';
import 'notification_type_icon.dart';
import 'notifications_controller.dart';
import 'pending_acknowledgments_controller.dart';

/// The full text of one notification — reaching this screen is now the only
/// way to actually read one; the inbox list shows a two-line preview and
/// nothing more, so a tap always means "show me the rest," not "I already
/// read enough to act."
class NotificationDetailScreen extends ConsumerStatefulWidget {
  const NotificationDetailScreen({super.key, required this.notification});

  final AppNotification notification;

  @override
  ConsumerState<NotificationDetailScreen> createState() =>
      _NotificationDetailScreenState();
}

class _NotificationDetailScreenState
    extends ConsumerState<NotificationDetailScreen> {
  late AppNotification _notification = widget.notification;

  @override
  void initState() {
    super.initState();
    // Opening this screen is what "read" means now — the list no longer
    // marks a row read just for having been tapped once.
    Future.microtask(
      () => ref
          .read(notificationsControllerProvider.notifier)
          .markRead(widget.notification),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final notification = _notification;

    final needsAcknowledgment = notification.requiresAcknowledgment &&
        notification.acknowledgedAt == null;

    final text = resolveNotificationText(l10n, notification);

    final target = deepLinkFor(
      notification,
      canViewDirectory: ref.watch(currentUserProvider)?.canViewDirectory ?? false,
    );

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navNotifications)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  radius: 22,
                  backgroundColor: theme.colorScheme.primaryContainer,
                  child: Icon(
                    notificationTypeIcon(notification.type),
                    color: theme.colorScheme.onPrimaryContainer,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        text.title,
                        style: theme.textTheme.titleLarge
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        enumLabel(l10n, EnumKind.notificationType, notification.type),
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 20),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  text.body,
                  style: theme.textTheme.bodyLarge,
                ),
              ),
            ),
            const SizedBox(height: 12),
            Text(
              formatDateTime(notification.createdAt),
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            if (notification.acknowledgedAt != null) ...[
              const SizedBox(height: 4),
              Text(
                l10n.notificationsAcknowledgedOn(
                  formatDateTime(notification.acknowledgedAt),
                ),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            if (needsAcknowledgment) ...[
              const SizedBox(height: 20),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () => _acknowledge(context),
                  icon: const Icon(Icons.check_circle_outline),
                  label: Text(l10n.ackConfirmButton),
                ),
              ),
            ],
            if (target != null) ...[
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: () => context.push(target),
                  icon: const Icon(Icons.open_in_new),
                  label: Text(l10n.notificationsOpenRelated),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _acknowledge(BuildContext context) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    try {
      await ref
          .read(acknowledgeControllerProvider.notifier)
          .acknowledge(_notification.id);

      if (mounted) {
        setState(() {
          _notification = _notification.copyWith(
            acknowledgedAt: DateTime.now().toUtc(),
          );
        });
      }
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}
