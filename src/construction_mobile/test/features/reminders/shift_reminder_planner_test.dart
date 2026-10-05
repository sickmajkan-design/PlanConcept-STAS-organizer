import 'package:construction_mobile/features/reminders/domain/shift_reminder_planner.dart';
import 'package:construction_mobile/features/reminders/domain/shift_reminder_settings.dart';
import 'package:flutter_test/flutter_test.dart';

/// Which reminders should be waiting on the phone. The phone cannot look at the shift when one
/// goes off, so the plan is the whole rule, and it is made again whenever the shift changes.
void main() {
  // Monday 5 October 2026, 06:00.
  final monday6 = DateTime(2026, 10, 5, 6);

  ShiftReminderSettings both({Set<int>? days}) => ShiftReminderSettings(
        startEnabled: true,
        endEnabled: true,
        startMinutes: 7 * 60,
        endMinutes: 16 * 60,
        weekdays: days ?? const {1, 2, 3, 4, 5},
      );

  List<ShiftReminderSlot> starts(List<ShiftReminderSlot> slots) =>
      slots.where((s) => s.kind == ShiftReminderKind.start).toList();

  test('nothing is planned until a reminder is switched on', () {
    final slots = ShiftReminderPlanner.plan(
      settings: const ShiftReminderSettings(),
      now: monday6,
      isOnShift: false,
    );

    expect(slots, isEmpty);
  });

  test('clock-in is planned for each chosen weekday ahead, and not for the weekend', () {
    final slots = ShiftReminderPlanner.plan(settings: both(), now: monday6, isOnShift: false);

    final days = starts(slots).map((s) => s.at.weekday).toSet();

    expect(days, {1, 2, 3, 4, 5});
    expect(starts(slots).first.at, DateTime(2026, 10, 5, 7));
    // Two working weeks: 10 weekdays in the 14 days ahead.
    expect(starts(slots), hasLength(10));
  });

  test('a clock-in time that has already passed today is skipped', () {
    final slots = ShiftReminderPlanner.plan(
      settings: both(),
      now: DateTime(2026, 10, 5, 8),
      isOnShift: false,
    );

    expect(starts(slots).first.at, DateTime(2026, 10, 6, 7));
  });

  test('already on shift: no clock-in for today, but tomorrow still gets one', () {
    final slots = ShiftReminderPlanner.plan(settings: both(), now: monday6, isOnShift: true);

    expect(starts(slots).first.at, DateTime(2026, 10, 6, 7));
  });

  test('clock-out is planned for today only, and only while on shift', () {
    final on = ShiftReminderPlanner.plan(settings: both(), now: DateTime(2026, 10, 5, 9), isOnShift: true);
    final off = ShiftReminderPlanner.plan(settings: both(), now: DateTime(2026, 10, 5, 9), isOnShift: false);

    final ends = on.where((s) => s.kind == ShiftReminderKind.end).toList();

    expect(ends.single.at, DateTime(2026, 10, 5, 16));
    expect(off.where((s) => s.kind == ShiftReminderKind.end), isEmpty);
  });

  test('clock-out still fires on a day that is not one of the chosen ones', () {
    // Saturday 10 October, on shift.
    final slots = ShiftReminderPlanner.plan(settings: both(), now: DateTime(2026, 10, 10, 9), isOnShift: true);

    expect(slots.where((s) => s.kind == ShiftReminderKind.end), hasLength(1));
  });

  test('clock-out time already passed: nothing to plan', () {
    final slots = ShiftReminderPlanner.plan(settings: both(), now: DateTime(2026, 10, 5, 17), isOnShift: true);

    expect(slots.where((s) => s.kind == ShiftReminderKind.end), isEmpty);
  });

  test('planning twice gives the same ids, so the phone replaces rather than piles up', () {
    final a = ShiftReminderPlanner.plan(settings: both(), now: monday6, isOnShift: false);
    final b = ShiftReminderPlanner.plan(settings: both(), now: monday6, isOnShift: false);

    expect(a, b);
    expect(a.map((s) => s.id).toSet(), hasLength(a.length));
  });

  test('a clock change cannot move a reminder off its day', () {
    // The last Sunday of October 2026 is when clocks go back in Europe; the next day must still
    // be Monday 07:00 local.
    final slots = ShiftReminderPlanner.plan(
      settings: both(),
      now: DateTime(2026, 10, 25, 20),
      isOnShift: false,
    );

    expect(starts(slots).first.at, DateTime(2026, 10, 26, 7));
  });

  group('the saved choices', () {
    test('survive a round trip', () {
      final saved = both(days: {1, 3}).copyWith(startMinutes: 6 * 60 + 30);

      final read = ShiftReminderSettings.fromJson(saved.toJson());

      expect(read.startMinutes, 390);
      expect(read.weekdays, {1, 3});
      expect(read.startEnabled, isTrue);
    });

    test('a damaged value falls back to usable defaults', () {
      final read = ShiftReminderSettings.fromJson({
        'startEnabled': true,
        'startMinutes': 99999,
        'weekdays': [0, 9],
      });

      expect(read.startMinutes, 7 * 60);
      expect(read.weekdays, {1, 2, 3, 4, 5});
    });

    test('nothing stored reads as everything off', () {
      expect(ShiftReminderSettings.fromJson(null).anyEnabled, isFalse);
    });
  });
}
