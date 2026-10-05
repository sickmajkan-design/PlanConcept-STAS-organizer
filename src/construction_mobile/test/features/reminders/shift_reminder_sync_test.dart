import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/reminders/data/reminder_scheduler.dart';
import 'package:construction_mobile/features/reminders/data/shift_reminder_storage.dart';
import 'package:construction_mobile/features/reminders/domain/shift_reminder_planner.dart';
import 'package:construction_mobile/features/reminders/domain/shift_reminder_settings.dart';
import 'package:construction_mobile/features/reminders/presentation/shift_reminder_controller.dart';
import 'package:construction_mobile/features/time_entries/presentation/shift_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:construction_mobile/core/network/network_providers.dart';

/// The wiring between the worker's choices, the shift and the phone's waiting reminders.
class _RecordingScheduler implements ReminderScheduler {
  final List<List<ShiftReminderSlot>> plans = [];
  int cancelled = 0;
  bool grant = true;

  @override
  Future<bool> requestPermission() async => grant;

  @override
  Future<void> replaceAll(List<ShiftReminderSlot> slots, ShiftReminderTexts texts) async => plans.add(slots);

  @override
  Future<void> cancelAll() async => cancelled++;
}

class _MemoryStorage extends ShiftReminderStorage {
  _MemoryStorage([this.value = const ShiftReminderSettings()]);

  ShiftReminderSettings value;

  @override
  Future<ShiftReminderSettings> read() async => value;

  @override
  Future<void> write(ShiftReminderSettings settings) async => value = settings;
}

class _EmptyKeystore implements FlutterSecureStorage {
  @override
  dynamic noSuchMethod(Invocation invocation) {
    return switch (invocation.memberName) {
      #read => Future<String?>.value(),
      _ => Future<void>.value(),
    };
  }
}

class _IdleShift extends ShiftController {
  @override
  Future<ShiftState> build() async => const ShiftState();
}

const _worker = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'mirza@construction.local',
  role: 'Worker',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
  firstName: 'Mirza',
  lastName: 'E',
);

Future<void> _settle() async {
  for (var i = 0; i < 30; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

ProviderContainer _container(_RecordingScheduler scheduler, _MemoryStorage storage, {User? user = _worker}) {
  final container = ProviderContainer(
    overrides: [
      currentUserProvider.overrideWithValue(user),
      shiftControllerProvider.overrideWith(_IdleShift.new),
      reminderSchedulerProvider.overrideWithValue(scheduler),
      shiftReminderStorageProvider.overrideWithValue(storage),
      secureStorageProvider.overrideWithValue(_EmptyKeystore()),
    ],
  );
  addTearDown(container.dispose);

  return container;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('switching a reminder on puts it on the phone', () async {
    final scheduler = _RecordingScheduler();
    final container = _container(scheduler, _MemoryStorage());

    container.listen(shiftReminderSyncProvider, (_, _) {});
    await _settle();

    final granted = await container
        .read(shiftReminderSettingsProvider.notifier)
        .save(const ShiftReminderSettings(startEnabled: true));
    await _settle();

    expect(granted, isTrue);
    expect(scheduler.plans.last, isNotEmpty);
    expect(scheduler.plans.last.every((s) => s.kind == ShiftReminderKind.start), isTrue);
  });

  test('a refused permission leaves the reminder off', () async {
    final scheduler = _RecordingScheduler()..grant = false;
    final storage = _MemoryStorage();
    final container = _container(scheduler, storage);

    final granted = await container
        .read(shiftReminderSettingsProvider.notifier)
        .save(const ShiftReminderSettings(startEnabled: true));

    expect(granted, isFalse);
    expect(storage.value.startEnabled, isFalse);
  });

  test('an account that is not an employee gets nothing', () async {
    final scheduler = _RecordingScheduler();
    final container = _container(
      scheduler,
      _MemoryStorage(const ShiftReminderSettings(startEnabled: true)),
      user: const User(
        id: '019fad65-d635-76f2-880f-d8d25aea67d1',
        email: 'office@construction.local',
        role: 'Admin',
        firstName: 'O',
        lastName: 'F',
      ),
    );

    container.listen(shiftReminderSyncProvider, (_, _) {});
    await _settle();

    expect(scheduler.plans, isEmpty);
    expect(scheduler.cancelled, greaterThan(0));
  });
}
