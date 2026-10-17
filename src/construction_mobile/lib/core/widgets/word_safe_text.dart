import 'package:flutter/material.dart';

/// Text that wraps between words but never inside one. When the longest word does not fit the width, the whole
/// label is set a little smaller until it does (down to [minScale]), so a word is never cut in two.
class WordSafeText extends StatelessWidget {
  const WordSafeText(
    this.text, {
    super.key,
    this.style,
    this.maxLines = 2,
    this.textAlign = TextAlign.center,
    this.minScale = 0.72,
    this.reserveLines = 0,
  });

  final String text;
  final TextStyle? style;
  final int maxLines;
  final TextAlign textAlign;
  final double minScale;

  /// Room for this many lines is always kept (a layout builder reports no height of its own, so a row that sizes
  /// itself by its tallest child needs it); the text sits in the middle of that room.
  final int reserveLines;

  @override
  Widget build(BuildContext context) {
    final base = style ?? DefaultTextStyle.of(context).style;
    final scaler = MediaQuery.textScalerOf(context);

    final lineHeight = (base.fontSize ?? 14) * (base.height ?? 1.2);
    final reserved = reserveLines > 0 ? scaler.scale(lineHeight) * reserveLines : null;

    final label = LayoutBuilder(
      builder: (context, constraints) {
        var scale = 1.0;

        if (constraints.maxWidth.isFinite) {
          var longest = 0.0;

          for (final word in text.split(RegExp(r'\s+'))) {
            final painter = TextPainter(
              text: TextSpan(text: word, style: base),
              textDirection: Directionality.of(context),
              textScaler: scaler,
              maxLines: 1,
            )..layout();

            if (painter.width > longest) longest = painter.width;
            painter.dispose();
          }

          if (longest > constraints.maxWidth) {
            scale = (constraints.maxWidth / longest).clamp(minScale, 1.0);
          }
        }

        return Text(
          text,
          maxLines: maxLines,
          textAlign: textAlign,
          overflow: TextOverflow.ellipsis,
          style: scale < 1 ? base.copyWith(fontSize: (base.fontSize ?? 14) * scale * 0.94) : base,
        );
      },
    );

    return reserved == null ? label : SizedBox(height: reserved, child: Center(child: label));
  }
}
