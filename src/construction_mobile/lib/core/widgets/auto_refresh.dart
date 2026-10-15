import 'dart:async';

import 'package:flutter/widgets.dart';

/// Calls [onRefresh] on a short interval while the screen is showing, and once more when the app
/// comes back to the foreground.
///
/// A push notice reaches the phone at once, but what is on screen should not depend on the person
/// noticing it: a request the office has just decided must read as decided without a pull to
/// refresh. There is no live connection to the server, so the screen asks, quietly, every so often.
class AutoRefresh extends StatefulWidget {
  const AutoRefresh({
    super.key,
    required this.onRefresh,
    required this.child,
    this.interval = const Duration(seconds: 30),
  });

  final VoidCallback onRefresh;
  final Widget child;
  final Duration interval;

  @override
  State<AutoRefresh> createState() => _AutoRefreshState();
}

class _AutoRefreshState extends State<AutoRefresh> with WidgetsBindingObserver {
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _start();
  }

  void _start() {
    _timer?.cancel();
    _timer = Timer.periodic(widget.interval, (_) => widget.onRefresh());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    switch (state) {
      case AppLifecycleState.resumed:
        widget.onRefresh();
        _start();
      case AppLifecycleState.paused:
      case AppLifecycleState.hidden:
      case AppLifecycleState.detached:
      case AppLifecycleState.inactive:
        _timer?.cancel();
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
