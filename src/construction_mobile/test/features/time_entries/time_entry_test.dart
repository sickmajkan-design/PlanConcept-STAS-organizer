import 'package:construction_mobile/core/l10n/enum_labels.dart';
import 'package:construction_mobile/features/time_entries/data/models/time_entry.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

TimeEntry entry({
  DateTime? startedAt,
  DateTime? endedAt,
  int breakMinutes = 0,
  int? workedMinutes,
  String status = 'Submitted',
}) {
  final start = startedAt ?? DateTime.utc(2026, 8, 3, 7);

  return TimeEntry(
    id: 'e1',
    employeeId: 'emp1',
    employeeName: 'Ana Anić',
    startedAt: start,
    endedAt: endedAt,
    breakMinutes: breakMinutes,
    workedMinutes: workedMinutes,
    workType: 'Regular',
    status: status,
    createdAt: start,
  );
}

void main() {
  group('running shift', () {
    test('is one with no end', () {
      expect(entry(status: 'InProgress').isRunning, isTrue);
      expect(
        entry(endedAt: DateTime.utc(2026, 8, 3, 15)).isRunning,
        isFalse,
      );
    });

    test('elapsed time grows against the given now', () {
      final shift = entry(startedAt: DateTime.utc(2026, 8, 3, 7));

      expect(
        shift.elapsedAt(DateTime.utc(2026, 8, 3, 10, 30)),
        const Duration(hours: 3, minutes: 30),
      );
    });

    test('elapsed time never runs backwards', () {
      // A phone whose clock is behind the server's would otherwise show a
      // negative shift, which reads as a bug to the person holding it.
      final shift = entry(startedAt: DateTime.utc(2026, 8, 3, 7));

      expect(
        shift.elapsedAt(DateTime.utc(2026, 8, 3, 6)),
        Duration.zero,
      );
    });

    test('a finished shift stops counting at its end', () {
      final shift = entry(
        startedAt: DateTime.utc(2026, 8, 3, 7),
        endedAt: DateTime.utc(2026, 8, 3, 15),
      );

      expect(
        shift.elapsedAt(DateTime.utc(2026, 8, 3, 20)),
        const Duration(hours: 8),
      );
    });
  });

  group('locking', () {
    test('only an approved entry is locked', () {
      expect(entry(status: 'Approved').isLocked, isTrue);
      expect(entry(status: 'Submitted').isLocked, isFalse);
      expect(entry(status: 'Rejected').isLocked, isFalse);
    });
  });

  group('json', () {
    test('reads the API shape, including a running shift', () {
      final decoded = TimeEntry.fromJson(<String, dynamic>{
        'id': 'e1',
        'employeeId': 'emp1',
        'employeeName': 'Ana Anić',
        'projectId': null,
        'projectName': null,
        'startedAt': '2026-08-03T07:00:00Z',
        'endedAt': null,
        'breakMinutes': 0,
        'workedMinutes': null,
        'workType': 'Regular',
        'status': 'InProgress',
        'createdAt': '2026-08-03T07:00:00Z',
      });

      expect(decoded.isRunning, isTrue);
      expect(decoded.workedMinutes, isNull);
      expect(decoded.startedAt.toUtc(), DateTime.utc(2026, 8, 3, 7));
    });
  });

  group('Serbian labels', () {
    late AppLocalizations sr;
    late AppLocalizations en;

    setUp(() async {
      sr = await AppLocalizations.delegate.load(const Locale('sr'));
      en = await AppLocalizations.delegate.load(const Locale('en'));
    });

    test('translate the statuses and work types', () {
      expect(
        enumLabel(sr, EnumKind.timeEntryStatus, 'Submitted'),
        'Čeka pregled',
      );
      expect(enumLabel(sr, EnumKind.workType, 'Overtime'), 'Prekovremeni');
      expect(
        enumLabel(sr, EnumKind.timeEntryStatus, 'Approved'),
        isNot(enumLabel(en, EnumKind.timeEntryStatus, 'Approved')),
      );
    });

    test('fall back readably for a status this build does not know', () {
      expect(
        enumLabel(sr, EnumKind.timeEntryStatus, 'AwaitingPayroll'),
        'Awaiting Payroll',
      );
    });

    test('inflect the break through all three Serbian plural forms', () {
      expect(sr.shiftBreakMinutes(0), 'Bez pauze');
      expect(sr.shiftBreakMinutes(1), '1 minut');
      expect(sr.shiftBreakMinutes(3), '3 minuta');
      expect(sr.shiftBreakMinutes(21), '21 minut');
      expect(sr.shiftBreakMinutes(30), '30 minuta');
    });
  });
}
