import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import 'pending_acknowledgments_controller.dart';

/// A strip across the top of the app while a notification the sender marked
/// "requires confirmation" has not been confirmed yet.
///
/// It only blocks mutating actions elsewhere in the app (see
/// `hasPendingAcknowledgmentProvider`) — this banner is what lets someone
/// actually clear that block, by reading the notice and tapping through it.
class AcknowledgmentBanner extends ConsumerWidget {
  const AcknowledgmentBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(pendingAcknowledgmentsProvider).value ?? const [];

    if (pending.isEmpty) {
      return const SizedBox.shrink();
    }

    final scheme = Theme.of(context).colorScheme;
    final notification = pending.first;
    final acknowledging = ref.watch(acknowledgeControllerProvider).isLoading;

    return Material(
      color: scheme.errorContainer,
      child: SafeArea(
        bottom: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Row(
            children: [
              Icon(Icons.priority_high, size: 20, color: scheme.onErrorContainer),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      context.l10n.ackBannerHeading,
                      style: Theme.of(context).textTheme.labelMedium?.copyWith(
                            color: scheme.onErrorContainer,
                            fontWeight: FontWeight.w700,
                          ),
                    ),
                    Text(
                      notification.title,
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: scheme.onErrorContainer,
                          ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              FilledButton(
                onPressed: acknowledging
                    ? null
                    : () => ref
                        .read(acknowledgeControllerProvider.notifier)
                        .acknowledge(notification.id),
                child: Text(context.l10n.ackConfirmButton),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
