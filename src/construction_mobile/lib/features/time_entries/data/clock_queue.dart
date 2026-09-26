import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/network/network_providers.dart';

/// Which end of a shift is waiting to be sent.
enum ClockAction { clockIn, clockOut }

/// A clock-in or clock-out recorded on the handset before it could be sent.
///
/// [occurredAt] is the reason this class exists. A worker starts at seven in a
/// basement and the request does not leave until half past nine; without the
/// moment travelling with it, the shift would be recorded as starting when the
/// signal came back, and the hour and a half in between would simply be gone
/// from somebody's week.
///
/// [idempotencyKey] is minted once, when the action is recorded, and reused on
/// every attempt to send it. A reply lost on the way back would otherwise have
/// the app send the same clock-in again, and two shifts opened at the same
/// moment is a correction somebody has to make by hand.
class PendingClockAction {
  const PendingClockAction({
    required this.action,
    required this.occurredAt,
    required this.idempotencyKey,
    this.breakMinutes = 0,
    this.latitude,
    this.longitude,
    this.projectId,
    this.workType = 'Regular',
    this.ownerId,
  });

  final ClockAction action;

  /// Who pressed the button. Site phones are handed around, and a clock-out
  /// left waiting when one person signs out must not be sent as the next
  /// person to sign in. Null only for an action written by a build that did
  /// not record it.
  final String? ownerId;

  PendingClockAction _ownedBy(String? owner) => PendingClockAction(
        action: action,
        occurredAt: occurredAt,
        idempotencyKey: idempotencyKey,
        breakMinutes: breakMinutes,
        latitude: latitude,
        longitude: longitude,
        projectId: projectId,
        workType: workType,
        ownerId: owner,
      );

  /// The site the worker picked, for a clock-in. Without it a worker posted to
  /// several sites who chose one with no signal would be placed by the server
  /// as "ambiguous", i.e. on no project at all.
  final String? projectId;

  /// Only meaningful for a clock-in.
  final String workType;

  /// When the handset says it happened, in UTC.
  final DateTime occurredAt;

  final String idempotencyKey;

  /// Only meaningful for a clock-out.
  final int breakMinutes;

  final double? latitude;
  final double? longitude;

  Map<String, dynamic> toJson() => <String, dynamic>{
        'action': action.name,
        'occurredAt': occurredAt.toIso8601String(),
        'idempotencyKey': idempotencyKey,
        'breakMinutes': breakMinutes,
        'workType': workType,
        'ownerId': ?ownerId,
        'projectId': ?projectId,
        'latitude': ?latitude,
        'longitude': ?longitude,
      };

  factory PendingClockAction.fromJson(Map<String, dynamic> json) {
    return PendingClockAction(
      action: ClockAction.values.firstWhere(
        (value) => value.name == json['action'],
        // A payload from a build that named these differently is not worth a
        // crash on the shift screen; the queue drops what it cannot read.
        orElse: () => throw const FormatException('Unknown clock action'),
      ),
      occurredAt: DateTime.parse(json['occurredAt'] as String).toUtc(),
      idempotencyKey: json['idempotencyKey'] as String,
      breakMinutes: (json['breakMinutes'] as num?)?.toInt() ?? 0,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      projectId: json['projectId'] as String?,
      // Absent in a queue written by an older build.
      workType: json['workType'] as String? ?? 'Regular',
      ownerId: json['ownerId'] as String?,
    );
  }
}

/// Clock actions captured with no signal, waiting for one.
///
/// Deliberately a different queue from the location one, though it is built
/// the same way and for the same reason — it has to survive the process being
/// reclaimed, because Android will do that during a shift. What is different
/// is the stakes and therefore the rules: a lost GPS fix is a gap in a trail,
/// a lost clock-in is somebody's pay.
///
/// Two bounds, both chosen to match what the API will accept rather than
/// invented here. Nothing older than [maxAge] is kept, because the server
/// refuses a self-stamped time more than a day old and holding one would only
/// produce a refusal later, further from where it could be understood. And at
/// most [maxActions] are held: more than that means an app that has been
/// offline for days, where what is needed is a supervisor, not a longer queue.
class ClockQueue {
  ClockQueue(this._store);

  /// Matches `TimeEntryRules.MaxOfflineDelay` on the API.
  static const Duration maxAge = Duration(hours: 24);

  /// A day's worth of starting and stopping, with room to spare.
  static const int maxActions = 6;

  final ClockQueueStore _store;

  /// Everyone's, as stored. What the rest of the app sees is [_mine].
  final List<PendingClockAction> _actions = <PendingClockAction>[];

  String? _owner;

  bool _restored = false;

  /// Who the queue is being read for. Everything below answers for this person
  /// only; what belongs to somebody else stays on disk until they sign in.
  ///
  /// An action recorded by a build that did not note its owner is given to
  /// whoever binds first: the likeliest owner is the person using the handset
  /// now, and dropping it would lose somebody's hours.
  Future<void> bindTo(String? owner) async {
    _owner = owner;

    if (owner == null) {
      return;
    }

    var changed = false;

    for (var i = 0; i < _actions.length; i++) {
      if (_actions[i].ownerId == null) {
        _actions[i] = _actions[i]._ownedBy(owner);
        changed = true;
      }
    }

    if (changed) {
      await _persist();
    }
  }

  Iterable<PendingClockAction> get _mine =>
      _actions.where((action) => action.ownerId == _owner);

  List<PendingClockAction> get pending =>
      List<PendingClockAction>.unmodifiable(_mine);

  bool get isEmpty => _mine.isEmpty;

  PendingClockAction? get first => _mine.isEmpty ? null : _mine.first;

  /// The last thing this handset recorded, which is what the shift card shows.
  PendingClockAction? get last => _mine.isEmpty ? null : _mine.last;

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
        throw const FormatException('Stored clock queue is not a list');
      }

      _actions.addAll(
        decoded.cast<Map<String, dynamic>>().map(PendingClockAction.fromJson),
      );
    } on FormatException {
      // Written by a build that shaped it differently, or truncated by a kill
      // mid-write. Unusable, and not worth a crash on the shift screen.
      _actions.clear();
      await _store.clear();
      return;
    } on TypeError {
      _actions.clear();
      await _store.clear();
      return;
    }

    await _prune(now: now);
  }

  Future<void> add(PendingClockAction action, {DateTime? now}) async {
    _actions.add(action._ownedBy(_owner));
    await _prune(now: now);
  }

  /// Drops the action at the front, once the API has taken it.
  Future<void> acknowledgeFirst() async {
    final next = first;

    if (next == null) {
      return;
    }

    _actions.remove(next);
    await _persist();
  }

  Future<void> clear() async {
    _actions.removeWhere((action) => action.ownerId == _owner);
    await _persist();
  }

  Future<void> _prune({DateTime? now}) async {
    final cutoff = (now ?? DateTime.now().toUtc()).subtract(maxAge);

    // Older than the API will accept. Keeping it would trade a refusal today
    // for the same refusal tomorrow, with nobody any wiser in between.
    _actions.removeWhere((action) => action.occurredAt.isBefore(cutoff));

    // Per person: one worker's backlog must not push out another's.
    final owners = _actions.map((action) => action.ownerId).toSet();

    for (final owner in owners) {
      final theirs = _actions.where((a) => a.ownerId == owner).toList();

      if (theirs.length > maxActions) {
        theirs
            .take(theirs.length - maxActions)
            .forEach(_actions.remove);
      }
    }

    await _persist();
  }

  Future<void> _persist() async {
    if (_actions.isEmpty) {
      await _store.clear();
      return;
    }

    await _store.write(
      jsonEncode(_actions.map((action) => action.toJson()).toList()),
    );
  }
}

/// Where the queue survives between app launches.
///
/// An interface for the same reason the location queue has one: the rules —
/// ageing out, capping, recovering from a corrupt payload — are worth testing
/// without a platform channel.
abstract interface class ClockQueueStore {
  Future<String?> read();

  Future<void> write(String value);

  Future<void> clear();
}

/// Stored in the encrypted store the app already opens. Not a secret, but it
/// is a record of somebody's working hours, which is not for plain
/// preferences either.
class SecureClockQueueStore implements ClockQueueStore {
  const SecureClockQueueStore(this._storage);

  static const _key = 'timeentries.queue';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() => _storage.read(key: _key);

  @override
  Future<void> write(String value) => _storage.write(key: _key, value: value);

  @override
  Future<void> clear() => _storage.delete(key: _key);
}

final clockQueueProvider = Provider<ClockQueue>((ref) {
  return ClockQueue(SecureClockQueueStore(ref.watch(secureStorageProvider)));
});
