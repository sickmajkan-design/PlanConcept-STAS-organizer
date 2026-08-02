import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/l10n/app_locales.dart';
import 'core/l10n/locale_controller.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'l10n/app_localizations.dart';

class ConstructionApp extends ConsumerWidget {
  const ConstructionApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Null means "follow the device", which is the state until someone picks
    // a language explicitly. While the stored choice is still being read the
    // app follows the device too, rather than flashing the wrong language.
    final selected = ref.watch(localeControllerProvider).value;

    return MaterialApp.router(
      title: 'Construction Organizer',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: ThemeMode.system,
      locale: selected,
      supportedLocales: supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      localeResolutionCallback: (deviceLocale, supported) =>
          resolveLocale(deviceLocale, supported),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
