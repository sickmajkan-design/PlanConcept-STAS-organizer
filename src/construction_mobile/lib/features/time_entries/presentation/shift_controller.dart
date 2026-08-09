import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/network/api_exception.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/time_entry.dart';
import '../data/time_entry_repository.dart';

/// What the shift card shows.
///
/// A record rather than leaning on `AsyncValue` for the failure case: a
/// refused clock-out has to leave the running shift on screen so the worker
/// can try again, and an `AsyncError` would blank it. The error rides
/// alongside the shift instead of replacing it.
class ShiftState {
  const ShiftState({this.shift, this.failure, this.isBusy = false});

  final TimeEntry? shift;

  /// Why a clock-in or clock-out was refused.
  ///
  /// The failure rather than its text: this controller has no locale, and a
  /// worker who is out of signal should be told that in their own language
  /// rather than in whichever one the network layer writes its defaults in.
  final ApiException? failure;

  /// A clock action is in flight, so the button is disabled — otherwise a
  /// double tap opens two shifts.
  final bool isBusy;

  bool get isRunning => shift != null && shift!.isRunning;

  ShiftState copyWith({
    TimeEntry? shift,
    ApiException? failure,
    bool? isBusy,
    bool clearShift = false,
    bool clearError = false,
  }) {
    return ShiftState(
      shift: clearShift ? null : (shift ?? this.shift),
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

    return ShiftState(
      shift: await ref.read(timeEntryRepositoryProvider).fetchCurrent(),
    );
  }

  Future<void> clockIn({String? projectId, String workType = 'Regular'}) {
    return _run((repository, position) => repository.clockIn(
          projectId: projectId,
          workType: workType,
          latitude: position?.latitude,
          longitude: position?.longitude,
        ));
  }

  Future<void> clockOut({int breakMinutes = 0}) {
    return _run((repository, position) => repository.clockOut(
          breakMinutes: breakMinutes,
          latitude: position?.latitude,
          longitude: position?.longitude,
        ));
  }

  Future<void> _run(
    Future<TimeEntry> Function(TimeEntryRepository, Position?) action,
  ) async {
    final current = state.value ?? const ShiftState();

    state = AsyncData(current.copyWith(isBusy: true, clearError: true));

    try {
      final position = await _currentPosition();
      final entry = await action(ref.read(timeEntryRepositoryProvider), position);

      // Clocking out returns the finished shift; there is no running one left.
      state = AsyncData(
        entry.isRunning
            ? ShiftState(shift: entry)
            : const ShiftState(),
      );
    } on ApiException catch (exception) {
      state = AsyncData(
        current.copyWith(isBusy: false, failure: exception),
      );
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
