import 'dart:async';
import 'dart:io';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_exception.dart';
import '../../core/network/idempotency.dart';
import '../../core/outbox/outbox_queue.dart';
import '../absences/data/absence_repository.dart';
import '../absences/presentation/my_absences_controller.dart';
import '../attachments/data/attachment_repository.dart';
import '../auth/presentation/auth_controller.dart';
import '../work_items/data/work_item_repository.dart';
import '../work_items/presentation/my_work_controller.dart';

/// Something the server would not take, once the phone finally got it there.
///
/// It was made with no signal, so the person is no longer looking at the
/// screen that would have shown them the reason. It is surfaced once instead.
class OutboxRefusal {
  const OutboxRefusal(this.kind, this.failure);

  final OutboxKind kind;
  final ApiException failure;
}

class OutboxState {
  const OutboxState({this.pendingCount = 0, this.refusal});

  final int pendingCount;
  final OutboxRefusal? refusal;
}

/// How a submission ended.
class SubmitResult {
  const SubmitResult({required this.queued, this.photoFailed = false});

  /// Not sent yet: recorded on the phone, to go when there is signal.
  final bool queued;

  /// The report went but its photograph did not.
  final bool photoFailed;
}

/// Sends defect reports and leave requests, now if it can and later if not.
///
/// The rule it turns on is the same as for clocking in: only "no signal" is
/// worth waiting out. A refusal — validation, permission, a clash with existing
/// leave — is shown to the person straight away when they are looking at the
/// screen, and surfaced once through [OutboxState.refusal] when it arrives
/// after the fact; queueing it would only repeat the same refusal later.
class OutboxController extends Notifier<OutboxState> {
  static const retryInterval = Duration(minutes: 1);

  Timer? _retryTimer;
  Future<void> _turn = Future<void>.value();
  Future<void>? _ready;

  @override
  OutboxState build() {
    final user = ref.watch(currentUserProvider);

    ref.onDispose(() => _retryTimer?.cancel());

    if (user == null) {
      return const OutboxState();
    }

    _ready = _load(user.id);

    return const OutboxState();
  }

  Future<void> _load(String owner) async {
    final queue = ref.read(outboxQueueProvider);

    await queue.restore();
    await queue.bindTo(owner);

    state = OutboxState(pendingCount: queue.length);

    if (queue.length > 0) {
      await flush();
    }
  }

  /// Sends what is waiting, oldest first, stopping at the first thing that
  /// cannot go for want of signal.
  Future<void> flush() => _exclusive(() async {
        final queue = ref.read(outboxQueueProvider);
        OutboxRefusal? refusal;

        while (queue.first != null) {
          final item = queue.first!;

          try {
            await _send(item);
            await queue.acknowledgeFirst();
            _refreshScreens(item.kind);
          } on ApiException catch (exception) {
            if (_isNoSignal(exception)) {
              _retryLater();
              break;
            }

            // Will be refused every time; keeping it would be an outbox that
            // never empties.
            await queue.acknowledgeFirst();
            refusal = OutboxRefusal(item.kind, exception);
          }
        }

        state = OutboxState(
          pendingCount: queue.length,
          refusal: refusal ?? state.refusal,
        );
      });

  /// The refusal has been shown; take it off so it is not shown again.
  void refusalShown() {
    state = OutboxState(pendingCount: state.pendingCount);
  }

  /// Sends [kind] now, or queues it if there is no signal. Throws what the
  /// server refused, exactly as calling the repository directly would.
  Future<SubmitResult> submit(
    OutboxKind kind,
    Map<String, dynamic> payload,
  ) async {
    await _ready;

    return _exclusive(() async {
      final item = OutboxItem(
        kind: kind,
        idempotencyKey: newIdempotencyKey(),
        createdAt: DateTime.now().toUtc(),
        payload: payload,
      );

      try {
        final photoFailed = await _send(item);
        _refreshScreens(kind);

        return SubmitResult(queued: false, photoFailed: photoFailed);
      } on ApiException catch (exception) {
        if (!_isNoSignal(exception)) {
          rethrow;
        }

        final queue = ref.read(outboxQueueProvider);

        await queue.add(item);
        state = OutboxState(pendingCount: queue.length, refusal: state.refusal);
        _retryLater();

        return const SubmitResult(queued: true);
      }
    });
  }

  bool _isNoSignal(ApiException exception) =>
      exception.kind == ApiFailureKind.offline ||
      exception.kind == ApiFailureKind.timeout;

  /// Returns whether the photograph, if there was one, failed to go.
  Future<bool> _send(OutboxItem item) async {
    final p = item.payload;

    switch (item.kind) {
      case OutboxKind.defect:
        final defect = await ref.read(workItemRepositoryProvider).reportDefect(
              projectId: p['projectId'] as String,
              title: p['title'] as String,
              description: p['description'] as String?,
              latitude: (p['latitude'] as num?)?.toDouble(),
              longitude: (p['longitude'] as num?)?.toDouble(),
              idempotencyKey: item.idempotencyKey,
            );

        final photoPath = p['photoPath'] as String?;

        if (photoPath == null) {
          return false;
        }

        // The report exists; a missing or unsent picture does not undo it. A
        // file that has since been cleared from the phone's cache is the same
        // case as one that failed to upload.
        try {
          if (!File(photoPath).existsSync()) {
            return true;
          }

          await ref.read(attachmentRepositoryProvider).uploadPhoto(
                ownerType: 'WorkItem',
                ownerId: defect.id,
                filePath: photoPath,
                fileName: p['photoName'] as String? ?? 'photo.jpg',
              );

          _forget(photoPath);

          return false;
        } on ApiException {
          return true;
        }

      case OutboxKind.absenceRequest:
        await ref.read(absenceRepositoryProvider).request(
              type: p['type'] as String,
              startDate: DateTime.parse(p['startDate'] as String),
              endDate: DateTime.parse(p['endDate'] as String),
              reason: p['reason'] as String?,
              idempotencyKey: item.idempotencyKey,
            );

        return false;
    }
  }

  /// Removes a photograph this app copied for the outbox, once it is on the
  /// server. Only ours: anything else is the camera's or the gallery's.
  void _forget(String path) {
    if (!path.contains('outbox-photos')) {
      return;
    }

    try {
      File(path).deleteSync();
    } catch (_) {
      // A leftover file is not worth failing a report that has gone.
    }
  }

  void _refreshScreens(OutboxKind kind) {
    switch (kind) {
      case OutboxKind.defect:
        ref.invalidate(myWorkControllerProvider);
      case OutboxKind.absenceRequest:
        ref.invalidate(myAbsencesControllerProvider);
    }
  }

  void _retryLater() {
    _retryTimer?.cancel();
    _retryTimer = Timer(retryInterval, () => unawaited(flush()));
  }

  /// One send at a time: a flush from the timer and a fresh submission must
  /// not both try the same item, or the second one is an avoidable replay.
  Future<T> _exclusive<T>(Future<T> Function() action) {
    final next = _turn.then((_) => action());

    _turn = next.then((_) {}, onError: (_) {});

    return next;
  }
}

final outboxControllerProvider =
    NotifierProvider<OutboxController, OutboxState>(OutboxController.new);

/// How many are waiting, for anything that only cares about that.
final outboxPendingProvider = Provider<int>(
  (ref) => ref.watch(outboxControllerProvider.select((s) => s.pendingCount)),
);
