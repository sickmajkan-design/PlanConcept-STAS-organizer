import 'dart:async';

import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/location/data/location_queue.dart';
import 'package:construction_mobile/features/location/data/location_repository.dart';
import 'package:construction_mobile/features/location/presentation/location_tracking_controller.dart';
import 'package:construction_mobile/features/time_entries/data/clock_queue.dart';
import 'package:construction_mobile/features/time_entries/presentation/shift_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';

/// The office may know where somebody is only while they are on a shift.
///
/// Tracking used to follow the sign-in: a worker who stayed signed in after
/// work, overnight or over a weekend kept reporting every minute. It now starts
/// when the shift does and stops when it ends — the same fact the payroll rests
/// on — and anything still waiting to be delivered when the shift ends is sent,
/// because it was taken during it.
class _FakeGeolocator extends GeolocatorPlatform {
  int streamsOpened = 0;
  int streamsCancelled = 0;

  @override
  Future<bool> isLocationServiceEnabled() async => true;

  @override
  Future<LocationPermission> checkPermission() async =>
      LocationPermission.always;

  @override
  Future<LocationPermission> requestPermission() async =>
      LocationPermission.always;

  @override
  Stream<Position> getPositionStream({LocationSettings? locationSettings}) {
    streamsOpened++;

    late StreamController<Position> controller;
    controller = StreamController<Position>(onCancel: () => streamsCancelled++);

    return controller.stream;
  }
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

class _MemoryQueueStore implements LocationQueueStore {
  String? value;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async => value = written;

  @override
  Future<void> clear() async => value = null;
}

/// Records what was sent, and accepts it.
class _RecordingRepository implements LocationRepository {
  final List<List<LocationPing>> batches = [];

  @override
  Future<void> report(List<LocationPing> pings) async => batches.add(pings);

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

const _employee = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'ivan@construction.local',
  role: 'Foreman',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
  firstName: 'Ivan',
  lastName: 'Horvat',
);

/// Whether the worker is on a shift, set by the test.
class _OnShift extends Notifier<bool> {
  @override
  bool build() => false;

  void set(bool value) => state = value;
}

final _onShiftProvider = NotifierProvider<_OnShift, bool>(_OnShift.new);

/// A shift controller that reports a running shift exactly when the test says.
class _FakeShift extends ShiftController {
  @override
  Future<ShiftState> build() async {
    final running = ref.watch(_onShiftProvider);

    return running
        ? ShiftState(
            queued: PendingClockAction(
              action: ClockAction.clockIn,
              occurredAt: DateTime.utc(2026, 9, 25, 7),
              idempotencyKey: 'k',
            ),
          )
        : const ShiftState();
  }
}

ProviderContainer _container({
  required _RecordingRepository repository,
  required _MemoryQueueStore store,
}) {
  final container = ProviderContainer(
    overrides: [
      currentUserProvider.overrideWithValue(_employee),
      shiftControllerProvider.overrideWith(_FakeShift.new),
      locationQueueProvider.overrideWithValue(LocationQueue(store)),
      locationRepositoryProvider.overrideWithValue(repository),
      secureStorageProvider.overrideWithValue(_EmptyKeystore()),
    ],
  );
  addTearDown(container.dispose);

  return container;
}

Future<void> _settle() async {
  for (var i = 0; i < 20; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('signed in but not on a shift: nothing is captured', () async {
    final geolocator = _FakeGeolocator();
    GeolocatorPlatform.instance = geolocator;

    final container = _container(
      repository: _RecordingRepository(),
      store: _MemoryQueueStore(),
    );

    container.read(locationTrackingProvider);
    await _settle();

    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.offShift,
    );
    expect(
      geolocator.streamsOpened,
      0,
      reason: 'a worker who is not at work is not tracked',
    );
  });

  test('clocking in starts the capture and clocking out stops it', () async {
    final geolocator = _FakeGeolocator();
    GeolocatorPlatform.instance = geolocator;

    final container = _container(
      repository: _RecordingRepository(),
      store: _MemoryQueueStore(),
    );

    container.read(locationTrackingProvider);
    await _settle();
    expect(geolocator.streamsOpened, 0);

    container.read(_onShiftProvider.notifier).set(true);
    await container.read(shiftControllerProvider.future);
    container.read(locationTrackingProvider);
    await _settle();

    expect(geolocator.streamsOpened, 1);
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.active,
    );

    container.read(_onShiftProvider.notifier).set(false);
    await container.read(shiftControllerProvider.future);
    container.read(locationTrackingProvider);
    await _settle();

    expect(
      geolocator.streamsCancelled,
      1,
      reason: 'the stream must not outlive the shift',
    );
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.offShift,
    );

    // The next shift starts it again. A start that had been overtaken by the
    // first clock-out must not have left the controller unable to begin.
    container.read(_onShiftProvider.notifier).set(true);
    await container.read(shiftControllerProvider.future);
    container.read(locationTrackingProvider);
    await _settle();

    expect(geolocator.streamsOpened, 2);
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.active,
    );
  });

  test('fixes taken during the shift but not yet delivered still go out afterwards',
      () async {
    GeolocatorPlatform.instance = _FakeGeolocator();

    final repository = _RecordingRepository();
    final store = _MemoryQueueStore();

    // What the last shift captured with no coverage on site.
    final queue = LocationQueue(store);
    await queue.add(
      LocationPing(
        latitude: 45.8,
        longitude: 15.9,
        accuracy: 5,
        timestamp: DateTime.now().toUtc(),
      ),
    );

    final container = _container(repository: repository, store: store);

    container.read(locationTrackingProvider);
    await _settle();

    expect(repository.batches, hasLength(1));
    expect(repository.batches.single, hasLength(1));
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.offShift,
      reason: 'delivering the leftovers does not switch tracking on',
    );
  });
}
