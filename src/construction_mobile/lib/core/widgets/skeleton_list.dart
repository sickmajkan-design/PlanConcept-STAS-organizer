import 'package:flutter/material.dart';

/// What a list looks like while it loads: the rows that are about to appear, as quiet bars that breathe.
///
/// Nothing jumps when the data arrives, because the page already has the shape of the answer, and it
/// reads as faster than a spinner in the middle of an empty screen. With reduced motion the bars stand still.
class SkeletonList extends StatefulWidget {
  const SkeletonList({super.key, this.rows = 6});

  final int rows;

  @override
  State<SkeletonList> createState() => _SkeletonListState();
}

class _SkeletonListState extends State<SkeletonList> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1100),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final reduceMotion = MediaQuery.disableAnimationsOf(context);
    final base = Theme.of(context).colorScheme.onSurface;

    if (reduceMotion && _controller.isAnimating) {
      _controller.stop();
    }

    return Semantics(
      label: MaterialLocalizations.of(context).refreshIndicatorSemanticLabel,
      child: ExcludeSemantics(
        child: AnimatedBuilder(
          animation: _controller,
          builder: (context, _) {
            final opacity = reduceMotion ? 0.08 : 0.06 + 0.06 * _controller.value;
            final color = base.withValues(alpha: opacity);

            return ListView.separated(
              physics: const NeverScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
              itemCount: widget.rows,
              separatorBuilder: (_, _) => const SizedBox(height: 16),
              itemBuilder: (_, index) => Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(color: color, shape: BoxShape.circle),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        FractionallySizedBox(
                          // Different lengths, so it does not look like a barcode.
                          widthFactor: 0.45 + (index % 3) * 0.15,
                          child: _Bar(color: color, height: 14),
                        ),
                        const SizedBox(height: 8),
                        FractionallySizedBox(
                          widthFactor: 0.7 - (index % 2) * 0.2,
                          child: _Bar(color: color, height: 11),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _Bar extends StatelessWidget {
  const _Bar({required this.color, required this.height});

  final Color color;
  final double height;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: height,
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(height / 2)),
    );
  }
}
