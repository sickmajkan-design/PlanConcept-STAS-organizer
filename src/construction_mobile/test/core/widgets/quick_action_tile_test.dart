import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/core/widgets/quick_action_tile.dart';

void main() {
  Widget host(Widget child) => MaterialApp(
        theme: AppTheme.light(),
        home: Scaffold(body: Center(child: SizedBox(width: 90, child: child))),
      );

  testWidgets('shows its label and runs its action on a tap', (tester) async {
    var taps = 0;

    await tester.pumpWidget(
      host(QuickActionTile(icon: Icons.folder, label: 'Moji dokumenti', onTap: () => taps++)),
    );

    expect(find.text('Moji dokumenti'), findsOneWidget);

    await tester.tap(find.byType(QuickActionTile));
    expect(taps, 1);
  });

  testWidgets('does nothing while it is busy', (tester) async {
    await tester.pumpWidget(
      host(const QuickActionTile(icon: Icons.folder, label: 'Moji dokumenti', onTap: null)),
    );

    // No action to run, and the tile still draws without error.
    await tester.tap(find.byType(QuickActionTile), warnIfMissed: false);
    expect(tester.takeException(), isNull);
  });
}
