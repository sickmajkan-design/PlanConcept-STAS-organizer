import 'package:construction_mobile/core/validation/validators.dart';
import 'package:construction_mobile/l10n/app_localizations_en.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final l10n = AppLocalizationsEn();

  group('Validators.email', () {
    test('accepts a well-formed address', () {
      expect(Validators.email('ivan.horvat@example.com', l10n), isNull);
    });

    test('trims surrounding whitespace before validating', () {
      expect(Validators.email('  ivan@example.com  ', l10n), isNull);
    });

    test('rejects an empty value', () {
      expect(Validators.email('', l10n), 'Email is required.');
      expect(Validators.email(null, l10n), 'Email is required.');
    });

    test('rejects an address without a domain', () {
      expect(Validators.email('ivan@', l10n), 'Enter a valid email address.');
      expect(Validators.email('ivan', l10n), 'Enter a valid email address.');
    });
  });

  group('Validators.strongPassword', () {
    test('accepts a password meeting the API policy', () {
      expect(Validators.strongPassword('Gradnja123', l10n), isNull);
    });

    test('rejects passwords shorter than eight characters', () {
      expect(
        Validators.strongPassword('Ab1cdef', l10n),
        'Password must be at least 8 characters long.',
      );
    });

    test('requires an upper-case letter', () {
      expect(
        Validators.strongPassword('gradnja123', l10n),
        'Password must contain an upper-case letter.',
      );
    });

    test('requires a lower-case letter', () {
      expect(
        Validators.strongPassword('GRADNJA123', l10n),
        'Password must contain a lower-case letter.',
      );
    });

    test('requires a digit', () {
      expect(
        Validators.strongPassword('GradnjaTest', l10n),
        'Password must contain a digit.',
      );
    });
  });

  group('Validators.notEmpty', () {
    test('accepts a non-blank value', () {
      expect(Validators.notEmpty('value', 'Field', l10n), isNull);
    });

    test('rejects whitespace only', () {
      expect(
        Validators.notEmpty('   ', 'Password', l10n),
        'Password is required.',
      );
    });
  });
}
