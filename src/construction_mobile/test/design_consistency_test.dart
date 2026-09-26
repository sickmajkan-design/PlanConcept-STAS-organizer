import 'dart:io';

import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:flutter/painting.dart';
import 'package:flutter_test/flutter_test.dart';

/// The app has one look, and these are the rules that keep it from drifting screen by screen.
/// They read the source because the mistakes they catch are the kind that look fine in isolation
/// and wrong beside the screen next door: a card indented twice, a chip that ticks where the
/// others do not.
void main() {
  final sources = Directory('lib/features')
      .listSync(recursive: true)
      .whereType<File>()
      .where((file) => file.path.endsWith('.dart'))
      .toList();

  test('a card has no margin of its own: the list around it sets the inset and the gap', () {
    expect(AppTheme.light().cardTheme.margin, EdgeInsets.zero);

    final offenders = <String>[];

    for (final file in sources) {
      final text = file.readAsStringSync();

      if (text.contains('margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4)') ||
          text.contains('margin: const EdgeInsets.only(bottom: 12)')) {
        offenders.add(file.path);
      }
    }

    expect(offenders, isEmpty, reason: 'These cards set their own margin: $offenders');
  });

  test('a filter is a FilterChip, as on every other list', () {
    final offenders = [
      for (final file in sources)
        if (file.readAsStringSync().contains('ChoiceChip')) file.path,
    ];

    expect(offenders, isEmpty, reason: 'Use FilterChip: $offenders');
  });

  test('a status is the shared StatusChip, never a plain Chip with its own colours', () {
    final plain = RegExp(r'(?<!Status)Chip\(\s*label:\s*Text\(\s*enumLabel\(l10n, EnumKind\.\w+Status');
    final offenders = [
      for (final file in sources)
        if (plain.hasMatch(file.readAsStringSync())) file.path,
    ];

    expect(offenders, isEmpty, reason: 'Use StatusChip: $offenders');
  });

  test('an empty list says so with the shared empty view, not a bare line of text', () {
    final offenders = [
      for (final file in sources)
        if (RegExp(r'Center\(child: Text\(l10n\.\w*Empty\w*\)\)').hasMatch(file.readAsStringSync())) file.path,
    ];

    expect(offenders, isEmpty, reason: 'Use EmptyView: $offenders');
  });
}
