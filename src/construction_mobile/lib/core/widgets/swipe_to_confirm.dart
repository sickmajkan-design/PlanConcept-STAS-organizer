import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/physics.dart';
import 'package:flutter/services.dart';

/// A track with a knob that is dragged to the end to confirm. Used for the few actions that should not happen
/// by accident in a pocket or with a glove: clocking in and out.
///
/// Let go before the end and the knob springs back from where the finger left it, carrying its speed; let go past
/// nine tenths of the way and the action runs. A screen reader (or a double tap on the whole track) runs it
/// directly, so nobody is locked out by the gesture.
class SwipeToConfirm extends StatefulWidget {
  const SwipeToConfirm({
    super.key,
    required this.label,
    required this.icon,
    required this.onConfirmed,
    required this.trackColor,
    required this.fillColor,
    required this.knobColor,
    required this.knobIconColor,
    required this.textColor,
    this.busy = false,
    this.enabled = true,
    this.height = 56,
  });

  final String label;
  final IconData icon;
  final VoidCallback onConfirmed;
  final Color trackColor;

  /// What the track fills with behind the knob while it is dragged.
  final Color fillColor;
  final Color knobColor;
  final Color knobIconColor;
  final Color textColor;

  /// A call is in flight: the knob shows a spinner and cannot be moved.
  final bool busy;
  final bool enabled;
  final double height;

  @override
  State<SwipeToConfirm> createState() => _SwipeToConfirmState();
}

class _SwipeToConfirmState extends State<SwipeToConfirm> with SingleTickerProviderStateMixin {
  static const _inset = 4.0;

  late final AnimationController _controller = AnimationController.unbounded(vsync: this);
  double _max = 0;
  bool _reachedEnd = false;

  bool get _active => widget.enabled && !widget.busy;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _drag(DragUpdateDetails details) {
    if (!_active || _max <= 0) return;

    final next = (_controller.value + details.delta.dx).clamp(0.0, _max);

    // A short tick when the knob is far enough that letting go will confirm.
    final past = next >= _max * 0.9;
    if (past && !_reachedEnd) unawaited(HapticFeedback.selectionClick());
    _reachedEnd = past;

    _controller.value = next;
  }

  void _release(DragEndDetails details) {
    if (!_active || _max <= 0) return;

    if (_controller.value >= _max * 0.9) {
      unawaited(HapticFeedback.mediumImpact());
      widget.onConfirmed();
    }

    _reachedEnd = false;
    _springBack(details.velocity.pixelsPerSecond.dx);
  }

  void _springBack(double velocity) {
    if (MediaQuery.disableAnimationsOf(context)) {
      _controller.value = 0;
      return;
    }

    // Critically damped would be flat; a little bounce says "it went back".
    final spring = SpringDescription.withDampingRatio(mass: 1, stiffness: 320, ratio: 0.8);
    _controller.animateWith(SpringSimulation(spring, _controller.value, 0, velocity));
  }

  @override
  Widget build(BuildContext context) {
    final radius = BorderRadius.circular(widget.height / 2);
    final knob = widget.height - _inset * 2;

    return Semantics(
      button: true,
      enabled: _active,
      label: widget.label,
      onTap: _active ? widget.onConfirmed : null,
      child: ExcludeSemantics(
        child: LayoutBuilder(
          builder: (context, constraints) {
            _max = (constraints.maxWidth - knob - _inset * 2).clamp(0.0, double.infinity);

            return SizedBox(
              height: widget.height,
              width: double.infinity,
              child: DecoratedBox(
                decoration: BoxDecoration(color: widget.trackColor, borderRadius: radius),
                child: ClipRRect(
                  borderRadius: radius,
                  child: AnimatedBuilder(
                    animation: _controller,
                    builder: (context, _) {
                      final x = _controller.value.clamp(0.0, _max);
                      final progress = _max > 0 ? x / _max : 0.0;

                      return Stack(
                        alignment: Alignment.centerLeft,
                        children: [
                          Positioned(
                            left: 0,
                            top: 0,
                            bottom: 0,
                            width: _inset + x + knob + _inset,
                            child: ColoredBox(color: widget.fillColor.withValues(alpha: 0.22 + 0.5 * progress)),
                          ),
                          Center(
                            child: Padding(
                              padding: EdgeInsets.only(left: knob + _inset * 2),
                              child: Opacity(
                                opacity: (1 - progress * 1.4).clamp(0.0, 1.0),
                                child: Text(
                                  widget.label,
                                  maxLines: 1,
                                  softWrap: false,
                                  overflow: TextOverflow.fade,
                                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                        color: widget.textColor,
                                        fontWeight: FontWeight.w600,
                                      ),
                                ),
                              ),
                            ),
                          ),
                          Positioned(
                            left: _inset + x,
                            child: GestureDetector(
                              behavior: HitTestBehavior.opaque,
                              onHorizontalDragUpdate: _drag,
                              onHorizontalDragEnd: _release,
                              onHorizontalDragCancel: () => _springBack(0),
                              child: SizedBox(
                                width: knob,
                                height: knob,
                                child: DecoratedBox(
                                  decoration: BoxDecoration(
                                    color: widget.knobColor,
                                    shape: BoxShape.circle,
                                    boxShadow: const [
                                      BoxShadow(color: Color(0x33000000), blurRadius: 6, offset: Offset(0, 2)),
                                    ],
                                  ),
                                  child: Center(
                                    child: widget.busy
                                        ? SizedBox(
                                            width: 20,
                                            height: 20,
                                            child: CircularProgressIndicator(
                                              strokeWidth: 2,
                                              color: widget.knobIconColor,
                                            ),
                                          )
                                        : Icon(widget.icon, color: widget.knobIconColor),
                                  ),
                                ),
                              ),
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
