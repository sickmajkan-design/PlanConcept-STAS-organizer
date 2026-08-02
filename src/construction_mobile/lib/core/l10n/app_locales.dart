import 'package:flutter/widgets.dart';

import '../../l10n/app_localizations.dart';

/// The languages the app ships.
const supportedLocales = <Locale>[Locale('sr'), Locale('en')];

/// Shown in the language picker, each in its own language.
String localeName(Locale locale) => switch (locale.languageCode) {
      'sr' => 'Srpski',
      _ => 'English',
    };

Locale localeFromTag(String tag) => Locale(tag.split(RegExp('[-_]')).first);

/// Picks the language for a device whose own setting the app does not ship.
///
/// Serbian rather than English, and the same for Bosnian, Croatian and
/// Montenegrin: the people using this are on sites in the region, so English
/// would be the wrong guess far more often than the right one.
Locale resolveLocale(Locale? deviceLocale, Iterable<Locale> supported) {
  if (deviceLocale != null) {
    for (final locale in supported) {
      if (locale.languageCode == deviceLocale.languageCode) {
        return locale;
      }
    }
  }

  return const Locale('sr');
}

/// Shorthand so screens read `context.l10n.someMessage`.
extension AppLocalizationsX on BuildContext {
  AppLocalizations get l10n => AppLocalizations.of(this);
}
