import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/config/app_config.dart';
import '../../../core/l10n/app_message.dart';
import '../../../core/network/api_exception.dart';
import '../../auth/presentation/auth_controller.dart';
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
    this.queuedReason,
  });

  final LocationTrackingStatus status;
  final DateTime? lastReportedAt;

  /// Fixes captured but not yet accepted by the server (poor coverage).
  final int pendingCount;
  /// A translatable app message; resolved by the widget that shows it.
  final AppMessage? message;

  /// Server text for a queued batch, which cannot be translated client-side.
  final String? queuedReason;

  bool get isTracking =>
      status == LocationTrackingStatus.active ||
      status == LocationTrackingStatus.starting;

  LocationTrackingState copyWith({
    LocationTrackingStatus? status,
    DateTime? lastReportedAt,
    int? pendingCount,
    AppMessage? message,
    String? queuedReason,
    bool clearMessage = false,
  }) {
    return LocationTrackingState(
      status: status ?? this.status,
      lastReportedAt: lastReportedAt ?? this.lastReportedAt,
      pendingCount: pendingCount ?? this.pendingCount,
      message: clearMessage ? null : (message ?? this.message),
      queuedReason: clearMessage ? null : (queuedReason ?? this.queuedReason),
    );
  }
}

/// Reports the device position to the API once a minute while an
/// employee-linked account is signed in.
///
/// Fixes that cannot be delivered (no coverage on site) are buffered and sent
/// in the next batch, which is why the API accepts batches at all. The buffer
/// is capped at one batch so a long outage cannot grow it without bound.
class LocationTrackingController extends Notifier<LocationTrackingState> {
  Timer? _timer;
  final List<LocationPing> _buffer = <LocationPing>[];
  bool _busy = false;

  @override
  LocationTrackingState build() {
    final user = ref.watch(currentUserProvider);

    ref.onDispose(() {
      _timer?.cancel();
      _timer = null;
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
    final permission = await _ensurePermission();

    if (permission != LocationTrackingStatus.active) {
      state = state.copyWith(status: permission);
      return;
    }

    state = state.copyWith(
      status: LocationTrackingStatus.active,
      clearMessage: true,
    );

    _timer?.cancel();
    _timer = Timer.periodic(AppConfig.locationReportInterval, (_) => _tick());

    await _tick();
  }

  /// Re-runs the permission flow, e.g. after the user returns from settings.
  Future<void> retry() => _start();

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

  Future<void> _tick() async {
    if (_busy) {
      return;
    }

    _busy = true;

    try {
      await _capture();
      await _flush();
    } finally {
      _busy = false;
    }
  }

  Future<void> _capture() async {
    try {
      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 30),
        ),
      );

      _buffer.add(
        LocationPing(
          latitude: position.latitude,
          longitude: position.longitude,
          accuracy: position.accuracy,
          timestamp: position.timestamp.toUtc(),
        ),
      );

      // Keep only the newest batch; older fixes lose their value quickly.
      if (_buffer.length > LocationRepository.maxBatchSize) {
        _buffer.removeRange(0, _buffer.length - LocationRepository.maxBatchSize);
      }

      state = state.copyWith(pendingCount: _buffer.length, clearMessage: true);
    } on TimeoutException {
      state = state.copyWith(message: AppMessage.locationNoFix);
    } catch (error) {
      state = state.copyWith(
        status: LocationTrackingStatus.error,
        message: AppMessage.locationReadFailed,
      );
    }
  }

  Future<void> _flush() async {
    if (_buffer.isEmpty) {
      return;
    }

    final batch = List<LocationPing>.unmodifiable(_buffer);

    try {
      await ref.read(locationRepositoryProvider).report(batch);

      // Only drop what was actually accepted; anything captured meanwhile
      // stays queued for the next batch.
      _buffer.removeRange(0, batch.length);

      state = state.copyWith(
        status: LocationTrackingStatus.active,
        lastReportedAt: DateTime.now().toUtc(),
        pendingCount: _buffer.length,
        clearMessage: true,
      );
    } on ApiException catch (exception) {
      // 403 means this account may not report at all — stop trying.
      if (exception.statusCode == 403) {
        _timer?.cancel();
        _buffer.clear();
        state = const LocationTrackingState(status: LocationTrackingStatus.off);
        return;
      }

      state = state.copyWith(
        pendingCount: _buffer.length,
        queuedReason: exception.message,
      );
    }
  }
}

final locationTrackingProvider =
    NotifierProvider<LocationTrackingController, LocationTrackingState>(
  LocationTrackingController.new,
);
