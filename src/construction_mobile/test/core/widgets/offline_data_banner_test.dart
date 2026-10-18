import 'package:construction_mobile/core/l10n/app_locales.dart';
import 'package:construction_mobile/core/network/offline_data_status.dart';
import 'package:construction_mobile/core/time/company_time.dart';
import 'package:construction_mobile/core/utils/formatting.dart';
import 'package:construction_mobile/core/widgets/offline_data_banner.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FixedOfflineData extends OfflineDataNotifier {
  _FixedOfflineData(this.value);

  final DateTime? value;

  @override
  DateTime? build() => value;
}

Future<void> _pump(
  WidgetTester tester, {
  required DateTime? savedAt,
  required Locale locale,
}) {
  return tester.pumpWidget(
    ProviderScope(
      overrides: [
        offlineDataProvider.overrideWith(() => _FixedOfflineData(savedAt)),
      ],
      child: MaterialApp(
        locale: locale,
        supportedLocales: supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: const Scaffold(body: OfflineDataBanner()),
      ),
    ),
  );
}

/// The strip that admits the screen below it is a copy.
void main() {
  // Instants whose reading on the organisation's clock is 07:14: the banner shows the organisation's time, so the
  // test must not depend on the zone of the machine it runs on (CI is UTC, a developer's laptop is not).
  final today = CompanyTime.today();
  final thisMorning = CompanyTime.instantOf(DateTime(today.year, today.month, today.day, 7, 14));
  final lastWeek = CompanyTime.instantOf(DateTime(today.year, today.month, today.day - 5, 7, 14));

  testWidgets('says nothing while the data is live', (tester) async {
    await _pump(tester, savedAt: null, locale: const Locale('sr'));

    expect(find.byIcon(Icons.cloud_off), findsNothing);
    expect(find.byType(Text), findsNothing);
  });

  testWidgets('gives the time, in Serbian', (tester) async {
    await _pump(tester, savedAt: thisMorning, locale: const Locale('sr'));

    expect(find.byIcon(Icons.cloud_off), findsOneWidget);

    // The hour is the point. "Nema veze" alone leaves a foreman guessing
    // whether the crew list is from the yard this morning or from Tuesday.
    expect(find.textContaining('sačuvani u 07:14'), findsOneWidget);
  });

  testWidgets('gives the time, in English', (tester) async {
    await _pump(tester, savedAt: thisMorning, locale: const Locale('en'));

    expect(find.textContaining('saved at 07:14'), findsOneWidget);
  });

  testWidgets('adds the date once it is no longer today', (tester) async {
    await _pump(tester, savedAt: lastWeek, locale: const Locale('sr'));

    // Without the date, a five-day-old copy reads as "07:14" and passes for
    // this morning's.
    expect(find.textContaining(formatDateTime(lastWeek)), findsOneWidget);
    expect(find.textContaining('sačuvani u 07:14'), findsNothing);
  });
}
