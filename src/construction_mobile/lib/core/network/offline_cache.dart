import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:flutter/foundation.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

/// A response the app kept in case the next one never arrives.
///
/// [savedAt] is not decoration. Everything this cache serves is out of date by
/// construction, and the only honest thing to put in front of a foreman is
/// *how* out of date — "the crew list as it was at 07:14" is usable, "the crew
/// list" is a lie the moment somebody is reassigned.
@immutable
class CachedResponse {
  const CachedResponse({
    required this.savedAt,
    required this.statusCode,
    required this.body,
  });

  final DateTime savedAt;
  final int statusCode;

  /// The decoded JSON body, exactly as Dio handed it over.
  final Object? body;

  Duration ageAt(DateTime now) => now.difference(savedAt);

  Map<String, dynamic> toJson() => <String, dynamic>{
        'savedAt': savedAt.toIso8601String(),
        'statusCode': statusCode,
        'body': body,
      };

  factory CachedResponse.fromJson(Map<String, dynamic> json) {
    return CachedResponse(
      savedAt: DateTime.parse(json['savedAt'] as String).toUtc(),
      statusCode: json['statusCode'] as int,
      body: json['body'],
    );
  }
}

/// Somewhere to put a blob under a name.
///
/// An interface, and not because a second implementation is planned: the
/// cache's rules — what may be stored, how long it stays worth serving, what
/// gets evicted first — are worth testing, and none of them should need a
/// device to run. [FileBlobStore] is the only production implementation.
abstract interface class BlobStore {
  Future<String?> read(String key);

  Future<void> write(String key, String value);

  Future<void> delete(String key);

  /// Every key currently held, in no particular order.
  Future<List<String>> keys();

  Future<void> clear();
}

/// Files in the app's private support directory.
///
/// Not the documents directory: on iOS that is offered to the Files app and
/// synced to iCloud, and this holds names, wages and positions of real people.
/// Support is app-private on both platforms and is excluded from device
/// backups on Android by `android:allowBackup="false"`.
class FileBlobStore implements BlobStore {
  FileBlobStore(this._directory);

  /// Opens (creating if needed) the cache directory under application support.
  static Future<FileBlobStore> open() async {
    final support = await getApplicationSupportDirectory();
    final directory = Directory(p.join(support.path, 'api_cache'));

    await directory.create(recursive: true);
    return FileBlobStore(directory);
  }

  final Directory _directory;

  File _fileFor(String key) => File(p.join(_directory.path, key));

  @override
  Future<String?> read(String key) async {
    final file = _fileFor(key);

    if (!await file.exists()) {
      return null;
    }

    try {
      return await file.readAsString();
    } on FileSystemException {
      // Killed mid-write, or the directory was cleared underneath us. An
      // unreadable cache entry is a cache miss, never a crash.
      return null;
    }
  }

  @override
  Future<void> write(String key, String value) async {
    // Through a temporary file: a process killed halfway through a write must
    // leave the previous entry intact rather than a truncated one, because a
    // truncated entry is indistinguishable from a short list.
    final target = _fileFor(key);
    final temporary = File('${target.path}.tmp');

    try {
      await temporary.writeAsString(value, flush: true);
      await temporary.rename(target.path);
    } on FileSystemException {
      // Out of space, or the directory has gone. Failing to cache must never
      // fail the request that was being cached.
      await _deleteQuietly(temporary);
    }
  }

  @override
  Future<void> delete(String key) => _deleteQuietly(_fileFor(key));

  @override
  Future<List<String>> keys() async {
    if (!await _directory.exists()) {
      return const <String>[];
    }

    try {
      return await _directory
          .list()
          .where((entity) => entity is File && !entity.path.endsWith('.tmp'))
          .map((entity) => p.basename(entity.path))
          .toList();
    } on FileSystemException {
      return const <String>[];
    }
  }

  @override
  Future<void> clear() async {
    if (!await _directory.exists()) {
      return;
    }

    try {
      await _directory.delete(recursive: true);
      await _directory.create(recursive: true);
    } on FileSystemException {
      // Nothing useful to do, and nothing that should stop a sign-out.
    }
  }

  static Future<void> _deleteQuietly(File file) async {
    try {
      if (await file.exists()) {
        await file.delete();
      }
    } on FileSystemException {
      // Already gone, or never there.
    }
  }
}

/// The last good answer to each read the app has made.
///
/// Sites have holes in their coverage — inside a lift shaft, behind a
/// retaining wall, in half the villages the crew drives through. Before this,
/// every screen in the app answered that with the same red panel, which is a
/// strange thing to show somebody who only wants to know which four people are
/// on his crew today, an answer that has not changed since he looked at it in
/// the yard.
///
/// Three rules keep it from becoming a liability:
///
/// * It is **read-through only**. Nothing is served from here while the
///   network works, so a live answer always wins and the cache can never make
///   the app show older data than it would have anyway.
/// * It **expires**. Past [maxAge] an entry is deleted rather than served —
///   month-old staffing shown without comment is worse than no answer.
/// * It is **emptied when the session changes**. Site phones are shared, and
///   the cache would otherwise let whoever picks one up next read the previous
///   user's data by turning on aeroplane mode.
class OfflineCache {
  OfflineCache(
    this._store, {
    this.maxAge = const Duration(days: 7),
    this.maxEntries = 200,
    this.maxEntryBytes = 512 * 1024,
  });

  final BlobStore _store;

  /// How long an entry stays worth serving.
  final Duration maxAge;

  /// How many entries are kept before the least recently written are dropped.
  final int maxEntries;

  /// Responses larger than this are not cached at all. A single oversized
  /// answer — an unpaged export, a base64 photograph — would otherwise push
  /// out every small one that a screen actually needs.
  final int maxEntryBytes;

  /// Names an entry after the request that produced it.
  ///
  /// Hashed rather than encoded: the query string carries search terms typed
  /// by the user, and a directory listing of file names should not read as a
  /// log of what somebody looked for. It also keeps the name inside every
  /// filesystem's length limit whatever the query contains.
  static String keyFor(String method, String path, Map<String, dynamic> query) {
    final ordered = query.keys.toList()..sort();
    final canonical = StringBuffer('${method.toUpperCase()} $path');

    for (final name in ordered) {
      canonical.write('&$name=${query[name]}');
    }

    return sha256.convert(utf8.encode(canonical.toString())).toString();
  }

  /// The stored answer for [key], or null when there is none worth serving.
  ///
  /// An entry past [maxAge] is deleted here rather than merely skipped, so a
  /// phone that spends a fortnight offline does not carry the dead weight of
  /// every screen its owner opened before the outage.
  Future<CachedResponse?> read(String key, {DateTime? now}) async {
    final raw = await _store.read(key);

    if (raw == null || raw.isEmpty) {
      return null;
    }

    final CachedResponse entry;

    try {
      final decoded = jsonDecode(raw);

      if (decoded is! Map<String, dynamic>) {
        throw const FormatException('Cache entry is not an object');
      }

      entry = CachedResponse.fromJson(decoded);
    } on FormatException {
      // Written by a build that shaped it differently, or truncated.
      await _store.delete(key);
      return null;
    } on TypeError {
      await _store.delete(key);
      return null;
    }

    if (entry.ageAt(now ?? DateTime.now().toUtc()) > maxAge) {
      await _store.delete(key);
      return null;
    }

    return entry;
  }

  /// Stores [body] as the answer to [key], unless it is too big to be worth it.
  ///
  /// Returns whether it was stored, which is what the tests assert against —
  /// the caller has nothing to do differently either way.
  Future<bool> write(
    String key, {
    required int statusCode,
    required Object? body,
    DateTime? now,
  }) async {
    final String encoded;

    try {
      encoded = jsonEncode(
        CachedResponse(
          savedAt: now ?? DateTime.now().toUtc(),
          statusCode: statusCode,
          body: body,
        ).toJson(),
      );
    } on JsonUnsupportedObjectError {
      // A response Dio did not decode as JSON — a file download, an empty
      // body typed as a stream. Not ours to keep.
      return false;
    }

    if (encoded.length > maxEntryBytes) {
      // And drop any previous, smaller copy: keeping it would mean serving a
      // version of this screen older than the one just refused.
      await _store.delete(key);
      return false;
    }

    await _store.write(key, encoded);
    await _evict();
    return true;
  }

  /// Empties the cache. Called on every session change.
  Future<void> clear() => _store.clear();

  /// Keeps the newest [maxEntries] and deletes the rest.
  ///
  /// Newest by `savedAt` inside the entry rather than by file timestamp: the
  /// two agree on a device, but a restored backup or a clock change makes file
  /// times lie, and the entry knows its own age.
  Future<void> _evict() async {
    final keys = await _store.keys();

    if (keys.length <= maxEntries) {
      return;
    }

    final dated = <MapEntry<String, DateTime>>[];

    for (final key in keys) {
      final raw = await _store.read(key);
      DateTime savedAt;

      try {
        final decoded = jsonDecode(raw ?? '') as Map<String, dynamic>;
        savedAt = DateTime.parse(decoded['savedAt'] as String);
      } catch (_) {
        // Unreadable: first in the queue to go.
        savedAt = DateTime.fromMillisecondsSinceEpoch(0);
      }

      dated.add(MapEntry<String, DateTime>(key, savedAt));
    }

    dated.sort((a, b) => b.value.compareTo(a.value));

    for (final entry in dated.skip(maxEntries)) {
      await _store.delete(entry.key);
    }
  }
}
