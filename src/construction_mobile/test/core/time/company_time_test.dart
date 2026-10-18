import 'package:flutter_test/flutter_test.dart';

import 'package:construction_mobile/core/time/company_time.dart';
import 'package:construction_mobile/core/utils/formatting.dart';

/// The organisation's calendar: one zone, chosen in the company settings, that decides what day it is, what day a shift
/// belongs to and what time it reads, whatever zone the phone itself is set to.
void main() {
  tearDown(() {
    CompanyTime.setZone(CompanyTime.defaultZoneId);
  });

  group('the zone', () {
    test('is Belgrade until the organisation has chosen another', () {
      expect(CompanyTime.zoneId, 'Europe/Belgrade');
    });

    test('is taken up when it is a real zone and ignored when it is not', () {
      expect(CompanyTime.setZone('America/New_York'), isTrue);
      expect(CompanyTime.zoneId, 'America/New_York');

      expect(CompanyTime.setZone('Mars/Olympus'), isFalse);
      expect(CompanyTime.setZone(null), isFalse);
      expect(CompanyTime.setZone('America/New_York'), isFalse);
      expect(CompanyTime.zoneId, 'America/New_York');
    });
  });

  group('the clock reading', () {
    // 22:30 UTC on 18 October 2026 is 00:30 on the 19th in Belgrade (UTC+2), and 18:30 on the 18th in New York.
    final instant = DateTime.utc(2026, 10, 18, 22, 30);

    test('puts the first hours after midnight on the new day', () {
      final wall = CompanyTime.wallClock(instant);

      expect([wall.year, wall.month, wall.day, wall.hour, wall.minute], [2026, 10, 19, 0, 30]);
    });

    test('is the organisation\'s, not the phone\'s', () {
      CompanyTime.setZone('America/New_York');
      final wall = CompanyTime.wallClock(instant);

      expect([wall.day, wall.hour, wall.minute], [18, 18, 30]);
    });

    test('turns back into the instant it came from, in every zone, either side of the hour the clocks repeat', () {
      for (final zone in ['Europe/Belgrade', 'America/New_York', 'Asia/Tokyo', 'Australia/Sydney', 'UTC']) {
        CompanyTime.setZone(zone);

        for (final iso in ['2026-01-15T12:00:00Z', '2026-03-29T00:30:00Z', '2026-07-04T23:00:00Z', '2026-10-24T22:30:00Z', '2026-10-25T02:30:00Z', '2026-11-01T08:30:00Z']) {
          final at = DateTime.parse(iso);

          expect(CompanyTime.instantOf(CompanyTime.wallClock(at)), at, reason: '$zone $iso');
        }
      }
    });
  });

  group('what is shown', () {
    final instant = DateTime.utc(2026, 10, 18, 22, 30);

    test('writes an instant as the day and time on the organisation\'s clock', () {
      expect(formatDate(instant), '19.10.2026.');
      expect(formatTime(instant), '00:30');
      expect(formatDateTime(instant), '19.10.2026. 00:30');

      CompanyTime.setZone('America/New_York');
      expect(formatDate(instant), '18.10.2026.');
      expect(formatTime(instant), '18:30');
    });

    test('writes a date as it is: a date is not a moment', () {
      CompanyTime.setZone('America/Los_Angeles');

      expect(formatDate(DateTime(2026, 10, 19)), '19.10.2026.');
    });
  });
}
