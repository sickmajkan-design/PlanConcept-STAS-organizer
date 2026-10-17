import 'package:flutter/material.dart';

/// Fades the edge of a horizontal scroller on the side where there is more to scroll to, so a row that is cut at
/// the screen's edge reads as "there is more here" and not as something that does not fit.
class EdgeFade extends StatefulWidget {
  const EdgeFade({super.key, required this.child, this.width = 28});

  final Widget child;
  final double width;

  @override
  State<EdgeFade> createState() => _EdgeFadeState();
}

class _EdgeFadeState extends State<EdgeFade> {
  bool _before = false;
  bool _after = false;

  bool _update(ScrollMetrics metrics) {
    final before = metrics.extentBefore > 1;
    final after = metrics.extentAfter > 1;

    if (before != _before || after != _after) {
      // Metrics arrive during layout; the repaint waits for the frame to finish.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) {
          return;
        }
        setState(() {
          _before = before;
          _after = after;
        });
      });
    }

    return false;
  }

  @override
  Widget build(BuildContext context) {
    return NotificationListener<ScrollMetricsNotification>(
      onNotification: (n) => _update(n.metrics),
      child: NotificationListener<ScrollNotification>(
        onNotification: (n) => _update(n.metrics),
        child: ShaderMask(
          blendMode: BlendMode.dstIn,
          shaderCallback: (rect) {
            final stop = (widget.width / rect.width).clamp(0.0, 0.5);

            return LinearGradient(
              colors: [
                _before ? Colors.transparent : Colors.black,
                Colors.black,
                Colors.black,
                _after ? Colors.transparent : Colors.black,
              ],
              stops: [0, stop, 1 - stop, 1],
            ).createShader(rect);
          },
          child: widget.child,
        ),
      ),
    );
  }
}
