import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/outbox/outbox_banner.dart';
import '../../features/outbox/outbox_controller.dart';
import '../network/offline_data_status.dart';
import 'offline_data_banner.dart';

/// The two notices about the phone's connection, above whatever screen is open.
///
/// Wrapped around the whole navigator rather than placed inside the tab shell,
/// because "these figures are old" and "two reports have not gone yet" are just
/// as true on a pushed screen — the leave request is made on one.
///
/// The screen below is told its top edge is already covered while a notice is
/// showing. Without that every screen leaves a status-bar-sized gap under the
/// strip, because both the strip and the screen's own app bar reserve room for
/// the clock and battery icons.
class StatusBanners extends ConsumerWidget {
  const StatusBanners({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final offline = ref.watch(offlineDataProvider) != null;

    return Column(
      children: [
        const OfflineDataBanner(),
        const OutboxBanner(),
        Expanded(
          child: offline || _outboxWaiting(ref)
              ? MediaQuery.removePadding(
                  context: context,
                  removeTop: true,
                  child: child,
                )
              : child,
        ),
      ],
    );
  }

  bool _outboxWaiting(WidgetRef ref) =>
      ref.watch(outboxPendingProvider) > 0;
}
