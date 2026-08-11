import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'app.dart';
import 'core/config/server_address.dart';
import 'core/telemetry/client_error_reporter.dart';
import 'core/widgets/crash_panel.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  installCrashPanel();

  // Before runApp, so a crash while the first screen is being built is
  // reported too — that is the one that leaves a worker with an app that
  // never opens and nobody any the wiser.
  installClientErrorReporting();

  // Read before the first provider is built, because every HTTP client needs
  // the address synchronously and reading a keystore is not synchronous. A
  // device that has never been told one falls back to the compiled default.
  const storage = FlutterSecureStorage();
  final stored = await ServerAddress.read(storage);

  runApp(
    ProviderScope(
      overrides: [
        if (stored != null) initialServerAddressProvider.overrideWithValue(stored),
      ],
      child: const ConstructionApp(),
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
