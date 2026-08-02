import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/utils/formatting.dart';
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

    // Either an app message we can translate, or server text we cannot.
    final detail = state.message?.resolve(l10n) ??
        (state.queuedReason == null
            ? null
            : l10n.locationQueued(state.queuedReason!));

    if (state.status == LocationTrackingStatus.off) {
      return const SizedBox.shrink();
    }

    final (icon, tint, title) = switch (state.status) {
      LocationTrackingStatus.active => (
          Icons.location_on,
          theme.colorScheme.tertiary,
          'Location sharing is on',
        ),
      LocationTrackingStatus.starting => (
          Icons.location_searching,
          theme.colorScheme.onSurfaceVariant,
          'Starting location sharing…',
        ),
      LocationTrackingStatus.serviceDisabled => (
          Icons.location_disabled,
          theme.colorScheme.error,
          'Location services are switched off',
        ),
      LocationTrackingStatus.permissionDenied => (
          Icons.location_disabled,
          theme.colorScheme.error,
          'Location permission not granted',
        ),
      LocationTrackingStatus.permissionBlocked => (
          Icons.location_disabled,
          theme.colorScheme.error,
          'Location permission is blocked',
        ),
      _ => (
          Icons.warning_amber_outlined,
          theme.colorScheme.error,
          'Location sharing has a problem',
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
              _subtitle(state),
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
                      state.status == LocationTrackingStatus.serviceDisabled
                          ? 'Open location settings'
                          : state.status ==
                                  LocationTrackingStatus.permissionBlocked
                              ? 'Open app settings'
                              : 'Allow location',
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

  static String _subtitle(LocationTrackingState state) {
    if (state.status != LocationTrackingStatus.active) {
      return 'Your position is not being shared with the office.';
    }

    final pending = state.pendingCount > 0
        ? ' · ${state.pendingCount} waiting to send'
        : '';

    return 'Your position is sent to the office every minute while you are '
        'signed in. Last sent ${formatRelative(state.lastReportedAt)}$pending.';
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
