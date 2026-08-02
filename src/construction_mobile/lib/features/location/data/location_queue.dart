import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/config/app_config.dart';
import '../../../core/network/network_providers.dart';
import 'location_repository.dart';

/// Where the queue survives between app launches.
///
/// An interface rather than a direct [FlutterSecureStorage] call so the queue's
/// rules — capping, ageing out, recovering from a corrupt payload — can be
/// tested without a platform channel.
abstract interface class LocationQueueStore {
  Future<String?> read();

  Future<void> write(String value);

  Future<void> clear();
}

/// Fixes captured but not yet accepted by the API.
///
/// Buffering used to live in the controller as a plain list, which was fine
/// while tracking only ran with the app on screen: the app being killed and
/// tracking stopping were the same event. Once reporting continues in the
/// background they come apart — Android reclaims the process during a shift,
/// and everything captured through a coverage gap went with it. That is the
/// exact stretch of the day the office has no other record of, so the queue
/// now outlives the process.
///
/// Two rules bound it. It keeps at most one API batch, because the API rejects
/// larger ones and older fixes are the ones worth dropping. And it drops fixes
/// past [AppConfig.locationMaxQueueAge], because replaying yesterday's trail
/// when a phone finally reconnects would put a worker in places they no longer
/// are.
class LocationQueue {
  LocationQueue(this._store);

  final LocationQueueStore _store;
  final List<LocationPing> _pings = <LocationPing>[];

  bool _restored = false;

  int get length => _pings.length;

  bool get isEmpty => _pings.isEmpty;

  /// A snapshot safe to hand to the repository while capture continues.
  List<LocationPing> get pending => List<LocationPing>.unmodifiable(_pings);

  /// Loads what the previous run left behind. Safe to call more than once.
  Future<void> restore({DateTime? now}) async {
    if (_restored) {
      return;
    }

    _restored = true;

    final raw = await _store.read();

    if (raw == null || raw.isEmpty) {
      return;
    }

    try {
      final decoded = jsonDecode(raw);

      if (decoded is! List) {
        throw const FormatException('Stored location queue is not a list');
      }

      _pings.addAll(
        decoded
            .cast<Map<String, dynamic>>()
            .map(LocationPing.fromJson),
      );
    } on FormatException {
      // Written by a build that shaped it differently, or truncated by a
      // kill mid-write. Either way it is unusable and not worth a crash.
      _pings.clear();
      await _store.clear();
      return;
    } on TypeError {
      _pings.clear();
      await _store.clear();
      return;
    }

    await _prune(now: now);
  }

  Future<void> add(LocationPing ping, {DateTime? now}) async {
    _pings.add(ping);
    await _prune(now: now);
  }

  /// Drops the fixes the API confirmed, keeping anything captured since.
  ///
  /// Takes the count rather than the list because capture keeps appending
  /// while a batch is in flight; removing by identity would race with it.
  Future<void> acknowledge(int count) async {
    if (count <= 0) {
      return;
    }

    _pings.removeRange(0, count.clamp(0, _pings.length));
    await _persist();
  }

  Future<void> clear() async {
    _pings.clear();
    await _store.clear();
  }

  Future<void> _prune({DateTime? now}) async {
    final cutoff =
        (now ?? DateTime.now().toUtc()).subtract(AppConfig.locationMaxQueueAge);

    _pings.removeWhere((ping) => ping.timestamp.isBefore(cutoff));

    if (_pings.length > LocationRepository.maxBatchSize) {
      _pings.removeRange(0, _pings.length - LocationRepository.maxBatchSize);
    }

    await _persist();
  }

  Future<void> _persist() async {
    if (_pings.isEmpty) {
      await _store.clear();
      return;
    }

    await _store.write(
      jsonEncode(_pings.map((ping) => ping.toJson()).toList()),
    );
  }
}

/// Stores the queue beside the session, in the same encrypted store the app
/// already opens. Positions are personal data, so plain preferences would be
/// the wrong home for them even though they are not credentials.
class SecureLocationQueueStore implements LocationQueueStore {
  const SecureLocationQueueStore(this._storage);

  static const _key = 'location.queue';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() => _storage.read(key: _key);

  @override
  Future<void> write(String value) => _storage.write(key: _key, value: value);

  @override
  Future<void> clear() => _storage.delete(key: _key);
}

final locationQueueProvider = Provider<LocationQueue>((ref) {
  return LocationQueue(SecureLocationQueueStore(ref.watch(secureStorageProvider)));
});
