import 'package:construction_mobile/core/l10n/latin_serbian.dart';
import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/notifications/presentation/notification_text.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The app's Serbian is Latin script; the framework's `sr` is Cyrillic. The date picker came up in
/// Cyrillic ("Изаберите период") inside an app written in Latin.
Future<MaterialLocalizations> _materialFor(WidgetTester tester, Locale locale) async {
  late MaterialLocalizations found;

  await tester.pumpWidget(
    MaterialApp(
      locale: locale,
      supportedLocales: const [Locale('sr'), Locale('en')],
      localizationsDelegates: const [
        ...latinSerbianDelegates,
        ...AppLocalizations.localizationsDelegates,
      ],
      home: Builder(
        builder: (context) {
          found = MaterialLocalizations.of(context);
          return const SizedBox();
        },
      ),
    ),
  );
  await tester.pumpAndSettle();

  return found;
}

void main() {
  testWidgets('the platform widgets are in Latin script when the language is Serbian', (tester) async {
    final serbian = await _materialFor(tester, const Locale('sr'));

    expect(serbian.cancelButtonLabel, 'Otkaži');
    expect(serbian.okButtonLabel, isNot(contains(RegExp('[Ѐ-ӿ]'))));
  });

  testWidgets('English is left alone', (tester) async {
    final english = await _materialFor(tester, const Locale('en'));

    expect(english.cancelButtonLabel, 'Cancel');
  });

  test('the bulletin heading is the app\'s own words, the notice stays as it was typed', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(
      l10n,
      AppNotification(
        id: '1',
        type: 'BulletinPosted',
        title: 'New notice on the bulletin board',
        body: 'Sastanak u ponedjeljak u 7:00',
        createdAt: DateTime.utc(2026, 9, 25),
      ),
    );

    expect(text.title, 'Nova objava na oglasnoj ploči');
    expect(text.body, 'Sastanak u ponedjeljak u 7:00');
  });

  test('a request for leave does not end in two full stops', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(
      l10n,
      AppNotification(
        id: '2',
        type: 'AbsenceRequested',
        title: 'x',
        body: 'x',
        dataJson: '{"employeeName":"Ana","startDate":"2026-10-12","endDate":"2026-10-13"}',
        createdAt: DateTime.utc(2026, 9, 25),
      ),
    );

    expect(text.body, isNot(endsWith('..')));
    expect(text.body, endsWith('13.10.2026.'));
  });
}
