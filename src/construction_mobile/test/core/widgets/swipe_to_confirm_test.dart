import 'package:construction_mobile/core/widgets/swipe_to_confirm.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Widget _host(VoidCallback onConfirmed) => MaterialApp(
      home: Scaffold(
        body: Center(
          child: SizedBox(
            width: 300,
            child: SwipeToConfirm(
              label: 'Slide',
              icon: Icons.play_arrow,
              onConfirmed: onConfirmed,
              trackColor: Colors.grey,
              fillColor: Colors.orange,
              knobColor: Colors.orange,
              knobIconColor: Colors.white,
              textColor: Colors.black,
            ),
          ),
        ),
      ),
    );

void main() {
  testWidgets('dragged to the end it confirms', (tester) async {
    var confirmed = 0;
    await tester.pumpWidget(_host(() => confirmed++));

    await tester.drag(find.byType(GestureDetector).first, const Offset(1000, 0));
    await tester.pumpAndSettle();

    expect(confirmed, 1);
  });

  testWidgets('let go early it springs back and does nothing', (tester) async {
    var confirmed = 0;
    await tester.pumpWidget(_host(() => confirmed++));

    await tester.drag(find.byType(GestureDetector).first, const Offset(80, 0));
    await tester.pumpAndSettle();

    expect(confirmed, 0);
  });

  testWidgets('a screen reader can run it with one activation', (tester) async {
    var confirmed = 0;
    final handle = tester.ensureSemantics();
    await tester.pumpWidget(_host(() => confirmed++));

    tester.semantics.tap(find.semantics.byLabel('Slide'));
    await tester.pump();

    expect(confirmed, 1);
    handle.dispose();
  });
}
