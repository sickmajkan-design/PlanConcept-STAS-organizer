import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../time_entries/presentation/shift_controller.dart';
import '../data/reminder_scheduler.dart';
import '../data/shift_reminder_storage.dart';
import '../domain/shift_reminder_planner.dart';
import '../domain/shift_reminder_settings.dart';

final reminderSchedulerProvider = Provider<ReminderScheduler>((ref) => LocalNotificationsScheduler());

final shiftReminderStorageProvider = Provider<ShiftReminderStorage>((ref) => const ShiftReminderStorage());

/// The worker's reminder choices, kept on this phone.
class ShiftReminderSettingsController extends AsyncNotifier<ShiftReminderSettings> {
  @override
  Future<ShiftReminderSettings> build() => ref.read(shiftReminderStorageProvider).read();

  /// Saves the choices. Turning a reminder on asks for permission to show notifications first;
  /// when it is refused the reminder stays off, so the switch never promises what the phone
  /// will not do. Returns false in that case.
  Future<bool> save(ShiftReminderSettings next) async {
    final current = state.value ?? const ShiftReminderSettings();
    final turnedOn = (next.startEnabled && !current.startEnabled) || (next.endEnabled && !current.endEnabled);

    var settings = next;
    var granted = true;

    if (turnedOn) {
      granted = await ref.read(reminderSchedulerProvider).requestPermission();

      if (!granted) {
        settings = next.copyWith(
          startEnabled: current.startEnabled,
          endEnabled: current.endEnabled,
        );
      }
    }

    await ref.read(shiftReminderStorageProvider).write(settings);
    state = AsyncData(settings);

    return granted;
  }
}

final shiftReminderSettingsProvider =
    AsyncNotifierProvider<ShiftReminderSettingsController, ShiftReminderSettings>(
  ShiftReminderSettingsController.new,
);

/// Keeps the phone's waiting reminders in step with the choices and with the shift.
///
/// Read once from the signed-in shell. It plans again whenever the worker clocks in or out, the
/// choices change, the language changes, or somebody else signs in.
final shiftReminderSyncProvider = Provider<void>((ref) {
  final user = ref.watch(currentUserProvider);
  final settings = ref.watch(shiftReminderSettingsProvider).value;
  final shift = ref.watch(shiftControllerProvider).value;
  final locale = ref.watch(localeControllerProvider).value;
  final scheduler = ref.read(reminderSchedulerProvider);

  if (user == null || !user.isEmployee) {
    unawaited(scheduler.cancelAll());
    return;
  }

  // Not known yet: planning from a guess would clear a reminder that is still wanted.
  if (settings == null || shift == null) return;

  final slots = ShiftReminderPlanner.plan(
    settings: settings,
    now: DateTime.now(),
    isOnShift: shift.isRunning,
  );

  unawaited(() async {
    final l10n = await AppLocalizations.delegate.load(resolveLocale(locale, supportedLocales));

    await scheduler.replaceAll(
      slots,
      ShiftReminderTexts(
        startTitle: l10n.shiftReminderStartTitle,
        startBody: l10n.shiftReminderStartBody,
        endTitle: l10n.shiftReminderEndTitle,
        endBody: l10n.shiftReminderEndBody,
        channelName: l10n.shiftReminderChannel,
      ),
    );
  }());
});
