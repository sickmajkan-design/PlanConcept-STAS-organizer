import 'shift_reminder_settings.dart';

enum ShiftReminderKind { start, end }

/// One reminder to be shown at [at] (the handset's local time).
class ShiftReminderSlot {
  const ShiftReminderSlot({required this.id, required this.kind, required this.at});

  /// Stable for a day and a kind, so planning again replaces rather than piles up.
  final int id;
  final ShiftReminderKind kind;
  final DateTime at;

  @override
  bool operator ==(Object other) =>
      other is ShiftReminderSlot && other.id == id && other.kind == kind && other.at == at;

  @override
  int get hashCode => Object.hash(id, kind, at);

  @override
  String toString() => 'ShiftReminderSlot($kind, $at)';
}

/// Works out which reminders should be waiting on the phone right now.
///
/// A scheduled notification cannot look at the shift when it goes off, so the plan is made from
/// what is known now and made again whenever that changes (clocking in or out, new settings):
///
///  * "clock in" is planned for every chosen weekday ahead, except today once the worker is
///    already on shift;
///  * "clock out" is planned only for today, and only while the worker is on shift — a reminder
///    to leave a shift that was never started would be noise.
class ShiftReminderPlanner {
  const ShiftReminderPlanner._();

  /// How many days ahead "clock in" is scheduled. The plan is redone every time the app opens,
  /// so this only has to outlast a stretch of days without opening it.
  static const horizonDays = 14;

  static int _id(DateTime day, ShiftReminderKind kind) =>
      (day.year * 10000 + day.month * 100 + day.day) * 10 + (kind == ShiftReminderKind.start ? 1 : 2);

  static List<ShiftReminderSlot> plan({
    required ShiftReminderSettings settings,
    required DateTime now,
    required bool isOnShift,
  }) {
    final slots = <ShiftReminderSlot>[];
    final today = DateTime(now.year, now.month, now.day);

    for (var offset = 0; offset < horizonDays; offset++) {
      // Calendar arithmetic rather than adding 24 hours, so a clock change cannot skip a day.
      final day = DateTime(today.year, today.month, today.day + offset);

      if (!settings.startEnabled || !settings.weekdays.contains(day.weekday)) continue;
      if (offset == 0 && isOnShift) continue;

      final at = day.add(Duration(minutes: settings.startMinutes));
      if (at.isAfter(now)) {
        slots.add(ShiftReminderSlot(id: _id(day, ShiftReminderKind.start), kind: ShiftReminderKind.start, at: at));
      }
    }

    // Whoever is on shift today is told to clock out, whatever day of the week it is: working a
    // Saturday does not make the reminder less useful.
    if (settings.endEnabled && isOnShift) {
      final at = today.add(Duration(minutes: settings.endMinutes));
      if (at.isAfter(now)) {
        slots.add(ShiftReminderSlot(id: _id(today, ShiftReminderKind.end), kind: ShiftReminderKind.end, at: at));
      }
    }

    return slots;
  }
}
