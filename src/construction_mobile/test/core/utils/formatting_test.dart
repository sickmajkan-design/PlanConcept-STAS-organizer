import 'package:construction_mobile/core/utils/formatting.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('humanizeEnum', () {
    test('splits PascalCase API enum names', () {
      expect(humanizeEnum('ProjectManager'), 'Project Manager');
      expect(humanizeEnum('OnLeave'), 'On Leave');
      expect(humanizeEnum('OutOfService'), 'Out Of Service');
    });

    test('leaves single words alone', () {
      expect(humanizeEnum('Active'), 'Active');
    });
  });

  group('initialsOf', () {
    test('uses both names when available', () {
      expect(initialsOf('Ivan', 'Horvat'), 'IH');
    });

    test('falls back to a single name', () {
      expect(initialsOf('Ivan', ''), 'I');
      expect(initialsOf(null, 'Horvat'), 'H');
    });

    test('falls back to the email when there is no name', () {
      expect(initialsOf(null, null, fallback: 'admin@example.com'), 'A');
    });

    test('never returns an empty label', () {
      expect(initialsOf(null, null), '?');
    });
  });

  group('formatDate', () {
    test('renders day-first', () {
      expect(formatDate(DateTime(2026, 3, 1)), '01.03.2026.');
    });

    test('shows a dash for a missing date', () {
      expect(formatDate(null), '—');
    });
  });

  group('formatRelative', () {
    test('describes recent instants', () {
      final now = DateTime.now().toUtc();

      expect(formatRelative(now), 'just now');
      expect(
        formatRelative(now.subtract(const Duration(minutes: 5))),
        '5 min ago',
      );
      expect(formatRelative(now.subtract(const Duration(hours: 3))), '3 h ago');
      expect(formatRelative(now.subtract(const Duration(days: 2))), '2 d ago');
    });

    test('falls back to a date beyond a week', () {
      final old = DateTime.now().toUtc().subtract(const Duration(days: 30));

      expect(formatRelative(old), formatDate(old));
    });

    test('handles a never-reported value', () {
      expect(formatRelative(null), 'never');
    });
  });
}
