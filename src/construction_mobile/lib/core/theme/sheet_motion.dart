import 'package:flutter/material.dart';

/// How bottom sheets move: out quickly and settle with a small give at the end, back down a little faster.
/// The overshoot is a few percent only, enough to feel alive without the sheet leaving a gap under itself.
/// With the phone's "reduce motion" on there is no travel to speak of.
AnimationStyle sheetMotion(BuildContext context) {
  if (MediaQuery.disableAnimationsOf(context)) {
    return const AnimationStyle(duration: Duration(milliseconds: 1), reverseDuration: Duration(milliseconds: 1));
  }

  return const AnimationStyle(
    curve: Cubic(0.32, 1.18, 0.42, 1),
    duration: Duration(milliseconds: 460),
    reverseCurve: Curves.easeInCubic,
    reverseDuration: Duration(milliseconds: 240),
  );
}
