import 'package:construction_mobile/core/l10n/api_failure_text.dart';
import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Which words a failure gets, and in whose language.
///
/// The rule has two halves and they pull in opposite directions: a sentence
/// the server wrote is specific and must survive, a sentence this app wrote is
/// generic and must be translated. Getting the split backwards is invisible in
/// English and obvious to nobody who reads the code.
void main() {
  late AppLocalizations sr;
  late AppLocalizations en;

  setUp(() async {
    sr = await AppLocalizations.delegate.load(const Locale('sr'));
    en = await AppLocalizations.delegate.load(const Locale('en'));
  });

  group('describe', () {
    test('translates a message this app made up', () {
      final failure = ApiException(
        'No connection to the server. Check your network and try again.',
        kind: ApiFailureKind.offline,
      );

      expect(failure.describe(sr), sr.failureOffline);
      expect(failure.describe(en), en.failureOffline);

      // The stored English is still there for the log, but it is not what the
      // foreman reads.
      expect(failure.describe(sr), isNot(failure.message));
    });

    test('keeps what the server said, in the server\'s own words', () {
      final failure = ApiException(
        "Employee number 'EMP-001' is already in use.",
        statusCode: 409,
        kind: ApiFailureKind.conflict,
        isFromServer: true,
      );

      // A translated "the action conflicts with the current data" would be in
      // the right language and tell them nothing they can act on.
      expect(failure.describe(sr), "Employee number 'EMP-001' is already in use.");
    });

    test('has a sentence for every kind, in both languages', () {
      // A missing case would be a compile error; an empty or duplicated
      // translation would not be.
      for (final kind in ApiFailureKind.values) {
        expect(kind.describe(sr).trim(), isNotEmpty, reason: '$kind in sr');
        expect(kind.describe(en).trim(), isNotEmpty, reason: '$kind in en');
        expect(kind.describe(sr), isNot(kind.describe(en)), reason: '$kind');
      }
    });
  });

  group('presentation', () {
    test('offers a retry only where trying again could work', () {
      expect(ApiFailureKind.offline.isRetryable, isTrue);
      expect(ApiFailureKind.server.isRetryable, isTrue);

      // The same request will be refused again. A button that does nothing
      // twice is worse than no button.
      expect(ApiFailureKind.forbidden.isRetryable, isFalse);
      expect(ApiFailureKind.notFound.isRetryable, isFalse);
    });

    test('shows being out of signal as being out of signal', () {
      expect(ApiFailureKind.offline.icon, Icons.cloud_off);
      expect(ApiFailureKind.forbidden.icon, Icons.lock_outline);
      expect(ApiFailureKind.server.icon, Icons.error_outline);
    });
  });
}
