import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:flutter_test/flutter_test.dart';

/// A [BlobStore] in a map, so the cache's rules can be exercised without a
/// filesystem or a device.
class _MemoryStore implements BlobStore {
  final Map<String, String> entries = <String, String>{};

  int writes = 0;

  @override
  Future<String?> read(String key) async => entries[key];

  @override
  Future<void> write(String key, String value) async {
    writes++;
    entries[key] = value;
  }

  @override
  Future<void> delete(String key) async => entries.remove(key);

  @override
  Future<List<String>> keys() async => entries.keys.toList();

  @override
  Future<void> clear() async => entries.clear();
}

void main() {
  late _MemoryStore store;

  setUp(() => store = _MemoryStore());

  group('naming an entry', () {
    test('two spellings of the same request are one entry', () {
      final first = OfflineCache.keyFor('GET', '/api/v1/employees', {
        'pageNumber': 1,
        'pageSize': 20,
      });

      // Same request, and Dart makes no promise about map order — a key that
      // depended on it would miss the cache roughly half the time, which is
      // the sort of bug that reads as "the cache does not work sometimes".
      final second = OfflineCache.keyFor('get', '/api/v1/employees', {
        'pageSize': 20,
        'pageNumber': 1,
      });

      expect(first, second);
    });

    test('page two is not page one', () {
      expect(
        OfflineCache.keyFor('GET', '/api/v1/employees', {'pageNumber': 1}),
        isNot(OfflineCache.keyFor('GET', '/api/v1/employees', {'pageNumber': 2})),
      );
    });

    test('a different path is a different entry', () {
      expect(
        OfflineCache.keyFor('GET', '/api/v1/employees', const {}),
        isNot(OfflineCache.keyFor('GET', '/api/v1/projects', const {})),
      );
    });

    test('the search term does not appear in the name', () {
      // The name ends up as a file name in a directory somebody may one day
      // read off a device. What a foreman looked for is his business.
      final key = OfflineCache.keyFor('GET', '/api/v1/employees', {
        'search': 'Petrovic',
      });

      expect(key, isNot(contains('Petrovic')));
      expect(key, matches(RegExp(r'^[0-9a-f]{64}$')));
    });
  });

  group('what comes back', () {
    test('what went in', () async {
      final cache = OfflineCache(store);
      final saved = DateTime.utc(2026, 8, 9, 7, 14);

      await cache.write(
        'k',
        statusCode: 200,
        body: {'items': <dynamic>[], 'totalCount': 0},
        now: saved,
      );

      final entry = await cache.read('k', now: saved);

      expect(entry, isNotNull);
      expect(entry!.statusCode, 200);
      expect(entry.body, {'items': <dynamic>[], 'totalCount': 0});
      expect(entry.savedAt, saved);
    });

    test('nothing, for a request never made', () async {
      expect(await OfflineCache(store).read('never'), isNull);
    });

    test('nothing once it is past its age, and the entry is dropped',
        () async {
      final cache = OfflineCache(store, maxAge: const Duration(days: 7));
      final saved = DateTime.utc(2026, 8, 1);

      await cache.write('k', statusCode: 200, body: {'a': 1}, now: saved);

      // Six days: still the best answer available.
      expect(
        await cache.read('k', now: saved.add(const Duration(days: 6))),
        isNotNull,
      );

      // Eight: a roster this old shown without comment is worse than a blank
      // screen, and carrying it around costs space for nothing.
      expect(
        await cache.read('k', now: saved.add(const Duration(days: 8))),
        isNull,
      );
      expect(store.entries, isEmpty);
    });

    test('nothing, when the entry is half-written', () async {
      store.entries['k'] = '{"savedAt":"2026-08-09T07:1';

      expect(await OfflineCache(store).read('k'), isNull);
      expect(store.entries, isEmpty, reason: 'unusable entry should be dropped');
    });

    test('nothing, when the entry is JSON of the wrong shape', () async {
      // What a previous version of the app might have left behind.
      store.entries['k'] = jsonEncode({'stored': 'differently'});

      expect(await OfflineCache(store).read('k'), isNull);
      expect(store.entries, isEmpty);
    });
  });

  group('what is refused', () {
    test('a response too big to be worth keeping', () async {
      final cache = OfflineCache(store, maxEntryBytes: 200);

      final stored = await cache.write(
        'k',
        statusCode: 200,
        body: {'blob': 'x' * 500},
      );

      expect(stored, isFalse);
      expect(store.entries, isEmpty);
    });

    test('and it takes the previous, smaller copy with it', () async {
      final cache = OfflineCache(store, maxEntryBytes: 200);

      await cache.write('k', statusCode: 200, body: {'blob': 'small'});
      expect(store.entries, isNotEmpty);

      // Otherwise the screen that just grew past the limit would keep showing
      // the last version small enough to store — older than the one refused,
      // and with no sign that anything was skipped.
      await cache.write('k', statusCode: 200, body: {'blob': 'x' * 500});

      expect(store.entries, isEmpty);
    });
  });

  group('when there are too many', () {
    test('the newest are kept', () async {
      final cache = OfflineCache(store, maxEntries: 3);
      final start = DateTime.utc(2026, 8, 9, 6);

      for (var index = 0; index < 5; index++) {
        await cache.write(
          'key-$index',
          statusCode: 200,
          body: {'index': index},
          now: start.add(Duration(minutes: index)),
        );
      }

      expect(store.entries.keys, hasLength(3));
      expect(store.entries.keys, containsAll(['key-2', 'key-3', 'key-4']));
      expect(store.entries.keys, isNot(contains('key-0')));
    });

    test('by the age they carry, not the order they were written', () async {
      // A device whose clock jumped, or a queue that flushed out of order.
      // The entry knows when it was made; the store does not.
      final cache = OfflineCache(store, maxEntries: 2);

      // Written newest first, so "keep the last two written" and "keep the two
      // newest" disagree — otherwise this passes against an implementation
      // that never looks at `savedAt` at all.
      await cache.write('new', statusCode: 200, body: {}, now: DateTime.utc(2026, 8, 9));
      await cache.write('old', statusCode: 200, body: {}, now: DateTime.utc(2026, 8, 1));
      await cache.write('mid', statusCode: 200, body: {}, now: DateTime.utc(2026, 8, 5));

      expect(store.entries.keys, unorderedEquals(['new', 'mid']));
    });
  });

  test('clearing leaves nothing behind', () async {
    final cache = OfflineCache(store);

    await cache.write('a', statusCode: 200, body: {'x': 1});
    await cache.write('b', statusCode: 200, body: {'y': 2});

    await cache.clear();

    expect(store.entries, isEmpty);
  });

  group('opening it', () {
    test('gives a cache when the platform cooperates', () async {
      final cache = await openOfflineCache(
        open: () async => _MemoryStore(),
      );

      expect(cache, isNotNull);
    });

    test('gives up rather than hang when the platform never answers', () async {
      // Everything downstream waits on this: a request that wants the cache,
      // and signing out, which empties it. A platform call that never returns
      // would strand a worker on the splash screen to protect a cache the app
      // does not need to run.
      final cache = await openOfflineCache(
        open: () => Completer<BlobStore>().future,
        timeout: const Duration(milliseconds: 20),
      );

      expect(cache, isNull);
    });

    test('gives up when the platform refuses', () async {
      final cache = await openOfflineCache(
        open: () async => throw const FileSystemException('no such directory'),
      );

      expect(cache, isNull);
    });
  });
}
