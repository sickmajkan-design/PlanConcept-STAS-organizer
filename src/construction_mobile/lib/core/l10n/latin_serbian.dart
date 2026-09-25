import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

/// The app's own Serbian is written in Latin script, but Flutter's `sr` is Cyrillic: the date
/// picker, dialogs' stock buttons and the like came up in Cyrillic inside a Latin app. These load
/// the Latin variant (`sr_Latn`) whenever the language is Serbian, and stand aside for every
/// other language, so they go in front of the framework's own delegates.
const latinSerbianDelegates = <LocalizationsDelegate<dynamic>>[
  _LatinMaterialDelegate(),
  _LatinCupertinoDelegate(),
];

const _latin = Locale.fromSubtags(languageCode: 'sr', scriptCode: 'Latn');

class _LatinMaterialDelegate extends LocalizationsDelegate<MaterialLocalizations> {
  const _LatinMaterialDelegate();

  @override
  bool isSupported(Locale locale) => locale.languageCode == 'sr';

  @override
  Future<MaterialLocalizations> load(Locale locale) => GlobalMaterialLocalizations.delegate.load(_latin);

  @override
  bool shouldReload(covariant LocalizationsDelegate<MaterialLocalizations> old) => false;
}

class _LatinCupertinoDelegate extends LocalizationsDelegate<CupertinoLocalizations> {
  const _LatinCupertinoDelegate();

  @override
  bool isSupported(Locale locale) => locale.languageCode == 'sr';

  @override
  Future<CupertinoLocalizations> load(Locale locale) => GlobalCupertinoLocalizations.delegate.load(_latin);

  @override
  bool shouldReload(covariant LocalizationsDelegate<CupertinoLocalizations> old) => false;
}
