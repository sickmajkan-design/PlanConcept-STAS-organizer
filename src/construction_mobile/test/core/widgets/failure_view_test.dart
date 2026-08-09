import 'package:construction_mobile/core/l10n/app_locales.dart';
import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/widgets/failure_view.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// The screen a worker sees when a load fails.
///
/// Every list and detail screen funnels here, so its two decisions — which
/// sentence, and whether to offer a retry — are made once for the whole app.
Widget _host(Widget child, {Locale locale = const Locale('sr')}) {
  return MaterialApp(
    locale: locale,
    supportedLocales: supportedLocales,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    home: Scaffold(body: child),
  );
}

void main() {
  testWidgets('says the phone is offline, in Serbian, with a way to retry',
      (tester) async {
    var retried = 0;

    await tester.pumpWidget(_host(
      FailureView(
        error: ApiException(
          'No connection to the server. Check your network and try again.',
          kind: ApiFailureKind.offline,
        ),
        onRetry: () => retried++,
      ),
    ));

    final sr = await AppLocalizations.delegate.load(const Locale('sr'));

    expect(find.text(sr.failureOffline), findsOneWidget);
    expect(find.byIcon(Icons.cloud_off), findsOneWidget);

    await tester.tap(find.byType(OutlinedButton));
    expect(retried, 1);
  });

  testWidgets('shows the server\'s own words when the server supplied any',
      (tester) async {
    await tester.pumpWidget(_host(
      FailureView(
        error: ApiException(
          'This shift was already approved.',
          statusCode: 409,
          kind: ApiFailureKind.conflict,
          isFromServer: true,
        ),
      ),
    ));

    expect(find.text('This shift was already approved.'), findsOneWidget);
  });

  testWidgets('offers no retry for a refusal that will be refused again',
      (tester) async {
    await tester.pumpWidget(_host(
      FailureView(
        error: ApiException(
          'You do not have permission to perform this action.',
          statusCode: 403,
          kind: ApiFailureKind.forbidden,
        ),
        onRetry: () {},
      ),
    ));

    final sr = await AppLocalizations.delegate.load(const Locale('sr'));

    expect(find.text(sr.failureForbidden), findsOneWidget);
    expect(find.byIcon(Icons.lock_outline), findsOneWidget);
    expect(find.byType(OutlinedButton), findsNothing);
  });

  testWidgets('has something to say about an error that is not ours',
      (tester) async {
    // A parse failure, a null in a place there should not be one. There is
    // nothing useful to say beyond that it happened — but a blank panel says
    // even less.
    await tester.pumpWidget(_host(FailureView(error: StateError('boom'))));

    final sr = await AppLocalizations.delegate.load(const Locale('sr'));

    expect(find.text(sr.failureUnknown), findsOneWidget);

    // And not the exception's own text, which is English at best and a
    // fragment of somebody's record at worst.
    expect(find.textContaining('boom'), findsNothing);
  });

  testWidgets('follows the language, not the failure', (tester) async {
    await tester.pumpWidget(_host(
      FailureView(
        error: ApiException('whatever', kind: ApiFailureKind.offline),
      ),
      locale: const Locale('en'),
    ));

    final en = await AppLocalizations.delegate.load(const Locale('en'));

    expect(find.text(en.failureOffline), findsOneWidget);
  });
}
