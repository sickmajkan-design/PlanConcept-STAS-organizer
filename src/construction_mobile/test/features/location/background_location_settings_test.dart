import 'package:construction_mobile/core/config/app_config.dart';
import 'package:construction_mobile/features/location/data/background_location_settings.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

/// These assert one thing: that the settings which make reporting survive the
/// app leaving the screen are actually asked for. Nothing else in the suite
/// would notice if they were dropped — the app would build, analyse and test
/// green, and simply stop reporting the moment a phone went into a pocket,
/// which is what it did before.
///
/// Asserted through `toJson`, which is the payload that actually crosses the
/// platform channel, rather than by casting to the per-platform setting
/// classes: those live in packages this one only depends on transitively.
void main() {
  late AppLocalizations sr;
  late AppLocalizations en;

  Map<String, dynamic> settingsFor(
    AppLocalizations l10n,
    TargetPlatform platform,
  ) =>
      backgroundLocationSettings(l10n, platform: platform).toJson();

  setUp(() async {
    sr = await AppLocalizations.delegate.load(const Locale('sr'));
    en = await AppLocalizations.delegate.load(const Locale('en'));
  });

  group('Android', () {
    test('runs as a foreground service', () {
      // Without this the stream dies with the activity, which was the bug.
      expect(
        settingsFor(sr, TargetPlatform.android)['foregroundNotificationConfig'],
        isNotNull,
      );
    });

    test('holds a wake lock and keeps the notification undismissable', () {
      final config = settingsFor(sr, TargetPlatform.android)[
          'foregroundNotificationConfig'] as Map<String, dynamic>;

      // Dozing would cut the fix short; a swipeable notification would let
      // the shift's tracking end without anyone knowing.
      expect(config['enableWakeLock'], isTrue);
      expect(config['setOngoing'], isTrue);
    });

    test('labels the notification in the language the worker chose', () {
      final serbian = settingsFor(sr, TargetPlatform.android)[
          'foregroundNotificationConfig'] as Map<String, dynamic>;
      final english = settingsFor(en, TargetPlatform.android)[
          'foregroundNotificationConfig'] as Map<String, dynamic>;

      expect(serbian['notificationTitle'], sr.locationServiceNotificationTitle);
      expect(english['notificationTitle'], en.locationServiceNotificationTitle);
      expect(serbian['notificationTitle'], isNot(english['notificationTitle']));
      expect(
        serbian['notificationChannelName'],
        sr.locationServiceChannelName,
      );
    });

    test('asks the platform for the configured interval and distance', () {
      final settings = settingsFor(sr, TargetPlatform.android);

      expect(
        settings['timeInterval'],
        AppConfig.locationReportInterval.inMilliseconds,
      );
      expect(settings['distanceFilter'], AppConfig.locationDistanceFilterMetres);
    });
  });

  group('Apple', () {
    test('allows background updates and shows the indicator', () {
      final settings = settingsFor(sr, TargetPlatform.iOS);

      expect(settings['allowBackgroundLocationUpdates'], isTrue);
      // Tracking a person must never be invisible to that person.
      expect(settings['showBackgroundLocationIndicator'], isTrue);
    });

    test('does not let iOS pause updates for a stationary worker', () {
      // Someone standing at one spot on site still has to be reported as
      // being there.
      expect(
        settingsFor(sr, TargetPlatform.iOS)['pauseLocationUpdatesAutomatically'],
        isFalse,
      );
    });
  });

  test('an unsupported platform still gets plain settings, not a crash', () {
    final settings = settingsFor(sr, TargetPlatform.linux);

    expect(settings['distanceFilter'], AppConfig.locationDistanceFilterMetres);
    expect(settings.containsKey('foregroundNotificationConfig'), isFalse);
  });
}
