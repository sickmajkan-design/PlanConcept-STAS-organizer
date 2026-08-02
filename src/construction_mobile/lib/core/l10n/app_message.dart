import '../../l10n/app_localizations.dart';

/// A message a controller wants to show, named rather than written out.
///
/// Controllers run without a `BuildContext`, so they cannot translate anything
/// themselves. Storing the meaning and resolving it in the widget also means a
/// message already sitting in state re-reads correctly when the language
/// changes, instead of keeping the language it was created in.
enum AppMessage {
  locationNoFix,
  locationReadFailed,
  notificationsDisabled,
  notificationsTokenFailed,
  notificationsNotConfigured,
  notificationsFirebaseFailed;

  String resolve(AppLocalizations l10n) => switch (this) {
        AppMessage.locationNoFix => l10n.locationNoFix,
        AppMessage.locationReadFailed => l10n.locationReadFailed,
        AppMessage.notificationsDisabled => l10n.notificationsDisabled,
        AppMessage.notificationsTokenFailed => l10n.notificationsTokenFailed,
        AppMessage.notificationsNotConfigured => l10n.notificationsNotConfigured,
        AppMessage.notificationsFirebaseFailed => l10n.notificationsFirebaseFailed,
      };
}
