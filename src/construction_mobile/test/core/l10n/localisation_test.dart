import 'package:construction_mobile/core/l10n/app_locales.dart';
import 'package:construction_mobile/core/l10n/app_message.dart';
import 'package:construction_mobile/core/l10n/enum_labels.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

/// The widget tests run under the test binding's `en` locale, so nothing else
/// in the suite would notice if the Serbian side stopped loading.
void main() {
  group('locale resolution', () {
    test('keeps a language the app ships', () {
      expect(resolveLocale(const Locale('en'), supportedLocales),
          const Locale('en'));
      expect(resolveLocale(const Locale('sr'), supportedLocales),
          const Locale('sr'));
    });

    test('matches on language even when the device sends a region', () {
      expect(resolveLocale(const Locale('sr', 'RS'), supportedLocales),
          const Locale('sr'));
    });

    test('falls back to Serbian, not English', () {
      // The people using this are on sites in the region, so an unknown
      // device language is far more likely to be a neighbouring one than to
      // mean the person reads English.
      expect(resolveLocale(const Locale('de'), supportedLocales),
          const Locale('sr'));
      expect(resolveLocale(const Locale('bs'), supportedLocales),
          const Locale('sr'));
      expect(resolveLocale(null, supportedLocales), const Locale('sr'));
    });
  });

  group('Serbian messages', () {
    late AppLocalizations sr;
    late AppLocalizations en;

    setUp(() async {
      sr = await AppLocalizations.delegate.load(const Locale('sr'));
      en = await AppLocalizations.delegate.load(const Locale('en'));
    });

    test('load and differ from English', () {
      expect(sr.commonSignIn, 'Prijavi se');
      expect(sr.navEmployees, 'Zaposleni');
      expect(sr.authPassword, 'Lozinka');
      expect(sr.commonSignIn, isNot(en.commonSignIn));
    });

    test('interpolate their placeholders', () {
      expect(sr.toolCategoryLine('Bušilice'), 'Kategorija: Bušilice');
      expect(sr.locationQueued('nema mreže'), contains('nema mreže'));
    });

    test('inflect the same API value differently per entity', () {
      // The reason StatusChip has to be told which enum it is showing:
      // one English word, two Serbian ones.
      expect(enumLabel(sr, EnumKind.vehicleStatus, 'Available'), 'Slobodno');
      expect(enumLabel(sr, EnumKind.toolStatus, 'Available'), 'Slobodan');
      expect(enumLabel(en, EnumKind.vehicleStatus, 'Available'),
          enumLabel(en, EnumKind.toolStatus, 'Available'));
    });

    test('fall back readably for a value this build does not know', () {
      // A status added on the server must not blank out a screen.
      expect(enumLabel(sr, EnumKind.toolStatus, 'SentForCalibration'),
          'Sent For Calibration');
    });

    test('inflect the queue count through all three Serbian plural forms', () {
      // English has two forms and Serbian has three, so a count glued to a
      // noun reads as broken Serbian for most numbers. These are the
      // boundaries that catch it: 21 takes the same form as 1, and 5 does
      // not take the same form as 2.
      expect(sr.locationPending(1), contains('očitavanje čeka'));
      expect(sr.locationPending(21), contains('očitavanje čeka'));
      expect(sr.locationPending(3), contains('očitavanja čekaju'));
      expect(sr.locationPending(22), contains('očitavanja čekaju'));
      expect(sr.locationPending(5), contains('očitavanja čeka'));
      expect(sr.locationPending(0), 'Sve je poslato');
    });

    test('resolve controller messages in the chosen language', () {
      expect(AppMessage.locationNoFix.resolve(sr), sr.locationNoFix);
      expect(AppMessage.locationNoFix.resolve(en), en.locationNoFix);
      expect(AppMessage.locationNoFix.resolve(sr),
          isNot(AppMessage.locationNoFix.resolve(en)));
    });
  });
}
