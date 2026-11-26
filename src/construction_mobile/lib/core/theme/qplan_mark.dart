import 'package:flutter/material.dart';

/// The QPlan mark: a rounded Q whose window holds three schedule bars, with its tail as a cable.
/// Drawn in code, so it needs no asset and follows the light or dark theme.
/// The source vector files are in `brand/` at the repository root.
class QPlanMark extends StatelessWidget {
  const QPlanMark({super.key, this.size = 72});

  final double size;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;

    return Semantics(
      label: 'QPlan',
      image: true,
      child: CustomPaint(
        size: Size.square(size),
        painter: _QPlanMarkPainter(
          ring: dark ? const Color(0xFFFF8A3D) : const Color(0xFFE65100),
          quiet: dark ? const Color(0xFFB0BEC5) : const Color(0xFF37474F),
        ),
      ),
    );
  }
}

class _QPlanMarkPainter extends CustomPainter {
  const _QPlanMarkPainter({required this.ring, required this.quiet});

  final Color ring;
  final Color quiet;

  @override
  void paint(Canvas canvas, Size size) {
    final s = size.width / 100;

    final stroke = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 9 * s
      ..strokeCap = StrokeCap.round
      ..color = ring;
    canvas.drawRRect(
      RRect.fromRectAndRadius(Rect.fromLTWH(14 * s, 12 * s, 66 * s, 66 * s), Radius.circular(22 * s)),
      stroke,
    );

    void bar(double x, double y, double w, Color color) {
      canvas.drawRRect(
        RRect.fromRectAndRadius(Rect.fromLTWH(x * s, y * s, w * s, 7 * s), Radius.circular(3.5 * s)),
        Paint()..color = color,
      );
    }

    bar(28, 28, 20, quiet);
    bar(38, 42, 28, ring);
    bar(28, 56, 16, quiet);

    canvas.drawLine(Offset(66 * s, 66 * s), Offset(90 * s, 90 * s), Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 9 * s
      ..strokeCap = StrokeCap.round
      ..color = quiet);
  }

  @override
  bool shouldRepaint(_QPlanMarkPainter old) => old.ring != ring || old.quiet != quiet;
}
