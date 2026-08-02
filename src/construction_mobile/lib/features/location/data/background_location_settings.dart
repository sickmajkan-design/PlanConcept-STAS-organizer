import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/config/app_config.dart';
import '../../../l10n/app_localizations.dart';

/// Platform settings that keep the position stream running once the app is no
/// longer the thing on screen.
///
/// This is the whole difference between the old behaviour and the new one.
/// Reporting used to be a `Timer` calling `getCurrentPosition`, which stops the
/// moment the framework stops servicing timers — so a phone in a pocket
/// reported nothing, which is the only state a site worker's phone is ever in.
///
/// On Android the stream is attached to a location-typed foreground service.
/// On Apple platforms it is background location updates with the status-bar
/// indicator left on. Both are visible to the person carrying the phone by
/// design: a workforce tracker that can hide is a workforce tracker nobody
/// should install.
///
/// Split out from the controller so the choice can be asserted in tests —
/// the settings object is pure data, while the stream it feeds is not.
LocationSettings backgroundLocationSettings(
  AppLocalizations l10n, {
  TargetPlatform? platform,
}) {
  switch (platform ?? defaultTargetPlatform) {
    case TargetPlatform.android:
      return AndroidSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: AppConfig.locationDistanceFilterMetres,
        intervalDuration: AppConfig.locationReportInterval,
        foregroundNotificationConfig: ForegroundNotificationConfig(
          notificationTitle: l10n.locationServiceNotificationTitle,
          notificationText: l10n.locationServiceNotificationBody,
          notificationChannelName: l10n.locationServiceChannelName,
          // Without the wake lock the fix can be cut short once the device
          // dozes, which on a quiet site is most of the day.
          enableWakeLock: true,
          // Not swipeable: dismissing it would silently end the shift's
          // tracking, and the office would have no way to tell.
          setOngoing: true,
        ),
      );

    case TargetPlatform.iOS:
    case TargetPlatform.macOS:
      return AppleSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: AppConfig.locationDistanceFilterMetres,
        // `other` lets iOS pause updates when it decides the device is
        // stationary; a worker standing at one spot still needs to be
        // reported as being there.
        activityType: ActivityType.otherNavigation,
        pauseLocationUpdatesAutomatically: false,
        showBackgroundLocationIndicator: true,
        allowBackgroundLocationUpdates: true,
      );

    default:
      return const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: AppConfig.locationDistanceFilterMetres,
      );
  }
}
