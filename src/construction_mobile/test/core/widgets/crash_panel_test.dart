import 'package:construction_mobile/core/l10n/app_locales.dart';
import 'package:construction_mobile/core/widgets/crash_panel.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The panel that replaces a widget whose build threw.
///
/// The thing worth testing is not how it looks but what it survives: it is
/// inserted at the point of the failure, which may be above `MaterialApp`,
/// above the theme, above the localisations delegate. A replacement that
/// itself needs any of those has no floor under it.
void main() {
  testWidgets('renders with no app, no theme and no localisations',
      (tester) async {
    await tester.pumpWidget(const CrashPanel());

    // Both languages, because with no delegate above it there is no language
    // preference to read.
    expect(find.text('Ovaj ekran ne može da se prikaže'), findsOneWidget);
    expect(find.text('This screen could not be displayed'), findsOneWidget);

    expect(tester.takeException(), isNull);
  });

  testWidgets('uses the operator\'s language when there is one', (tester) async {
    await tester.pumpWidget(MaterialApp(
      locale: const Locale('sr'),
      supportedLocales: supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: const CrashPanel(),
    ));

    final sr = await AppLocalizations.delegate.load(const Locale('sr'));

    expect(find.text(sr.crashTitle), findsOneWidget);
    expect(find.text(sr.crashBody), findsOneWidget);

    // Not the both-languages fallback: that one is for when there is nothing
    // to ask.
    expect(find.text('This screen could not be displayed'), findsNothing);
  });

  testWidgets('replaces a widget that throws, instead of a grey rectangle',
      (tester) async {
    // What Flutter actually does with `ErrorWidget.builder`, rather than a
    // test of the builder assignment.
    final previous = ErrorWidget.builder;
    ErrorWidget.builder = (_) => const CrashPanel();
    addTearDown(() => ErrorWidget.builder = previous);

    await tester.pumpWidget(MaterialApp(
      locale: const Locale('en'),
      supportedLocales: supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: Builder(builder: (_) => throw StateError('render exploded')),
    ));

    final en = await AppLocalizations.delegate.load(const Locale('en'));

    expect(find.text(en.crashTitle), findsOneWidget);

    // The error still gets reported — the panel changes what is shown, not
    // what is known.
    expect(tester.takeException(), isA<StateError>());
  });
}
