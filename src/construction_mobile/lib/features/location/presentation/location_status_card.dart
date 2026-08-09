import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/utils/formatting.dart';
import '../../../l10n/app_localizations.dart';
import 'location_tracking_controller.dart';

/// Makes location sharing visible to the person being tracked: whether it is
/// on, when the last position reached the office, and what to do when the
/// device is blocking it.
class LocationStatusCard extends ConsumerWidget {
  const LocationStatusCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final state = ref.watch(locationTrackingProvider);
    final theme = Theme.of(context);

    // Both halves are named rather than written out, so both come back in
    // the operator's language.
    final queued = state.queuedFailure;

    final detail = state.message?.resolve(l10n) ??
        (queued == null ? null : l10n.locationQueued(queued.describe(l10n)));

    if (state.status == LocationTrackingStatus.off) {
      return const SizedBox.shrink();
    }

    final (icon, tint, title) = switch (state.status) {
      LocationTrackingStatus.active => (
          Icons.location_on,
          theme.colorScheme.tertiary,
          l10n.locationSharingOn,
        ),
      LocationTrackingStatus.starting => (
          Icons.location_searching,
          theme.colorScheme.onSurfaceVariant,
          l10n.locationStarting,
        ),
      LocationTrackingStatus.serviceDisabled => (
          Icons.location_disabled,
          theme.colorScheme.error,
          l10n.locationServicesOff,
        ),
      LocationTrackingStatus.permissionDenied => (
          Icons.location_disabled,
          theme.colorScheme.error,
          l10n.locationPermissionDenied,
        ),
      LocationTrackingStatus.permissionBlocked => (
          Icons.location_disabled,
          theme.colorScheme.error,
          l10n.locationPermissionBlocked,
        ),
      _ => (
          Icons.warning_amber_outlined,
          theme.colorScheme.error,
          l10n.locationProblem,
        ),
    };

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, color: tint),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    title,
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              _subtitle(l10n, state),
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            if (detail != null) ...[
              const SizedBox(height: 4),
              Text(
                detail,
                style: theme.textTheme.bodySmall?.copyWith(color: tint),
              ),
            ],
            if (_needsAction(state.status)) ...[
              const SizedBox(height: 12),
              Row(
                children: [
                  OutlinedButton(
                    onPressed: () => _resolve(ref, state.status),
                    child: Text(
                      switch (state.status) {
                        LocationTrackingStatus.serviceDisabled =>
                          l10n.locationOpenSettings,
                        LocationTrackingStatus.permissionBlocked =>
                          l10n.locationOpenAppSettings,
                        _ => l10n.locationAllow,
                      },
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  static bool _needsAction(LocationTrackingStatus status) =>
      status == LocationTrackingStatus.serviceDisabled ||
      status == LocationTrackingStatus.permissionDenied ||
      status == LocationTrackingStatus.permissionBlocked ||
      status == LocationTrackingStatus.error;

  static String _subtitle(AppLocalizations l10n, LocationTrackingState state) {
    if (state.status != LocationTrackingStatus.active) {
      return l10n.locationNotShared;
    }

    final parts = <String>[
      l10n.locationSharingOnBody,
      l10n.locationLastSent(formatRelative(state.lastReportedAt)),
      // Serbian inflects this by count, so it is a plural message rather than
      // a number glued to a noun.
      if (state.pendingCount > 0) l10n.locationPending(state.pendingCount),
    ];

    return parts.join(' ');
  }

  static Future<void> _resolve(WidgetRef ref, LocationTrackingStatus status) async {
    switch (status) {
      case LocationTrackingStatus.serviceDisabled:
        await Geolocator.openLocationSettings();
      case LocationTrackingStatus.permissionBlocked:
        await Geolocator.openAppSettings();
      default:
        break;
    }

    await ref.read(locationTrackingProvider.notifier).retry();
  }
}
