import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/l10n/app_locales.dart';
import 'core/l10n/locale_controller.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'features/time_entries/presentation/shift_controller.dart';
import 'l10n/app_localizations.dart';

class ConstructionApp extends ConsumerStatefulWidget {
  const ConstructionApp({super.key});

  @override
  ConsumerState<ConstructionApp> createState() => _ConstructionAppState();
}

class _ConstructionAppState extends ConsumerState<ConstructionApp>
    with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // A clock action queued while offline only ever gets sent from
    // `ShiftController.build()` — otherwise it sits until the worker happens
    // to reopen the shift screen. Signal usually comes back while the phone
    // is still in a pocket, not at the exact moment someone looks at it, so
    // this is the one moment worth re-triggering that build on its own:
    // coming back to the app is also the moment it is worth checking again.
    if (state == AppLifecycleState.resumed) {
      ref.invalidate(shiftControllerProvider);
    }
  }

  @override
  Widget build(BuildContext context) {
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
