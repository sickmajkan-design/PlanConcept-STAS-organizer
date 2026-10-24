import 'package:construction_mobile/core/utils/iso_week.dart';
import 'package:flutter_test/flutter_test.dart';

/// A week's hours go into the monthly sheet of the month they fall in, so the form has to know whether
/// a week runs over a month's end.
void main() {
  test('a week inside one month falls in that month only', () {
    // KW42 of 2026 is Monday 12 to Sunday 18 October.
    const week = IsoWeek(isoYear: 2026, isoWeek: 42);

    expect(week.monday, DateTime.utc(2026, 10, 12));
    expect(week.months.first, DateTime.utc(2026, 10));
    expect(week.months.second, isNull);
  });

  test('a week that runs over a month end falls in both months', () {
    // KW40 of 2026 is Monday 28 September to Sunday 4 October.
    const week = IsoWeek(isoYear: 2026, isoWeek: 40);

    expect(week.monday, DateTime.utc(2026, 9, 28));
    expect(week.months.first, DateTime.utc(2026, 9));
    expect(week.months.second, DateTime.utc(2026, 10));
  });

  test('a week that runs over the end of the year falls in December and January', () {
    // KW53 of 2026 is Monday 28 December to Sunday 3 January 2027.
    const week = IsoWeek(isoYear: 2026, isoWeek: 53);

    expect(week.months.first, DateTime.utc(2026, 12));
    expect(week.months.second, DateTime.utc(2027, 1));
  });

  test('the first week of the year may begin in the old year', () {
    // KW01 of 2026 is Monday 29 December 2025 to Sunday 4 January 2026.
    const week = IsoWeek(isoYear: 2026, isoWeek: 1);

    expect(week.monday, DateTime.utc(2025, 12, 29));
    expect(week.months.first, DateTime.utc(2025, 12));
    expect(week.months.second, DateTime.utc(2026, 1));
  });
}
