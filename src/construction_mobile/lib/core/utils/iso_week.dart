/// ISO-8601 week numbering (Monday-start, week 1 is the week containing the
/// year's first Thursday) — the "KW" convention the German/Austrian-market
/// paperwork the weekly site report feature mirrors already uses.
class IsoWeek {
  const IsoWeek({required this.isoYear, required this.isoWeek});

  final int isoYear;
  final int isoWeek;

  static IsoWeek of(DateTime date) {
    final utc = DateTime.utc(date.year, date.month, date.day);
    // Shift to the Thursday of this date's week: DateTime.weekday is
    // already 1 (Mon) .. 7 (Sun), so no wraparound trick is needed here.
    final thursday = utc.add(Duration(days: 3 - (utc.weekday - 1)));

    final yearStart = DateTime.utc(thursday.year, 1, 1);
    final isoWeek =
        ((thursday.difference(yearStart).inDays + 1) / 7).ceil();

    return IsoWeek(isoYear: thursday.year, isoWeek: isoWeek);
  }

  /// The most recently completed ISO week — what a Monday submission is
  /// normally reporting on.
  static IsoWeek lastCompleted([DateTime? today]) {
    final reference = today ?? DateTime.now();
    return IsoWeek.of(reference.subtract(const Duration(days: 7)));
  }

  String get label => 'KW${isoWeek.toString().padLeft(2, '0')}/$isoYear';
}
