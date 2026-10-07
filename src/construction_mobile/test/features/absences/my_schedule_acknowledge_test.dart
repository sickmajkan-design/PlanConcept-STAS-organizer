import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/absences/data/absence_repository.dart';
import 'package:construction_mobile/features/absences/data/models/schedule.dart';
import 'package:construction_mobile/features/absences/presentation/my_schedule_screen.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// A worker is told where they are posted and can say they have seen it. Until they do, the card asks;
/// afterwards it shows that they did, and a failed confirmation leaves it asking.
class _FakeAbsences implements AbsenceRepository {
  _FakeAbsences({this.refuse = false});

  final bool refuse;
  final List<String> acknowledged = <String>[];
  String? seenAt;

  @override
  Future<Schedule> fetchSchedule({
    required DateTime from,
    required DateTime to,
  }) async {
    return Schedule(
      from: '2026-10-07',
      to: '2026-10-20',
      rows: [
        ScheduleRow(
          employeeId: 'e1',
          employeeName: 'Ivan Horvat',
          position: 'Zidar',
          assignments: [
            ScheduleAssignment(
              id: 'p1',
              projectId: 's1',
              projectName: 'Hala B, Zagreb',
              from: '2026-10-07',
              to: '2026-10-20',
              acknowledgedAt: seenAt,
            ),
          ],
        ),
      ],
    );
  }

  @override
  Future<void> acknowledgePosting(String postingId) async {
    if (refuse) {
      throw ApiException('offline', kind: ApiFailureKind.offline);
    }

    acknowledged.add(postingId);
    seenAt = '2026-10-07T08:00:00Z';
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

const _worker = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'ivan@construction.local',
  role: 'Worker',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
);

Future<void> _pump(WidgetTester tester, _FakeAbsences repository) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        absenceRepositoryProvider.overrideWithValue(repository),
        currentUserProvider.overrideWithValue(_worker),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        locale: const Locale('en'),
        home: const MyScheduleScreen(),
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

void main() {
  testWidgets('an unseen posting asks for confirmation, and shows it once given',
      (tester) async {
    final repository = _FakeAbsences();

    await _pump(tester, repository);

    expect(find.text('Hala B, Zagreb'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Confirm'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Confirm'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 200));

    expect(repository.acknowledged, ['p1']);
    expect(find.widgetWithText(FilledButton, 'Confirm'), findsNothing);
    expect(find.text('Confirmed'), findsOneWidget);
  });

  testWidgets('an already confirmed posting shows no button', (tester) async {
    final repository = _FakeAbsences()..seenAt = '2026-10-06T08:00:00Z';

    await _pump(tester, repository);

    expect(find.text('Confirmed'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Confirm'), findsNothing);
  });

  testWidgets('a failed confirmation says so and leaves the button to try again',
      (tester) async {
    final repository = _FakeAbsences(refuse: true);

    await _pump(tester, repository);

    await tester.tap(find.widgetWithText(FilledButton, 'Confirm'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 200));

    expect(find.text('Could not confirm. Check the connection and try again.'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Confirm'), findsOneWidget);
  });
}
