import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';

/// What replaces a widget whose `build` threw.
///
/// Flutter's own replacement is a red box full of stack trace in debug and a
/// grey rectangle in release. The grey one is the problem: it says nothing,
/// it is the same shape as a loading state, and it appears in the middle of an
/// otherwise working screen — so a worker on a roof reads it as "the app is
/// slow" and taps it for the next two minutes.
///
/// Deliberately built out of nothing: no `Material`, no `Scaffold`, no
/// `Theme`. It is inserted wherever the failure happened, which may be above
/// every one of those, and a replacement that throws while replacing is the
/// one bug with no floor under it. Same reasoning as the admin panel's root
/// fallback, for the same reason.
class CrashPanel extends StatelessWidget {
  const CrashPanel({super.key});

  @override
  Widget build(BuildContext context) {
    // The nullable lookup, not `AppLocalizations.of`: the delegate is provided
    // by `MaterialApp`, and if that is what failed then it is not there.
    final l10n = Localizations.of<AppLocalizations>(context, AppLocalizations);

    return Directionality(
      textDirection: TextDirection.ltr,
      child: ColoredBox(
        color: const Color(0xFFFAFAFA),
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: DefaultTextStyle(
              style: const TextStyle(
                color: Color(0xFF1C1C1C),
                fontSize: 15,
                height: 1.4,
                decoration: TextDecoration.none,
                fontWeight: FontWeight.normal,
              ),
              textAlign: TextAlign.center,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: l10n != null
                    ? [
                        Text(
                          l10n.crashTitle,
                          style: const TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF1C1C1C),
                            decoration: TextDecoration.none,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(l10n.crashBody),
                      ]
                    // No delegate above this point, so there is no language to
                    // read the preference in. Both, rather than guessing.
                    : const [
                        Text(
                          'Ovaj ekran ne može da se prikaže',
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF1C1C1C),
                            decoration: TextDecoration.none,
                          ),
                        ),
                        SizedBox(height: 8),
                        Text('This screen could not be displayed'),
                      ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
