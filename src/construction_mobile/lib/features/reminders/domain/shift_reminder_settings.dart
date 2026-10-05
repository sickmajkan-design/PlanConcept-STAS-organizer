/// What the worker asked to be reminded of, set on their own phone.
///
/// Kept on the handset rather than on the server: it is a personal habit (when this person
/// leaves home), and a reminder has to fire with no signal.
class ShiftReminderSettings {
  const ShiftReminderSettings({
    this.startEnabled = false,
    this.startMinutes = 7 * 60,
    this.endEnabled = false,
    this.endMinutes = 16 * 60,
    this.weekdays = const {1, 2, 3, 4, 5},
  });

  /// Remind to clock in at [startMinutes].
  final bool startEnabled;

  /// Minutes after midnight, local time.
  final int startMinutes;

  /// Remind to clock out at [endMinutes], only while still clocked in.
  final bool endEnabled;
  final int endMinutes;

  /// `DateTime.weekday` values, 1 = Monday … 7 = Sunday.
  final Set<int> weekdays;

  bool get anyEnabled => startEnabled || endEnabled;

  ShiftReminderSettings copyWith({
    bool? startEnabled,
    int? startMinutes,
    bool? endEnabled,
    int? endMinutes,
    Set<int>? weekdays,
  }) {
    return ShiftReminderSettings(
      startEnabled: startEnabled ?? this.startEnabled,
      startMinutes: startMinutes ?? this.startMinutes,
      endEnabled: endEnabled ?? this.endEnabled,
      endMinutes: endMinutes ?? this.endMinutes,
      weekdays: weekdays ?? this.weekdays,
    );
  }

  Map<String, dynamic> toJson() => {
        'startEnabled': startEnabled,
        'startMinutes': startMinutes,
        'endEnabled': endEnabled,
        'endMinutes': endMinutes,
        'weekdays': weekdays.toList()..sort(),
      };

  /// Tolerant of a missing or damaged value: a handset never ends up with no settings at all.
  factory ShiftReminderSettings.fromJson(Map<String, dynamic>? json) {
    if (json == null) return const ShiftReminderSettings();

    int minutes(Object? value, int fallback) =>
        value is int && value >= 0 && value < 24 * 60 ? value : fallback;

    final days = (json['weekdays'] as List<dynamic>? ?? const [])
        .whereType<int>()
        .where((d) => d >= 1 && d <= 7)
        .toSet();

    return ShiftReminderSettings(
      startEnabled: json['startEnabled'] == true,
      startMinutes: minutes(json['startMinutes'], 7 * 60),
      endEnabled: json['endEnabled'] == true,
      endMinutes: minutes(json['endMinutes'], 16 * 60),
      weekdays: days.isEmpty ? const {1, 2, 3, 4, 5} : days,
    );
  }
}
