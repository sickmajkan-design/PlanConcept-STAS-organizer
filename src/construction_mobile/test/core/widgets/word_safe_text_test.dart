import 'package:construction_mobile/core/widgets/quick_action_tile.dart';
import 'package:construction_mobile/core/widgets/word_safe_text.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';

Future<void> _pump(WidgetTester tester, String text, double width) => tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Center(
            child: SizedBox(width: width, child: WordSafeText(text, style: const TextStyle(fontSize: 14))),
          ),
        ),
      ),
    );

void main() {
  testWidgets('a long word is set smaller instead of being broken', (tester) async {
    await _pump(tester, 'Moji dokumenti', 100);

    final paragraph = tester.renderObject<RenderParagraph>(find.text('Moji dokumenti'));
    // Two lines at most (one per word): a word split in two would make three.
    expect(paragraph.didExceedMaxLines, isFalse);
    expect(tester.widget<Text>(find.text('Moji dokumenti')).style!.fontSize, lessThan(14));
  });

  testWidgets('a label that fits keeps its size', (tester) async {
    await _pump(tester, 'Odsustva', 200);

    expect(tester.widget<Text>(find.text('Odsustva')).style!.fontSize, 14);
  });

  testWidgets('tiles in a row show every label whole and are tall enough for it', (tester) async {
    tester.view.physicalSize = const Size(1400, 1600);
    tester.view.devicePixelRatio = 2;
    addTearDown(tester.view.reset);

    const labels = ['Prijavi nedostatak', 'Moji dokumenti', 'Moj raspored', 'Odsustva'];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Padding(
            padding: const EdgeInsets.all(16),
            child: IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  for (final label in labels)
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 4),
                        child: QuickActionTile(icon: Icons.add, label: label, onTap: () {}),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );

    for (final label in labels) {
      final paragraph = tester.renderObject<RenderParagraph>(find.text(label));
      expect(paragraph.didExceedMaxLines, isFalse, reason: label);
    }
    expect(tester.takeException(), isNull);
    // Icon, gap, two lines of label and the padding.
    expect(tester.getSize(find.byType(QuickActionTile).first).height, greaterThan(80));
  });
}
