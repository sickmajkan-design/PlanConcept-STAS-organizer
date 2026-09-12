import '../../l10n/app_localizations.dart';

/// Client-side form validation. The password rule intentionally mirrors the
/// API's policy so the user gets instant feedback instead of a round trip.
class Validators {
  const Validators._();

  static final RegExp _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static String? email(String? value, AppLocalizations l10n) {
    final input = value?.trim() ?? '';

    if (input.isEmpty) {
      return l10n.validationEmailRequired;
    }

    if (!_emailPattern.hasMatch(input)) {
      return l10n.validationEmailInvalid;
    }

    return null;
  }

  static String? notEmpty(String? value, String fieldLabel, AppLocalizations l10n) {
    if ((value ?? '').trim().isEmpty) {
      return l10n.validationFieldRequired(fieldLabel);
    }

    return null;
  }

  static String? strongPassword(String? value, AppLocalizations l10n) {
    final input = value ?? '';

    if (input.isEmpty) {
      return l10n.validationPasswordRequired;
    }

    if (input.length < 8) {
      return l10n.validationPasswordMinLength;
    }

    if (!input.contains(RegExp('[A-Z]'))) {
      return l10n.validationPasswordUpper;
    }

    if (!input.contains(RegExp('[a-z]'))) {
      return l10n.validationPasswordLower;
    }

    if (!input.contains(RegExp('[0-9]'))) {
      return l10n.validationPasswordDigit;
    }

    return null;
  }
}
