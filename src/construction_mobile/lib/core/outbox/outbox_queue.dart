import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../network/network_providers.dart';

/// What can be made with no signal and sent later.
///
/// Deliberately short. A clock-in is handled by its own queue because time is
/// the point of it. These two are the things a person files from the field and
/// which are just as true an hour later: a defect they are standing in front
/// of, and a request for leave. Everything else — approvals, stock, costs —
/// depends on the state of the server at the moment and would need conflict
/// rules, so it still asks for a connection.
enum OutboxKind { defect, absenceRequest }

/// A report or request made on the handset, waiting for a connection.
class OutboxItem {
  const OutboxItem({
    required this.kind,
    required this.idempotencyKey,
    required this.createdAt,
    required this.payload,
    this.ownerId,
  });

  final OutboxKind kind;

  /// Minted once when it is recorded and reused on every attempt, so a reply
  /// lost on the way back cannot file the same defect twice.
  final String idempotencyKey;

  final DateTime createdAt;

  /// What the repository call needs, as plain JSON.
  final Map<String, dynamic> payload;

  /// Whose it is; see the note on the clock queue's owner.
  final String? ownerId;

  OutboxItem ownedBy(String? owner) => OutboxItem(
        kind: kind,
        idempotencyKey: idempotencyKey,
        createdAt: createdAt,
        payload: payload,
        ownerId: owner,
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
        'kind': kind.name,
        'idempotencyKey': idempotencyKey,
        'createdAt': createdAt.toUtc().toIso8601String(),
        'payload': payload,
        'ownerId': ?ownerId,
      };

  factory OutboxItem.fromJson(Map<String, dynamic> json) {
    return OutboxItem(
      kind: OutboxKind.values.firstWhere(
        (value) => value.name == json['kind'],
        orElse: () => throw const FormatException('Unknown outbox kind'),
      ),
      idempotencyKey: json['idempotencyKey'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String).toUtc(),
      payload: Map<String, dynamic>.from(json['payload'] as Map),
      ownerId: json['ownerId'] as String?,
    );
  }
}

/// Reports and requests made with no signal, on disk until they can go.
///
/// On disk because Android reclaims the process, and a foreman who reported a
/// crack in the basement and then locked the phone has not agreed to lose it.
class OutboxQueue {
  OutboxQueue(this._store);

  /// A week: long enough to ride out a job site with no coverage, short enough
  /// that a report nobody has heard about does not turn up months later.
  static const Duration maxAge = Duration(days: 7);

  static const int maxItems = 20;

  final OutboxStore _store;
  final List<OutboxItem> _items = <OutboxItem>[];

  String? _owner;
  bool _restored = false;

  Iterable<OutboxItem> get _mine =>
      _items.where((item) => item.ownerId == _owner);

  List<OutboxItem> get pending => List<OutboxItem>.unmodifiable(_mine);

  int get length => _mine.length;

  OutboxItem? get first => _mine.isEmpty ? null : _mine.first;

  /// Everything below answers for one person only. Anything belonging to
  /// somebody else stays on disk for when they sign in, and is never sent
  /// with this person's token.
  Future<void> bindTo(String? owner) async {
    _owner = owner;
  }

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
        throw const FormatException('Stored outbox is not a list');
      }

      _items.addAll(
        decoded.cast<Map<String, dynamic>>().map(OutboxItem.fromJson),
      );
    } on FormatException {
      _items.clear();
      await _store.clear();
      return;
    } on TypeError {
      _items.clear();
      await _store.clear();
      return;
    }

    await _prune(now: now);
  }

  Future<void> add(OutboxItem item, {DateTime? now}) async {
    _items.add(item.ownedBy(_owner));
    await _prune(now: now);
  }

  /// Takes the oldest of this person's off the queue, once it has been sent or
  /// refused for good.
  Future<void> acknowledgeFirst() async {
    final next = first;

    if (next == null) {
      return;
    }

    _items.remove(next);
    await _persist();
  }

  Future<void> _prune({DateTime? now}) async {
    final cutoff = (now ?? DateTime.now().toUtc()).subtract(maxAge);

    _items.removeWhere((item) => item.createdAt.isBefore(cutoff));

    final owners = _items.map((item) => item.ownerId).toSet();

    for (final owner in owners) {
      final theirs = _items.where((item) => item.ownerId == owner).toList();

      if (theirs.length > maxItems) {
        theirs.take(theirs.length - maxItems).forEach(_items.remove);
      }
    }

    await _persist();
  }

  Future<void> _persist() async {
    if (_items.isEmpty) {
      await _store.clear();
      return;
    }

    await _store.write(jsonEncode(_items.map((item) => item.toJson()).toList()));
  }
}

abstract interface class OutboxStore {
  Future<String?> read();

  Future<void> write(String value);

  Future<void> clear();
}

/// In the encrypted store the app already opens: a defect report carries a
/// position and a leave request carries a reason that may be a health matter.
class SecureOutboxStore implements OutboxStore {
  const SecureOutboxStore(this._storage);

  static const _key = 'outbox.queue';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() => _storage.read(key: _key);

  @override
  Future<void> write(String value) => _storage.write(key: _key, value: value);

  @override
  Future<void> clear() => _storage.delete(key: _key);
}

final outboxQueueProvider = Provider<OutboxQueue>((ref) {
  return OutboxQueue(SecureOutboxStore(ref.watch(secureStorageProvider)));
});
