import 'dart:io';

import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:path/path.dart' as p;

/// The store as it actually runs: real files, real renames.
///
/// [FileBlobStore.open] needs a platform channel to find the support
/// directory, but everything it does afterwards is `dart:io` and can be
/// checked against a temporary directory — which is where the interesting
/// parts are, because a cache is mostly a set of promises about what survives.
void main() {
  late Directory directory;
  late FileBlobStore store;

  setUp(() {
    directory = Directory.systemTemp.createTempSync('api_cache_test');
    store = FileBlobStore(directory);
  });

  tearDown(() {
    if (directory.existsSync()) {
      directory.deleteSync(recursive: true);
    }
  });

  test('what was written is what is read', () async {
    await store.write('a', '{"hello":"world"}');

    expect(await store.read('a'), '{"hello":"world"}');
  });

  test('a key never written reads as nothing, not as an error', () async {
    expect(await store.read('missing'), isNull);
  });

  test('a second write replaces the first', () async {
    await store.write('a', 'first');
    await store.write('a', 'second');

    expect(await store.read('a'), 'second');
    expect(await store.keys(), ['a']);
  });

  test('the half-written file is never the one that is read', () async {
    // The write goes to a temporary name and is renamed into place, so a
    // process killed mid-write leaves the previous entry rather than a
    // truncated one — and a truncated JSON list is indistinguishable from a
    // short crew.
    await store.write('a', 'complete');

    final temporary = File(p.join(directory.path, 'a.tmp'));
    temporary.writeAsStringSync('half of som');

    expect(await store.read('a'), 'complete');
    expect(await store.keys(), ['a'], reason: 'the .tmp file is not an entry');
  });

  test('deleting one leaves the others', () async {
    await store.write('a', '1');
    await store.write('b', '2');

    await store.delete('a');

    expect(await store.read('a'), isNull);
    expect(await store.read('b'), '2');
  });

  test('deleting something that was never there is not an error', () async {
    await store.delete('never');
  });

  test('clearing empties it and leaves it usable', () async {
    await store.write('a', '1');
    await store.write('b', '2');

    await store.clear();

    expect(await store.keys(), isEmpty);

    // Sign-out clears; the next person signs in and the app keeps caching.
    // A clear that removed the directory without putting it back would break
    // every write after the first sign-out.
    await store.write('c', '3');
    expect(await store.read('c'), '3');
  });

  test('a directory that has gone reads as empty rather than throwing',
      () async {
    await store.write('a', '1');
    directory.deleteSync(recursive: true);

    expect(await store.keys(), isEmpty);
    expect(await store.read('a'), isNull);
  });
}
