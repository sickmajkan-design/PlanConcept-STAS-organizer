import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/core/widgets/contractor_badge.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A subcontractor is marked purple and always with the word, in both languages and both themes.
void main() {
  Future<void> pump(WidgetTester tester, Locale locale, ThemeData theme) {
    return tester.pumpWidget(
      MaterialApp(
        locale: locale,
        theme: theme,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const Scaffold(body: Center(child: ContractorBadge())),
      ),
    );
  }

  testWidgets('says Kooperant in Serbian and Subcontractor in English', (tester) async {
    await pump(tester, const Locale('sr'), AppTheme.light());
    expect(find.text('Kooperant'), findsOneWidget);

    await pump(tester, const Locale('en'), AppTheme.light());
    await tester.pumpAndSettle();
    expect(find.text('Subcontractor'), findsOneWidget);
  });

  testWidgets('keeps its own light purple fill in the dark theme, so the dark text stays readable', (tester) async {
    await pump(tester, const Locale('sr'), AppTheme.dark());

    final box = tester.widget<Container>(find.byType(Container).first);
    final decoration = box.decoration! as BoxDecoration;

    expect(decoration.color, ContractorColors.soft);
  });
}
