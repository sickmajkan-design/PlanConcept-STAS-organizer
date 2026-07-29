import 'package:construction_mobile/core/validation/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Validators.email', () {
    test('accepts a well-formed address', () {
      expect(Validators.email('ivan.horvat@example.com'), isNull);
    });

    test('trims surrounding whitespace before validating', () {
      expect(Validators.email('  ivan@example.com  '), isNull);
    });

    test('rejects an empty value', () {
      expect(Validators.email(''), 'Email is required.');
      expect(Validators.email(null), 'Email is required.');
    });

    test('rejects an address without a domain', () {
      expect(Validators.email('ivan@'), 'Enter a valid email address.');
      expect(Validators.email('ivan'), 'Enter a valid email address.');
    });
  });

  group('Validators.strongPassword', () {
    test('accepts a password meeting the API policy', () {
      expect(Validators.strongPassword('Gradnja123'), isNull);
    });

    test('rejects passwords shorter than eight characters', () {
      expect(
        Validators.strongPassword('Ab1cdef'),
        'Password must be at least 8 characters long.',
      );
    });

    test('requires an upper-case letter', () {
      expect(
        Validators.strongPassword('gradnja123'),
        'Password must contain an upper-case letter.',
      );
    });

    test('requires a lower-case letter', () {
      expect(
        Validators.strongPassword('GRADNJA123'),
        'Password must contain a lower-case letter.',
      );
    });

    test('requires a digit', () {
      expect(
        Validators.strongPassword('GradnjaTest'),
        'Password must contain a digit.',
      );
    });
  });

  group('Validators.notEmpty', () {
    test('accepts a non-blank value', () {
      expect(Validators.notEmpty('value', 'Field'), isNull);
    });

    test('rejects whitespace only', () {
      expect(Validators.notEmpty('   ', 'Password'), 'Password is required.');
    });
  });
}
