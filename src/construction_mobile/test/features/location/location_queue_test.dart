import 'dart:convert';

import 'package:construction_mobile/core/config/app_config.dart';
import 'package:construction_mobile/features/location/data/location_queue.dart';
import 'package:construction_mobile/features/location/data/location_repository.dart';
import 'package:flutter_test/flutter_test.dart';

/// Stands in for the platform keystore, and records what was written so a
/// test can assert the queue survives a restart rather than just claiming it.
class _FakeStore implements LocationQueueStore {
  String? value;
  int writes = 0;
  int clears = 0;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async {
    value = written;
    writes++;
  }

  @override
  Future<void> clear() async {
    value = null;
    clears++;
  }
}

LocationPing _ping(DateTime at, {double latitude = 44.8}) => LocationPing(
      latitude: latitude,
      longitude: 20.4,
      accuracy: 5,
      timestamp: at,
    );

void main() {
  final now = DateTime.utc(2026, 8, 2, 12);

  group('persistence', () {
    test('a fix captured before a restart is still there after it', () async {
      // The whole point of the queue: Android reclaiming the process
      // mid-shift used to take the day's undelivered fixes with it.
      final store = _FakeStore();
      final before = LocationQueue(store);

      await before.add(_ping(now), now: now);

      final after = LocationQueue(store);
      await after.restore(now: now);

      expect(after.length, 1);
      expect(after.pending.single.latitude, 44.8);
    });

    test('acknowledging what the API took leaves the rest queued', () async {
      final store = _FakeStore();
      final queue = LocationQueue(store);

      await queue.add(_ping(now, latitude: 1), now: now);
      await queue.add(_ping(now, latitude: 2), now: now);
      await queue.add(_ping(now, latitude: 3), now: now);

      // Two were in flight; the third arrived while they were being sent.
      await queue.acknowledge(2);

      expect(queue.length, 1);
      expect(queue.pending.single.latitude, 3);

      final restored = LocationQueue(store);
      await restored.restore(now: now);
      expect(restored.pending.single.latitude, 3);
    });

    test('an emptied queue leaves nothing behind in storage', () async {
      final store = _FakeStore();
      final queue = LocationQueue(store);

      await queue.add(_ping(now), now: now);
      await queue.acknowledge(1);

      expect(store.value, isNull);
    });

    test('restoring twice does not double the queue', () async {
      final store = _FakeStore();
      final queue = LocationQueue(store);

      await queue.add(_ping(now), now: now);

      final restored = LocationQueue(store);
      await restored.restore(now: now);
      await restored.restore(now: now);

      expect(restored.length, 1);
    });
  });

  group('bounds', () {
    test('never grows past what the API will accept', () async {
      final queue = LocationQueue(_FakeStore());

      for (var i = 0; i < LocationRepository.maxBatchSize + 25; i++) {
        await queue.add(_ping(now, latitude: i.toDouble()), now: now);
      }

      expect(queue.length, LocationRepository.maxBatchSize);

      // The oldest went, not the newest: a recent position is the useful one.
      expect(queue.pending.last.latitude, LocationRepository.maxBatchSize + 24);
      expect(queue.pending.first.latitude, 25);
    });

    test('drops fixes too old to describe where anyone is now', () async {
      final store = _FakeStore();
      final stale = now.subtract(AppConfig.locationMaxQueueAge * 2);

      store.value = jsonEncode([
        _ping(stale, latitude: 1).toJson(),
        _ping(now, latitude: 2).toJson(),
      ]);

      final queue = LocationQueue(store);
      await queue.restore(now: now);

      expect(queue.length, 1);
      expect(queue.pending.single.latitude, 2);
    });
  });

  group('recovery', () {
    test('a payload from an incompatible build is discarded, not crashed on',
        () async {
      final store = _FakeStore()..value = '{"pings":[]}';

      final queue = LocationQueue(store);
      await queue.restore(now: now);

      expect(queue.isEmpty, isTrue);
      expect(store.clears, 1);
    });

    test('a payload truncated by a kill mid-write is discarded', () async {
      final store = _FakeStore()..value = '[{"latitude":44.8,"longi';

      final queue = LocationQueue(store);
      await queue.restore(now: now);

      expect(queue.isEmpty, isTrue);
    });

    test('an entry missing its timestamp is discarded', () async {
      final store = _FakeStore()
        ..value = jsonEncode([
          {'latitude': 44.8, 'longitude': 20.4},
        ]);

      final queue = LocationQueue(store);
      await queue.restore(now: now);

      expect(queue.isEmpty, isTrue);
    });
  });

  group('round trip', () {
    test('a stored ping comes back with the values it went in with', () {
      final original = _ping(now);
      final restored = LocationPing.fromJson(original.toJson());

      expect(restored.latitude, original.latitude);
      expect(restored.longitude, original.longitude);
      expect(restored.accuracy, original.accuracy);
      expect(restored.timestamp, original.timestamp);
    });

    test('a ping without accuracy survives the round trip', () {
      final restored = LocationPing.fromJson(
        LocationPing(latitude: 1, longitude: 2, timestamp: now).toJson(),
      );

      expect(restored.accuracy, isNull);
    });
  });
}
