import 'dart:async';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../config/app_config.dart';

/// Sends a crash on a phone to the API, which logs it beside everything else.
///
/// This is the half of error tracking that matters most here. A panel that
/// breaks breaks in an office, in front of somebody who can say so. The app
/// breaks on a site, on a device nobody will ever hand over, usually in the
/// one situation the developer could not reproduce — bad signal, an odd
/// Android build, a permission revoked mid-shift. Without this, the entire
/// record of that is a crash panel one worker looked at and dismissed.
///
/// Deliberately not a third-party crash service. What this reports — screens,
/// stack traces, app versions from a workforce tracker — is the operator's
/// data, and where it goes should be their decision rather than a dependency's
/// default. The API is on their host and already has the logging pipeline.
///
/// Fire-and-forget, always. A crash reporter that throws, retries, or blocks
/// startup has made the crash worse than it was.
class ClientErrorReporter {
  /// Built with its own bare [Dio] rather than the app's.
  ///
  /// The configured client carries the auth interceptor and the offline cache.
  /// Reporting a crash through those means a failed report can refresh a
  /// token, which can fail, which is another error — a loop that starts at the
  /// worst possible moment. This one has no interceptors at all.
  ClientErrorReporter({Dio? client})
      : _client = client ??
            Dio(BaseOptions(
              baseUrl: AppConfig.apiBaseUrl,
              connectTimeout: const Duration(seconds: 8),
              sendTimeout: const Duration(seconds: 8),
              receiveTimeout: const Duration(seconds: 8),
              // Any status is a result, never an exception. There is nothing
              // useful to do with a rejected crash report.
              validateStatus: (_) => true,
            ));

  /// Longer than any useful trace, shorter than what the API will refuse.
  static const int maxStack = 8000;
  static const int maxMessage = 1500;

  /// Reports sent per launch.
  ///
  /// A widget that throws on every frame throws sixty times a second, and
  /// after the first the stack is identical. The server has its own limit;
  /// this stops a broken phone spending its battery and its data allowance
  /// describing the same fault.
  static const int maxPerLaunch = 10;

  final Dio _client;

  int _sent = 0;
  String _previous = '';

  /// True while a report is in flight, so a failure to report cannot report
  /// itself — the API being unreachable is exactly when that would loop.
  bool _reporting = false;

  Future<void> report(
    Object error, {
    StackTrace? stackTrace,
    String? screen,
  }) async {
    if (_reporting || _sent >= maxPerLaunch) {
      return;
    }

    final message = _truncate(error.toString(), maxMessage);
    final fingerprint = '${error.runtimeType}:$message';

    if (fingerprint == _previous) {
      return;
    }

    _previous = fingerprint;
    _sent += 1;
    _reporting = true;

    try {
      await _client.post<void>(
        '/api/v1/client-errors',
        data: {
          'app': 'mobile',
          'message': message,
          'kind': error.runtimeType.toString(),
          if (stackTrace != null) 'stack': _truncate(stackTrace.toString(), maxStack),
          'route': ?screen,
          'platform': _platform(),
        },
        // No Authorization header. The report needs no account, and an
        // endpoint that works signed out is the one that can report a sign-in
        // screen that will not load.
        options: Options(headers: const {'Content-Type': 'application/json'}),
      );
    } catch (_) {
      // Intentionally empty, and the only one in this app that should be:
      // there is nowhere left to report a failure to report.
    } finally {
      _reporting = false;
    }
  }

  static String _platform() {
    try {
      return '${Platform.operatingSystem} ${Platform.operatingSystemVersion}';
    } catch (_) {
      // Platform is unavailable on some targets and in some tests.
      return 'unknown';
    }
  }

  static String _truncate(String value, int limit) =>
      value.length <= limit ? value : '${value.substring(0, limit)}\n… truncated';

  /// Test seam: forgets what has been sent and how much.
  @visibleForTesting
  void reset() {
    _sent = 0;
    _previous = '';
    _reporting = false;
  }
}

/// The reporter the app's error handlers use.
///
/// A plain global rather than a provider: these handlers are installed before
/// `runApp`, so there is no container to read from yet, and a crash during
/// start-up is one of the crashes worth hearing about.
ClientErrorReporter clientErrorReporter = ClientErrorReporter();

/// Routes Flutter's two error channels into the reporter.
///
/// [FlutterError.onError] catches what breaks inside the framework — a build,
/// a layout, a paint. [PlatformDispatcher.instance.onError] catches what
/// escapes everywhere else: an unawaited future, an isolate's uncaught throw.
/// Only the first has ever been wired here, and only to the console.
void installClientErrorReporting() {
  final previousOnError = FlutterError.onError;

  FlutterError.onError = (details) {
    // The original first, so the red screen in debug and the console output in
    // release both behave exactly as before.
    previousOnError?.call(details);

    unawaited(clientErrorReporter.report(
      details.exception,
      stackTrace: details.stack,
      screen: details.library,
    ));
  };

  PlatformDispatcher.instance.onError = (error, stack) {
    unawaited(clientErrorReporter.report(error, stackTrace: stack));

    // False, meaning "not handled": the platform still logs it. Claiming to
    // have handled a crash because it was reported is how a fault becomes
    // invisible in the place it was already visible.
    return false;
  };
}
