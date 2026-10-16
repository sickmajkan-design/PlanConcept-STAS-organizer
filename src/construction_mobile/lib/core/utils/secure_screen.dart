import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:flutter/widgets.dart';

/// Keeps the screen out of screenshots, screen recordings and the recent-apps thumbnail while a
/// sensitive document is showing (Android's `FLAG_SECURE`).
///
/// A medical certificate or an identity card on a worker's phone is personal data of a special kind;
/// a screenshot of it goes to a chat or a cloud backup the firm has no say over. iOS has no
/// equivalent switch, so there this does nothing.
class SecureScreen {
  const SecureScreen._();

  static const _channel = MethodChannel('construction/secure_screen');

  /// Documents of these categories keep the screen secure while they are open.
  static bool isSensitive(String category) =>
      category == 'MedicalCheck' || category == 'Identity' || category == 'Visa';

  static Future<void> set(bool secure) async {
    if (kIsWeb || !Platform.isAndroid) {
      return;
    }

    try {
      await _channel.invokeMethod<void>('set', secure);
    } on MissingPluginException {
      // A build without the native side (a test, an older shell): nothing to switch.
    } on PlatformException {
      // Best effort: failing to mark the screen secure must never stop the document opening.
    }
  }
}

/// Marks the screen secure for as long as it is in the tree, and releases it afterwards.
class SecureScope extends StatefulWidget {
  const SecureScope({super.key, required this.enabled, required this.child});

  final bool enabled;
  final Widget child;

  @override
  State<SecureScope> createState() => _SecureScopeState();
}

class _SecureScopeState extends State<SecureScope> {
  @override
  void initState() {
    super.initState();

    if (widget.enabled) {
      SecureScreen.set(true);
    }
  }

  @override
  void dispose() {
    if (widget.enabled) {
      SecureScreen.set(false);
    }

    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
