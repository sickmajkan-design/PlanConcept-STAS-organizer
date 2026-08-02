import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/network_providers.dart';
import 'app_locales.dart';

/// Remembers the language the user picked, across restarts.
///
/// Stored in the same secure storage that already holds the session rather
/// than pulling in a preferences package for one string. It is not a secret,
/// but it is one key in a store the app already has open.
class LocaleController extends AsyncNotifier<Locale?> {
  static const _storageKey = 'app.locale';

  @override
  Future<Locale?> build() async {
    final stored = await ref.read(secureStorageProvider).read(key: _storageKey);

    return stored == null ? null : localeFromTag(stored);
  }

  /// Sets the language, or passes `null` to follow the device again.
  Future<void> select(Locale? locale) async {
    final storage = ref.read(secureStorageProvider);

    if (locale == null) {
      await storage.delete(key: _storageKey);
    } else {
      await storage.write(key: _storageKey, value: locale.languageCode);
    }

    state = AsyncData(locale);
  }
}

final localeControllerProvider =
    AsyncNotifierProvider<LocaleController, Locale?>(LocaleController.new);
