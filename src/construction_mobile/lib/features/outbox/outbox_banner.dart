import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/l10n/api_failure_text.dart';
import '../../core/l10n/app_locales.dart';
import '../../core/outbox/outbox_queue.dart';
import 'outbox_controller.dart';

/// A strip across the top while reports or leave requests made with no signal
/// are still on the phone, and a one-off notice when one of them is refused.
///
/// Shown for the phone rather than for a page, like the offline-data strip: a
/// person who filed a defect and then moved on to another tab should still be
/// able to see that it has not gone yet, and hear about it if the office's
/// server turned it down.
class OutboxBanner extends ConsumerWidget {
  const OutboxBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen<OutboxRefusal?>(
      outboxControllerProvider.select((state) => state.refusal),
      (previous, refusal) {
        if (refusal == null) {
          return;
        }

        final l10n = context.l10n;
        final reason = refusal.failure.describe(l10n);

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              refusal.kind == OutboxKind.defect
                  ? l10n.outboxDefectRefused(reason)
                  : l10n.outboxAbsenceRefused(reason),
            ),
            duration: const Duration(seconds: 10),
          ),
        );

        ref.read(outboxControllerProvider.notifier).refusalShown();
      },
    );

    final count = ref.watch(outboxControllerProvider.select((s) => s.pendingCount));

    if (count == 0) {
      return const SizedBox.shrink();
    }

    final scheme = Theme.of(context).colorScheme;

    return Material(
      color: scheme.secondaryContainer,
      child: SafeArea(
        bottom: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            children: [
              Icon(Icons.outbox_outlined, size: 18, color: scheme.onSecondaryContainer),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  context.l10n.outboxWaiting(count),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: scheme.onSecondaryContainer,
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
