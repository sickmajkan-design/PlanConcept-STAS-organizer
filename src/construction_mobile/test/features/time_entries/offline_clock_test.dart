import 'dart:convert';

import 'package:construction_mobile/features/time_entries/data/clock_queue.dart';
import 'package:flutter_test/flutter_test.dart';

/// The queue a shift waits in when there is no signal to send it.
///
/// The rules here are not the location queue's, though the shape is. A lost
/// GPS fix is a gap in a trail; a lost clock-in is somebody's pay. What that
/// changes is the bounds: both of these match what the API will accept, so the
/// handset never holds something it will only be refused for later.
class _MemoryStore implements ClockQueueStore {
  String? value;

  int writes = 0;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async {
    writes++;
    value = written;
  }

  @override
  Future<void> clear() async => value = null;
}

PendingClockAction _action({
  ClockAction action = ClockAction.clockIn,
  DateTime? occurredAt,
  String key = 'key-1',
  int breakMinutes = 0,
}) {
  return PendingClockAction(
    action: action,
    occurredAt: occurredAt ?? DateTime.now().toUtc(),
    idempotencyKey: key,
    breakMinutes: breakMinutes,
  );
}

void main() {
  test('an action survives the app being killed', () async {
    // Android reclaims the process during a shift; that is the ordinary case,
    // not a rare one, and it is why this queue is on disk at all.
    final store = _MemoryStore();
    final morning = DateTime.now().toUtc().subtract(const Duration(hours: 2));

    final before = ClockQueue(store);
    await before.restore();
    await before.add(_action(occurredAt: morning, key: 'the-key'));

    final after = ClockQueue(store);
    await after.restore();

    expect(after.pending, hasLength(1));
    expect(after.first!.action, ClockAction.clockIn);
    expect(after.first!.occurredAt, morning);
    expect(
      after.first!.idempotencyKey,
      'the-key',
      reason: 'the key has to be the same one, or the replay opens a second shift',
    );
  });

  test('a clock-out carries its break minutes across a restart', () async {
    final store = _MemoryStore();

    final before = ClockQueue(store);
    await before.restore();
    await before.add(
      _action(action: ClockAction.clockOut, breakMinutes: 45),
    );

    final after = ClockQueue(store);
    await after.restore();

    expect(after.first!.breakMinutes, 45);
  });

  test('anything older than the API will accept is dropped', () async {
    final store = _MemoryStore();
    final queue = ClockQueue(store);

    await queue.restore();
    await queue.add(
      _action(
        occurredAt: DateTime.now().toUtc().subtract(const Duration(hours: 30)),
      ),
    );

    // Holding it would trade a refusal today for the same refusal tomorrow,
    // with nobody any wiser in between.
    expect(queue.pending, isEmpty);
    expect(store.value, isNull);
  });

  test('the queue is bounded, keeping the newest', () async {
    final queue = ClockQueue(_MemoryStore());
    await queue.restore();

    for (var i = 0; i < ClockQueue.maxActions + 3; i++) {
      await queue.add(_action(key: 'key-$i'));
    }

    expect(queue.pending, hasLength(ClockQueue.maxActions));
    expect(queue.first!.idempotencyKey, 'key-3');
    expect(queue.last!.idempotencyKey, 'key-${ClockQueue.maxActions + 2}');
  });

  test('acknowledging takes them off the front, in order', () async {
    final queue = ClockQueue(_MemoryStore());
    await queue.restore();

    await queue.add(_action(key: 'first'));
    await queue.add(_action(action: ClockAction.clockOut, key: 'second'));

    await queue.acknowledgeFirst();

    expect(queue.pending, hasLength(1));
    expect(queue.first!.idempotencyKey, 'second');

    await queue.acknowledgeFirst();

    expect(queue.isEmpty, isTrue);
  });

  test('a payload from an incompatible build is dropped, not thrown', () async {
    final store = _MemoryStore()..value = '{"not":"a list"}';
    final queue = ClockQueue(store);

    await queue.restore();

    expect(queue.isEmpty, isTrue);
    expect(store.value, isNull);
  });

  test('a truncated payload is dropped, not thrown', () async {
    // What a process killed mid-write leaves behind.
    final store = _MemoryStore()..value = '[{"action":"clockIn","occ';
    final queue = ClockQueue(store);

    await queue.restore();

    expect(queue.isEmpty, isTrue);
  });

  test('an action naming a kind this build does not know is dropped',
      () async {
    final store = _MemoryStore()
      ..value = jsonEncode([
        {
          'action': 'startBreak',
          'occurredAt': DateTime.now().toUtc().toIso8601String(),
          'idempotencyKey': 'key',
          'breakMinutes': 0,
        }
      ]);

    final queue = ClockQueue(store);
    await queue.restore();

    expect(queue.isEmpty, isTrue);
  });

  test('an emptied queue leaves nothing on disk', () async {
    final store = _MemoryStore();
    final queue = ClockQueue(store);

    await queue.restore();
    await queue.add(_action());
    await queue.acknowledgeFirst();

    // Not an empty list sitting in the keystore: this is a record of somebody's
    // hours, and it goes when it is no longer needed.
    expect(store.value, isNull);
  });
}
