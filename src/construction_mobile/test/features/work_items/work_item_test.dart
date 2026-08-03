import 'package:construction_mobile/core/l10n/enum_labels.dart';
import 'package:construction_mobile/features/work_items/data/models/work_item.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

WorkItem item({
  String kind = 'Task',
  String status = 'Open',
  String? dueDate,
  bool isFinished = false,
}) {
  return WorkItem(
    id: 'w1',
    kind: kind,
    title: 'Popravi ogradu',
    priority: 'Normal',
    status: status,
    dueDate: dueDate,
    isFinished: isFinished,
    createdAt: DateTime.utc(2026, 8, 1),
  );
}

void main() {
  group('overdue', () {
    final today = DateTime.utc(2026, 8, 3);

    test('work with no deadline is never late', () {
      expect(item().isOverdueOn(today), isFalse);
    });

    test('yesterday is late', () {
      expect(item(dueDate: '2026-08-02').isOverdueOn(today), isTrue);
    });

    test('the deadline day itself is not yet missed', () {
      // Work due "on the 3rd" is not late during the 3rd.
      expect(item(dueDate: '2026-08-03').isOverdueOn(today), isFalse);
    });

    test('finished work is not a problem waiting to be dealt with', () {
      expect(
        item(dueDate: '2026-08-02', status: 'Closed', isFinished: true)
            .isOverdueOn(today),
        isFalse,
      );
    });

    test('a date this build cannot read is ignored rather than thrown on', () {
      final unreadable = item(dueDate: 'not-a-date');

      expect(unreadable.due, isNull);
      expect(unreadable.isOverdueOn(today), isFalse);
    });
  });

  group('the moves offered', () {
    test('open work can be started or finished', () {
      expect(item(status: 'Open').nextStates, ['InProgress', 'Resolved']);
    });

    test('resolved work can only be sent back from the phone', () {
      // Closing is the supervisor's check that it was done, so the phone does
      // not offer it — a button that always produces a 403 teaches nothing.
      expect(item(status: 'Resolved').nextStates, ['InProgress']);
    });

    test('finished work offers nothing', () {
      expect(item(status: 'Closed').nextStates, isEmpty);
      expect(item(status: 'Cancelled').nextStates, isEmpty);
    });

    test('a status this build does not know offers nothing rather than crashing',
        () {
      expect(item(status: 'AwaitingParts').nextStates, isEmpty);
    });
  });

  group('json', () {
    test('reads the API shape', () {
      final decoded = WorkItem.fromJson(<String, dynamic>{
        'id': 'w1',
        'kind': 'Defect',
        'title': 'Pukotina u zidu',
        'description': 'Kod ulaza',
        'projectId': 'p1',
        'projectName': 'Gradilište 1',
        'assignedEmployeeId': null,
        'assignedEmployeeName': null,
        'priority': 'High',
        'status': 'Open',
        'dueDate': '2026-08-10',
        'latitude': 44.8,
        'longitude': 20.4,
        'attachmentCount': 2,
        'isFinished': false,
        'createdAt': '2026-08-03T09:00:00Z',
      });

      expect(decoded.isDefect, isTrue);
      expect(decoded.attachmentCount, 2);
      expect(decoded.due, DateTime.parse('2026-08-10'));
    });

    test('defaults a missing attachment count rather than failing', () {
      final decoded = WorkItem.fromJson(<String, dynamic>{
        'id': 'w1',
        'kind': 'Task',
        'title': 'Naruči cement',
        'priority': 'Normal',
        'status': 'Open',
        'createdAt': '2026-08-03T09:00:00Z',
      });

      expect(decoded.attachmentCount, 0);
      expect(decoded.isFinished, isFalse);
    });
  });

  group('Serbian labels', () {
    late AppLocalizations sr;
    late AppLocalizations en;

    setUp(() async {
      sr = await AppLocalizations.delegate.load(const Locale('sr'));
      en = await AppLocalizations.delegate.load(const Locale('en'));
    });

    test('translate the kinds, statuses and priorities', () {
      expect(enumLabel(sr, EnumKind.workItemKind, 'Defect'), 'Nedostatak');
      expect(enumLabel(sr, EnumKind.workItemStatus, 'InProgress'), 'U toku');
      expect(enumLabel(sr, EnumKind.workItemPriority, 'Urgent'), 'Hitno');
      expect(
        enumLabel(sr, EnumKind.workItemStatus, 'Resolved'),
        isNot(enumLabel(en, EnumKind.workItemStatus, 'Resolved')),
      );
    });

    test('fall back readably for a status this build does not know', () {
      expect(
        enumLabel(sr, EnumKind.workItemStatus, 'AwaitingParts'),
        'Awaiting Parts',
      );
    });

    test('inflect the photo count through all three Serbian plural forms', () {
      expect(sr.workItemsPhotoCount(0), 'Bez fotografija');
      expect(sr.workItemsPhotoCount(1), '1 fotografija');
      expect(sr.workItemsPhotoCount(3), '3 fotografije');
      expect(sr.workItemsPhotoCount(21), '21 fotografija');
      expect(sr.workItemsPhotoCount(30), '30 fotografija');
    });
  });
}
