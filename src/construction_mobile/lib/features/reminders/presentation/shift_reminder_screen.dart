import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../l10n/app_localizations.dart';
import '../domain/shift_reminder_settings.dart';
import 'shift_reminder_controller.dart';

/// The worker's own reminders to clock in and clock out.
class ShiftReminderScreen extends ConsumerWidget {
  const ShiftReminderScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final settings = ref.watch(shiftReminderSettingsProvider).value;

    Future<void> save(ShiftReminderSettings next) async {
      final granted = await ref.read(shiftReminderSettingsProvider.notifier).save(next);

      if (!granted && context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(l10n.shiftReminderPermissionDenied)));
      }
    }

    return Scaffold(
      appBar: AppBar(title: Text(l10n.shiftReminderTitle)),
      body: settings == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(l10n.shiftReminderIntro, style: Theme.of(context).textTheme.bodyMedium),
                const SizedBox(height: 16),
                _ReminderCard(
                  title: l10n.shiftReminderStartSwitch,
                  enabled: settings.startEnabled,
                  minutes: settings.startMinutes,
                  onEnabled: (on) => save(settings.copyWith(startEnabled: on)),
                  onMinutes: (m) => save(settings.copyWith(startMinutes: m)),
                ),
                const SizedBox(height: 12),
                _ReminderCard(
                  title: l10n.shiftReminderEndSwitch,
                  hint: l10n.shiftReminderEndHint,
                  enabled: settings.endEnabled,
                  minutes: settings.endMinutes,
                  onEnabled: (on) => save(settings.copyWith(endEnabled: on)),
                  onMinutes: (m) => save(settings.copyWith(endMinutes: m)),
                ),
                const SizedBox(height: 16),
                Text(l10n.shiftReminderDays, style: Theme.of(context).textTheme.titleSmall),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children: [
                    for (var day = 1; day <= 7; day++)
                      FilterChip(
                        label: Text(_dayName(l10n, day)),
                        selected: settings.weekdays.contains(day),
                        onSelected: (on) {
                          final next = {...settings.weekdays};
                          on ? next.add(day) : next.remove(day);
                          // At least one day: with none the reminders could never fire.
                          if (next.isNotEmpty) save(settings.copyWith(weekdays: next));
                        },
                      ),
                  ],
                ),
              ],
            ),
    );
  }

  static String _dayName(AppLocalizations l10n, int day) => switch (day) {
        1 => l10n.shiftReminderDayMon,
        2 => l10n.shiftReminderDayTue,
        3 => l10n.shiftReminderDayWed,
        4 => l10n.shiftReminderDayThu,
        5 => l10n.shiftReminderDayFri,
        6 => l10n.shiftReminderDaySat,
        _ => l10n.shiftReminderDaySun,
      };
}

class _ReminderCard extends StatelessWidget {
  const _ReminderCard({
    required this.title,
    required this.enabled,
    required this.minutes,
    required this.onEnabled,
    required this.onMinutes,
    this.hint,
  });

  final String title;
  final String? hint;
  final bool enabled;
  final int minutes;
  final ValueChanged<bool> onEnabled;
  final ValueChanged<int> onMinutes;

  @override
  Widget build(BuildContext context) {
    final time = TimeOfDay(hour: minutes ~/ 60, minute: minutes % 60);

    return Card(
      child: Column(
        children: [
          SwitchListTile(
            title: Text(title),
            subtitle: hint == null ? null : Text(hint!),
            value: enabled,
            onChanged: onEnabled,
          ),
          ListTile(
            enabled: enabled,
            leading: const Icon(Icons.schedule_outlined),
            title: Text(context.l10n.shiftReminderTime),
            trailing: Text(time.format(context), style: Theme.of(context).textTheme.titleMedium),
            onTap: !enabled
                ? null
                : () async {
                    final picked = await showTimePicker(context: context, initialTime: time);
                    if (picked != null) onMinutes(picked.hour * 60 + picked.minute);
                  },
          ),
        ],
      ),
    );
  }
}
