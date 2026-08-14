import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/app_message.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/background_location_settings.dart';
import '../data/location_queue.dart';
import '../data/location_repository.dart';

enum LocationTrackingStatus {
  /// Nobody is signed in, or the account is not linked to an employee.
  off,
  starting,
  active,

  /// The device's location services are switched off entirely.
  serviceDisabled,
  permissionDenied,

  /// Denied permanently — only the system settings can undo this.
  permissionBlocked,
  error,
}

class LocationTrackingState {
  const LocationTrackingState({
    required this.status,
    this.lastReportedAt,
    this.pendingCount = 0,
    this.message,
    this.queuedFailure,
  });

  final LocationTrackingStatus status;
  final DateTime? lastReportedAt;

  /// Fixes captured but not yet accepted by the server (poor coverage).
  final int pendingCount;

  /// A translatable app message; resolved by the widget that shows it.
  final AppMessage? message;

  /// Why a batch of pings is still sitting in the queue.
  ///
  /// The failure, not a sentence: this is the state a phone sits in for an
  /// hour at a time on a site with no signal, and it should be readable in the
  /// operator's language for all of it.
  final ApiException? queuedFailure;

  bool get isTracking =>
      status == LocationTrackingStatus.active ||
      status == LocationTrackingStatus.starting;

  LocationTrackingState copyWith({
    LocationTrackingStatus? status,
    DateTime? lastReportedAt,
    int? pendingCount,
    AppMessage? message,
    ApiException? queuedFailure,
    bool clearMessage = false,
  }) {
    return LocationTrackingState(
      status: status ?? this.status,
      lastReportedAt: lastReportedAt ?? this.lastReportedAt,
      pendingCount: pendingCount ?? this.pendingCount,
      message: clearMessage ? null : (message ?? this.message),
      queuedFailure:
          clearMessage ? null : (queuedFailure ?? this.queuedFailure),
    );
  }
}

/// Reports the device position to the API while an employee-linked account is
/// signed in, including with the app off screen.
///
/// The platform, not a timer, drives capture: [backgroundLocationSettings]
/// attaches the stream to an Android foreground service or Apple background
/// location updates, so a phone in a pocket keeps reporting. A timer could
/// not — the framework stops servicing timers the moment the app is
/// backgrounded, which is the state a site worker's phone spends the day in.
///
/// Fixes that cannot be delivered (no coverage on site) go to [LocationQueue],
/// which survives the process being reclaimed mid-shift, and are sent in the
/// next batch — which is why the API accepts batches at all.
class LocationTrackingController extends Notifier<LocationTrackingState> {
  StreamSubscription<Position>? _subscription;
  bool _busy = false;

  /// Set once this notifier has been thrown away.
  ///
  /// Which happens on every sign-out, because [build] watches who is signed
  /// in. Starting is a sequence of awaits — restoring the queue, asking for
  /// permission, loading the notification's wording — and the session can end
  /// at any point in it. `onDispose` alone does not cover that: it cancels a
  /// subscription that does not exist yet, and the one attached a moment later
  /// belongs to an object nobody holds a reference to any more. On Android
  /// that is a foreground service, its permanent notification and a GPS fix
  /// still running for somebody who has signed out, with nothing left alive to
  /// stop them.
  bool _abandoned = false;

  @override
  LocationTrackingState build() {
    final user = ref.watch(currentUserProvider);

    ref.onDispose(() {
      _abandoned = true;
      _stopStream();
    });

    // Admin accounts are not linked to an employee; the API would reject
    // their pings with 403, so the app does not ask for location at all.
    if (user == null || !user.isEmployee) {
      return const LocationTrackingState(status: LocationTrackingStatus.off);
    }

    scheduleMicrotask(_start);
    return const LocationTrackingState(status: LocationTrackingStatus.starting);
  }

  Future<void> _start() async {
    final queue = ref.read(locationQueueProvider);

    // Anything the previous run captured but could not deliver.
    await queue.restore();

    if (_abandoned) {
      return;
    }

    final permission = await _ensurePermission();

    if (_abandoned) {
      return;
    }

    if (permission != LocationTrackingStatus.active) {
      state = state.copyWith(
        status: permission,
        pendingCount: queue.length,
      );
      return;
    }

    state = state.copyWith(
      status: LocationTrackingStatus.active,
      pendingCount: queue.length,
      clearMessage: true,
    );

    await _listen();

    // A queue carried over from the last run should not wait for the first
    // fix of this one, which may be minutes away.
    if (!queue.isEmpty) {
      await _flush();
    }
  }

  /// Re-runs the permission flow, e.g. after the user returns from settings.
  Future<void> retry() => _start();

  Future<void> _listen() async {
    _stopStream();

    // The service notification is text the worker reads all day, so it is
    // resolved in the language they picked rather than the build's default.
    final l10n = await _localisations();

    // Reading the chosen language is one more await the session can end
    // during, and the last one before a stream exists to leak.
    if (_abandoned) {
      return;
    }

    _subscription = Geolocator.getPositionStream(
      locationSettings: backgroundLocationSettings(l10n),
    ).listen(
      _onPosition,
      onError: _onStreamError,
      cancelOnError: false,
    );
  }

  Future<AppLocalizations> _localisations() async {
    final selected = await ref.read(localeControllerProvider.future);

    return AppLocalizations.delegate.load(
      resolveLocale(selected, supportedLocales),
    );
  }

  void _stopStream() {
    _subscription?.cancel();
    _subscription = null;
  }

  Future<LocationTrackingStatus> _ensurePermission() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        return LocationTrackingStatus.serviceDisabled;
      }

      var permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      return switch (permission) {
        LocationPermission.denied => LocationTrackingStatus.permissionDenied,
        LocationPermission.deniedForever =>
          LocationTrackingStatus.permissionBlocked,
        LocationPermission.unableToDetermine => LocationTrackingStatus.error,
        _ => LocationTrackingStatus.active,
      };
    } catch (_) {
      // No location hardware or platform support on this device.
      return LocationTrackingStatus.error;
    }
  }

  Future<void> _onPosition(Position position) async {
    await ref.read(locationQueueProvider).add(
          LocationPing(
            latitude: position.latitude,
            longitude: position.longitude,
            accuracy: position.accuracy,
            timestamp: position.timestamp.toUtc(),
          ),
        );

    state = state.copyWith(
      status: LocationTrackingStatus.active,
      pendingCount: ref.read(locationQueueProvider).length,
      clearMessage: true,
    );

    await _flush();
  }

  void _onStreamError(Object error) {
    // A dropped fix is not a dropped stream: the platform keeps trying, so
    // the subscription stays and only the surfaced message changes.
    state = state.copyWith(
      message: error is TimeoutException
          ? AppMessage.locationNoFix
          : AppMessage.locationReadFailed,
    );
  }

  Future<void> _flush() async {
    if (_busy || _abandoned) {
      // Abandoned means signed out, and a batch posted then carries a token
      // the API has already been asked to revoke.
      return;
    }

    final queue = ref.read(locationQueueProvider);

    if (queue.isEmpty) {
      return;
    }

    _busy = true;

    try {
      final batch = queue.pending;

      await ref.read(locationRepositoryProvider).report(batch);

      // Only drop what was actually accepted; anything captured meanwhile
      // stays queued for the next batch.
      await queue.acknowledge(batch.length);

      state = state.copyWith(
        status: LocationTrackingStatus.active,
        lastReportedAt: DateTime.now().toUtc(),
        pendingCount: queue.length,
        clearMessage: true,
      );
    } on ApiException catch (exception) {
      // 403 means this account may not report at all — stop trying.
      if (exception.statusCode == 403) {
        _stopStream();
        await queue.clear();
        state = const LocationTrackingState(status: LocationTrackingStatus.off);
        return;
      }

      state = state.copyWith(
        pendingCount: queue.length,
        queuedFailure: exception,
      );
    } finally {
      _busy = false;
    }
  }
}

final locationTrackingProvider =
    NotifierProvider<LocationTrackingController, LocationTrackingState>(
  LocationTrackingController.new,
);
