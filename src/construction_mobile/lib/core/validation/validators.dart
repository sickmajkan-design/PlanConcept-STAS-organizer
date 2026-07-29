/// Client-side form validation. The password rule intentionally mirrors the
/// API's policy so the user gets instant feedback instead of a round trip.
class Validators {
  const Validators._();

  static final RegExp _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static String? email(String? value) {
    final input = value?.trim() ?? '';

    if (input.isEmpty) {
      return 'Email is required.';
    }

    if (!_emailPattern.hasMatch(input)) {
      return 'Enter a valid email address.';
    }

    return null;
  }

  static String? notEmpty(String? value, String fieldLabel) {
    if ((value ?? '').trim().isEmpty) {
      return '$fieldLabel is required.';
    }

    return null;
  }

  static String? strongPassword(String? value) {
    final input = value ?? '';

    if (input.isEmpty) {
      return 'Password is required.';
    }

    if (input.length < 8) {
      return 'Password must be at least 8 characters long.';
    }

    if (!input.contains(RegExp('[A-Z]'))) {
      return 'Password must contain an upper-case letter.';
    }

    if (!input.contains(RegExp('[a-z]'))) {
      return 'Password must contain a lower-case letter.';
    }

    if (!input.contains(RegExp('[0-9]'))) {
      return 'Password must contain a digit.';
    }

    return null;
  }
}
