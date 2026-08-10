import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../l10n/app_locales.dart';
import '../network/offline_data_status.dart';
import '../utils/formatting.dart';

/// A strip across the top of the app while the screens below are being drawn
/// from the cache rather than from the server.
///
/// The moment matters more than the fact. "Offline" on its own leaves a
/// foreman guessing whether the crew list in front of him is from ten minutes
/// ago or from Tuesday; the time it was saved settles it, and settles it in
/// the only unit anybody on a site thinks in.
///
/// It shows nothing at all when the data is live, which is the normal case —
/// a permanent connection indicator would be noise on every screen for the
/// benefit of the rare one.
class OfflineDataBanner extends ConsumerWidget {
  const OfflineDataBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final savedAt = ref.watch(offlineDataProvider);

    if (savedAt == null) {
      return const SizedBox.shrink();
    }

    final scheme = Theme.of(context).colorScheme;
    final local = savedAt.toLocal();
    final now = DateTime.now();
    final isToday = local.year == now.year &&
        local.month == now.month &&
        local.day == now.day;

    // Today is the overwhelming case and needs no date; anything older needs
    // one, because "saved at 07:14" without a day is how a week-old roster
    // gets mistaken for this morning's.
    final message = isToday
        ? context.l10n.offlineDataNoticeTime(formatTime(local))
        : context.l10n.offlineDataNoticeDate(formatDateTime(local));

    return Material(
      color: scheme.secondaryContainer,
      child: SafeArea(
        bottom: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            children: [
              Icon(Icons.cloud_off, size: 18, color: scheme.onSecondaryContainer),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  message,
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
