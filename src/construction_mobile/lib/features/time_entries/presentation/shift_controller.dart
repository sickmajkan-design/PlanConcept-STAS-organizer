import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/network/api_exception.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../../core/network/idempotency.dart';
import '../data/clock_queue.dart';
import '../data/models/time_entry.dart';
import '../data/time_entry_repository.dart';

/// What the shift card shows.
///
/// A record rather than leaning on `AsyncValue` for the failure case: a
/// refused clock-out has to leave the running shift on screen so the worker
/// can try again, and an `AsyncError` would blank it. The error rides
/// alongside the shift instead of replacing it.
class ShiftState {
  const ShiftState({
    this.shift,
    this.queued,
    this.failure,
    this.isBusy = false,
  });

  final TimeEntry? shift;

  /// What this handset recorded but has not managed to send.
  ///
  /// Present only when the phone had no signal at the moment the button was
  /// pressed. Everything on the screen follows it rather than the server's
  /// answer while it is here, because it is the newer truth — the worker did
  /// clock in, the office simply does not know yet.
  final PendingClockAction? queued;

  /// Why a clock-in or clock-out was refused.
  ///
  /// The failure rather than its text: this controller has no locale, and a
  /// worker who is out of signal should be told that in their own language
  /// rather than in whichever one the network layer writes its defaults in.
  final ApiException? failure;

  /// A clock action is in flight, so the button is disabled — otherwise a
  /// double tap opens two shifts.
  final bool isBusy;

  /// Whether the worker is on shift, as far as this handset knows.
  bool get isRunning => switch (queued?.action) {
        ClockAction.clockIn => true,
        ClockAction.clockOut => false,
        null => shift != null && shift!.isRunning,
      };

  /// Something is recorded here and not yet on the server.
  bool get isWaitingToSend => queued != null;

  /// When the running shift began, whichever side of the network knows.
  DateTime? get startedAt => queued?.action == ClockAction.clockIn
      ? queued!.occurredAt
      : shift?.startedAt;

  ShiftState copyWith({
    TimeEntry? shift,
    PendingClockAction? queued,
    ApiException? failure,
    bool? isBusy,
    bool clearShift = false,
    bool clearQueued = false,
    bool clearError = false,
  }) {
    return ShiftState(
      shift: clearShift ? null : (shift ?? this.shift),
      queued: clearQueued ? null : (queued ?? this.queued),
      failure: clearError ? null : (failure ?? this.failure),
      isBusy: isBusy ?? this.isBusy,
    );
  }
}

/// The signed-in employee's running shift.
///
/// Only the running shift lives here. Past entries are a separate paged
/// provider: this one is read on every build to decide which button to show,
/// and it must not drag a page of history along with it.
class ShiftController extends AsyncNotifier<ShiftState> {
  /// How long to wait for a position before acting without one.
  ///
  /// A worker in a basement or a lift shaft must not be unable to start work
  /// because the GPS is thinking about it. The stamp is evidence when it is
  /// there, never a precondition.
  static const positionTimeout = Duration(seconds: 8);

  @override
  Future<ShiftState> build() async {
    final user = ref.watch(currentUserProvider);

    // Admin accounts are not linked to an employee; the API answers 403.
    if (user == null || !user.isEmployee) {
      return const ShiftState();
    }

    final queue = ref.read(clockQueueProvider);

    await queue.restore();

    // Anything the last run recorded and could not send goes first, so the
    // screen is built from an up-to-date server rather than showing a shift
    // as unsent that has in fact just been accepted.
    final rejected = await _flush(queue);

    if (queue.last != null) {
      // Still stuck. Show what this handset knows and say it is unsent.
      return ShiftState(queued: queue.last, failure: rejected);
    }

    return ShiftState(
      shift: await ref.read(timeEntryRepositoryProvider).fetchCurrent(),
      failure: rejected,
    );
  }

  /// Sends what is queued, oldest first, and stops at the first thing that
  /// cannot go.
  ///
  /// Two kinds of failure, treated differently on purpose. Still no signal
  /// means keep it and try later. A refusal — the shift is too old now, it
  /// overlaps one already recorded, there is already a shift open — will be a
  /// refusal every time it is retried, so the action is dropped and the
  /// failure surfaced. Retrying it forever would be an app that quietly never
  /// records anything again.
  ///
  /// Returns the refusal, if there was one, rather than setting it: this runs
  /// inside `build`, and anything written to `state` there is replaced by what
  /// `build` goes on to return. The refusal has to travel back out to be shown.
  Future<ApiException?> _flush(ClockQueue queue) async {
    final repository = ref.read(timeEntryRepositoryProvider);

    ApiException? rejected;

    while (true) {
      final action = queue.first;

      if (action == null) {
        return rejected;
      }

      try {
        switch (action.action) {
          case ClockAction.clockIn:
            await repository.clockIn(
              latitude: action.latitude,
              longitude: action.longitude,
              occurredAt: action.occurredAt,
              idempotencyKey: action.idempotencyKey,
            );
          case ClockAction.clockOut:
            await repository.clockOut(
              breakMinutes: action.breakMinutes,
              latitude: action.latitude,
              longitude: action.longitude,
              occurredAt: action.occurredAt,
              idempotencyKey: action.idempotencyKey,
            );
        }

        await queue.acknowledgeFirst();
      } on ApiException catch (exception) {
        if (exception.kind == ApiFailureKind.offline ||
            exception.kind == ApiFailureKind.timeout) {
          return rejected;
        }

        await queue.acknowledgeFirst();
        rejected = exception;
      }
    }
  }

  Future<void> clockIn({String? projectId, String workType = 'Regular'}) {
    return _run(
      ClockAction.clockIn,
      (repository, position) => repository.clockIn(
        projectId: projectId,
        workType: workType,
        latitude: position?.latitude,
        longitude: position?.longitude,
      ),
    );
  }

  Future<void> clockOut({int breakMinutes = 0}) {
    return _run(
      ClockAction.clockOut,
      (repository, position) => repository.clockOut(
        breakMinutes: breakMinutes,
        latitude: position?.latitude,
        longitude: position?.longitude,
      ),
      breakMinutes: breakMinutes,
    );
  }

  Future<void> _run(
    ClockAction kind,
    Future<TimeEntry> Function(TimeEntryRepository, Position?) attempt, {
    int breakMinutes = 0,
  }) async {
    final current = state.value ?? const ShiftState();

    // Read before anything can wait. What goes in the queue has to be the
    // moment the worker pressed the button, not the moment fifteen seconds
    // later when a connection attempt finally gave up.
    final pressedAt = DateTime.now().toUtc();

    state = AsyncData(current.copyWith(isBusy: true, clearError: true));

    final position = await _currentPosition();

    try {
      final entry = await attempt(ref.read(timeEntryRepositoryProvider), position);

      // Clocking out returns the finished shift; there is no running one left.
      state = AsyncData(
        entry.isRunning ? ShiftState(shift: entry) : const ShiftState(),
      );
    } on ApiException catch (exception) {
      if (exception.kind == ApiFailureKind.offline ||
          exception.kind == ApiFailureKind.timeout) {
        // The one failure that is not a refusal. Nothing is wrong with what
        // the worker did — the phone is in a basement — so it is recorded
        // here, with the moment it happened, and sent when there is signal.
        final queued = PendingClockAction(
          action: kind,
          occurredAt: pressedAt,
          idempotencyKey: newIdempotencyKey(),
          breakMinutes: breakMinutes,
          latitude: position?.latitude,
          longitude: position?.longitude,
        );

        await ref.read(clockQueueProvider).add(queued);

        state = AsyncData(
          current.copyWith(isBusy: false, queued: queued, clearError: true),
        );
        return;
      }

      // Everything else is the server saying no — already clocked in, break
      // longer than the shift, no employee record. Queueing those would only
      // move the same refusal to later.
      state = AsyncData(current.copyWith(isBusy: false, failure: exception));
    }
  }

  /// A position for the stamp, or null if one cannot be had quickly.
  Future<Position?> _currentPosition() async {
    try {
      final permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return null;
      }

      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: positionTimeout,
        ),
      );
    } catch (_) {
      // No fix, no permission, no hardware — none of which is a reason to
      // stop someone recording that they are at work.
      return null;
    }
  }
}

final shiftControllerProvider =
    AsyncNotifierProvider<ShiftController, ShiftState>(ShiftController.new);
