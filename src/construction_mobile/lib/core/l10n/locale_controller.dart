import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/data/auth_repository.dart';
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

    // Best-effort: a push notification sent before the app is next opened
    // should render in this language too, which only the server can do —
    // but nothing about picking a language in-app should fail or block on
    // this call, so a signed-out user or an offline moment is silently
    // skipped rather than surfaced as an error here.
    if (locale != null) {
      try {
        await ref.read(authRepositoryProvider).updatePreferredLanguage(locale.languageCode);
      } catch (_) {
        // Ignored — see above. The in-app language still changed.
      }
    }
  }
}

final localeControllerProvider =
    AsyncNotifierProvider<LocaleController, Locale?>(LocaleController.new);
