import 'dart:convert';

import 'package:construction_mobile/core/outbox/outbox_queue.dart';
import 'package:flutter_test/flutter_test.dart';

class _MemoryStore implements OutboxStore {
  String? value;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async => value = written;

  @override
  Future<void> clear() async => value = null;
}

OutboxItem _item({
  String key = 'key-1',
  OutboxKind kind = OutboxKind.defect,
  DateTime? createdAt,
}) {
  return OutboxItem(
    kind: kind,
    idempotencyKey: key,
    createdAt: createdAt ?? DateTime.now().toUtc(),
    payload: <String, dynamic>{'title': 'Crack', 'latitude': 44.5},
  );
}

void main() {
  test('an item survives the app being killed, key and all', () async {
    final store = _MemoryStore();

    final before = OutboxQueue(store);
    await before.restore();
    await before.bindTo('ana');
    await before.add(_item(key: 'the-key'));

    final after = OutboxQueue(store);
    await after.restore();
    await after.bindTo('ana');

    expect(after.length, 1);
    expect(after.first!.idempotencyKey, 'the-key');
    expect(after.first!.payload['title'], 'Crack');
    expect(after.first!.payload['latitude'], 44.5);
  });

  test('what one person left waiting is not offered to the next', () async {
    final store = _MemoryStore();

    final queue = OutboxQueue(store);
    await queue.restore();
    await queue.bindTo('ana');
    await queue.add(_item(key: 'anas'));

    await queue.bindTo('marko');

    expect(queue.length, 0);
    expect(queue.first, isNull);

    // Still on disk for her.
    await queue.bindTo('ana');
    expect(queue.first!.idempotencyKey, 'anas');
  });

  test('acknowledging takes the oldest of this person\'s, in order', () async {
    final queue = OutboxQueue(_MemoryStore());
    await queue.restore();
    await queue.bindTo('ana');

    await queue.add(_item(key: 'first'));
    await queue.add(_item(key: 'second', kind: OutboxKind.absenceRequest));

    await queue.acknowledgeFirst();

    expect(queue.first!.idempotencyKey, 'second');
    expect(queue.first!.kind, OutboxKind.absenceRequest);

    await queue.acknowledgeFirst();

    expect(queue.length, 0);
  });

  test('anything older than a week is dropped', () async {
    final store = _MemoryStore();
    final queue = OutboxQueue(store);

    await queue.restore();
    await queue.bindTo('ana');
    await queue.add(
      _item(createdAt: DateTime.now().toUtc().subtract(const Duration(days: 8))),
    );

    expect(queue.length, 0);
    expect(store.value, isNull);
  });

  test('the queue is bounded per person, keeping the newest', () async {
    final queue = OutboxQueue(_MemoryStore());
    await queue.restore();

    await queue.bindTo('ana');
    await queue.add(_item(key: 'anas'));

    await queue.bindTo('marko');

    for (var i = 0; i < OutboxQueue.maxItems + 3; i++) {
      await queue.add(_item(key: 'm-$i'));
    }

    expect(queue.length, OutboxQueue.maxItems);
    expect(queue.first!.idempotencyKey, 'm-3');

    await queue.bindTo('ana');
    expect(queue.first!.idempotencyKey, 'anas', reason: 'his backlog is not hers');
  });

  test('an unreadable payload is dropped, not thrown', () async {
    for (final broken in ['{"not":"a list"}', '[{"kind":"defect","idem', '[1,2]']) {
      final store = _MemoryStore()..value = broken;
      final queue = OutboxQueue(store);

      await queue.restore();

      expect(queue.length, 0, reason: broken);
      expect(store.value, isNull, reason: broken);
    }
  });

  test('a kind this build does not know is dropped', () async {
    final store = _MemoryStore()
      ..value = jsonEncode([
        {
          'kind': 'expense',
          'idempotencyKey': 'k',
          'createdAt': DateTime.now().toUtc().toIso8601String(),
          'payload': <String, dynamic>{},
        }
      ]);

    final queue = OutboxQueue(store);
    await queue.restore();

    expect(queue.length, 0);
  });

  test('an emptied queue leaves nothing on disk', () async {
    final store = _MemoryStore();
    final queue = OutboxQueue(store);

    await queue.restore();
    await queue.bindTo('ana');
    await queue.add(_item());
    await queue.acknowledgeFirst();

    expect(store.value, isNull);
  });
}
