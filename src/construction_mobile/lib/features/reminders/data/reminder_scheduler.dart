import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_timezone/flutter_timezone.dart';
import 'package:timezone/data/latest_all.dart' as tzdata;
import 'package:timezone/timezone.dart' as tz;

import '../domain/shift_reminder_planner.dart';

/// What a reminder says, in the worker's language.
class ShiftReminderTexts {
  const ShiftReminderTexts({
    required this.startTitle,
    required this.startBody,
    required this.endTitle,
    required this.endBody,
    required this.channelName,
  });

  final String startTitle;
  final String startBody;
  final String endTitle;
  final String endBody;
  final String channelName;
}

/// Puts reminders on the phone. A seam, so the rules can be tested without a handset.
abstract class ReminderScheduler {
  /// Asks for permission to show notifications. False when refused.
  Future<bool> requestPermission();

  /// Makes the phone's waiting reminders exactly [slots], dropping any others.
  Future<void> replaceAll(List<ShiftReminderSlot> slots, ShiftReminderTexts texts);

  Future<void> cancelAll();
}

class LocalNotificationsScheduler implements ReminderScheduler {
  LocalNotificationsScheduler([FlutterLocalNotificationsPlugin? plugin])
      : _plugin = plugin ?? FlutterLocalNotificationsPlugin();

  final FlutterLocalNotificationsPlugin _plugin;
  bool _ready = false;

  static const _channelId = 'shift_reminders';

  Future<void> _init() async {
    if (_ready) return;

    tzdata.initializeTimeZones();
    try {
      final zone = await FlutterTimezone.getLocalTimezone();
      tz.setLocalLocation(tz.getLocation(zone.identifier));
    } catch (_) {
      // An unknown zone name leaves the default (UTC), which would shift every reminder, so
      // fall back to the offset the handset reports right now instead.
      final offset = DateTime.now().timeZoneOffset;
      tz.setLocalLocation(tz.Location('device', [tz.minTime], [0], [tz.TimeZone(offset, isDst: false, abbreviation: 'LOC')]));
    }

    await _plugin.initialize(
      settings: const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      ),
    );
    _ready = true;
  }

  @override
  Future<bool> requestPermission() async {
    try {
      await _init();
      final android = _plugin.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>();

      return await android?.requestNotificationsPermission() ?? true;
    } catch (e) {
      debugPrint('Reminder permission failed: $e');
      return false;
    }
  }

  @override
  Future<void> replaceAll(List<ShiftReminderSlot> slots, ShiftReminderTexts texts) async {
    try {
      await _init();
      await _plugin.cancelAll();

      for (final slot in slots) {
        final start = slot.kind == ShiftReminderKind.start;

        await _plugin.zonedSchedule(
          id: slot.id,
          title: start ? texts.startTitle : texts.endTitle,
          body: start ? texts.startBody : texts.endBody,
          scheduledDate: tz.TZDateTime.from(slot.at, tz.local),
          notificationDetails: NotificationDetails(
            android: AndroidNotificationDetails(
              _channelId,
              texts.channelName,
              importance: Importance.high,
              priority: Priority.high,
            ),
          ),
          // Inexact: no special "alarms" permission, and a reminder a few minutes late is fine.
          androidScheduleMode: AndroidScheduleMode.inexactAllowWhileIdle,
        );
      }
    } catch (e) {
      // A reminder that cannot be set must never take the app down with it.
      debugPrint('Scheduling reminders failed: $e');
    }
  }

  @override
  Future<void> cancelAll() async {
    try {
      await _init();
      await _plugin.cancelAll();
    } catch (e) {
      debugPrint('Cancelling reminders failed: $e');
    }
  }
}
