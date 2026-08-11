import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/telemetry/client_error_reporter.dart';
import 'core/widgets/crash_panel.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  installCrashPanel();

  // Before runApp, so a crash while the first screen is being built is
  // reported too — that is the one that leaves a worker with an app that
  // never opens and nobody any the wiser.
  installClientErrorReporting();

  runApp(
    const ProviderScope(
      child: ConstructionApp(),
    ),
  );
}

/// Replaces Flutter's release-mode grey box with something that says what
/// happened.
///
/// Debug keeps the red screen on purpose. It carries the exception and the
/// widget stack, which is the whole reason it is unbearable to look at and the
/// whole reason it is worth keeping in front of whoever can fix it. The
/// friendly panel is for the phone in the field, where nobody is going to read
/// a stack trace and the only question is whether the app is broken or slow.
///
/// The error still reaches `FlutterError.onError` either way, so replacing the
/// widget hides nothing from the console or from a crash reporter added later.
@visibleForTesting
void installCrashPanel() {
  if (kDebugMode) {
    return;
  }

  ErrorWidget.builder = (_) => const CrashPanel();
}
