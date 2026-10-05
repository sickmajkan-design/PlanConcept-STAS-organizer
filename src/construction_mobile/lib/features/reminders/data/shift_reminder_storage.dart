import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../domain/shift_reminder_settings.dart';

/// Where the worker's reminder choices are kept on the handset.
class ShiftReminderStorage {
  const ShiftReminderStorage();

  static const _key = 'shift_reminders.v1';

  Future<ShiftReminderSettings> read() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_key);

      return ShiftReminderSettings.fromJson(raw == null ? null : jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return const ShiftReminderSettings();
    }
  }

  Future<void> write(ShiftReminderSettings settings) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, jsonEncode(settings.toJson()));
  }
}
