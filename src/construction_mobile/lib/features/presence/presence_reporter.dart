import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/network_providers.dart';
import '../../core/router/app_router.dart';
import '../auth/presentation/auth_controller.dart';

/// How often the app tells the office it is open and in use.
const Duration presenceInterval = Duration(minutes: 1);

/// Tells the API, once a minute while the app is in the foreground and
/// someone is signed in, that this account is online and which screen it is on.
///
/// Foreground only, on purpose: a phone in a pocket with the app in the
/// background is not "using the platform", and the office list must not show a
/// worker as online for as long as the app merely exists. Failures are ignored
/// — no signal is the normal state on a site, and the ordinary API calls keep
/// the account marked as active anyway.
class PresenceReporter {
  PresenceReporter(this._ref);

  final Ref _ref;
  Timer? _timer;

  /// Starts (or restarts) reporting, sending one right away.
  void start() {
    _timer?.cancel();
    unawaited(_send());
    _timer = Timer.periodic(presenceInterval, (_) => unawaited(_send()));
  }

  void stop() {
    _timer?.cancel();
    _timer = null;
  }

  Future<void> _send() async {
    try {
      if (_ref.read(authControllerProvider).value is! Authenticated) return;

      final screen = _ref
          .read(routerProvider)
          .routeInformationProvider
          .value
          .uri
          .path;

      await _ref.read(apiClientProvider).post<void>(
        '/api/v1/presence/heartbeat',
        data: {'screen': screen},
        options: Options(sendTimeout: const Duration(seconds: 10)),
      );
    } catch (_) {
      // Best effort. See the class comment.
    }
  }
}

final presenceReporterProvider = Provider<PresenceReporter>((ref) {
  final reporter = PresenceReporter(ref);
  ref.onDispose(reporter.stop);
  return reporter;
});
