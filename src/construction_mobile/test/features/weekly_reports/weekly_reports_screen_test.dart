import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/weekly_reports/data/models/weekly_site_report.dart';
import 'package:construction_mobile/features/weekly_reports/data/weekly_report_repository.dart';
import 'package:construction_mobile/features/weekly_reports/presentation/weekly_reports_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// A foreman reports the week as workers with hours and as pictures of the signed sheet; a report the office
/// sent back says why and can be sent again.
class _FakeReports extends WeeklyReportRepository {
  _FakeReports(this.reports) : super(Dio());

  final List<WeeklySiteReport> reports;

  @override
  Future<PagedList<WeeklySiteReport>> fetchMine({int pageNumber = 1, int pageSize = 20}) async {
    return PagedList<WeeklySiteReport>(
      items: reports,
      pageNumber: 1,
      pageSize: pageSize,
      totalCount: reports.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }

  @override
  Future<List<ReportableProject>> fetchReportableProjects() async =>
      const [ReportableProject(id: 'p1', name: 'Hala B')];

  final searches = <String?>[];

  @override
  Future<List<ReportWorker>> fetchReportWorkers({String? search}) async {
    searches.add(search);

    return const [
      ReportWorker(id: 'e1', fullName: 'Adin Čengić', position: 'Monter'),
      ReportWorker(id: 'e2', fullName: 'Igor Pijetlović'),
    ];
  }
}

WeeklySiteReport _report({
  String status = 'Submitted',
  String source = 'App',
  String? reviewNote,
}) =>
    WeeklySiteReport(
      id: 'w1',
      projectId: 'p1',
      projectName: 'Hala B',
      submittedByEmployeeId: 'e9',
      submittedByEmployeeName: 'Marko Marković',
      isoYear: 2026,
      isoWeek: 42,
      type: 'SignedHours',
      quantity: 212.5,
      fileName: 'satnica.jpg',
      pageCount: 2,
      lineCount: 3,
      source: source,
      status: status,
      reviewNote: reviewNote,
      createdAt: DateTime.utc(2026, 10, 19, 8),
    );

Future<void> _pump(WidgetTester tester, List<WeeklySiteReport> reports) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        weeklyReportRepositoryProvider.overrideWithValue(_FakeReports(reports)),
        currentUserProvider.overrideWithValue(
          User(id: 'me', email: 'me@example.test', role: 'Foreman'),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const WeeklyReportsScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('a report shows how many pictures and workers it has', (tester) async {
    await _pump(tester, [_report()]);

    expect(find.text('Hala B'), findsOneWidget);
    expect(find.textContaining('2 pictures · 3 workers'), findsOneWidget);
  });

  testWidgets('a report sent back says why and offers to send it again', (tester) async {
    await _pump(tester, [_report(status: 'CorrectionRequested', reviewNote: 'Missing customer signature')]);

    expect(find.text('Needs correction'), findsOneWidget);
    expect(find.text('Sent back: Missing customer signature'), findsOneWidget);
    expect(find.text('Send again'), findsOneWidget);
  });

  testWidgets('sending it again opens the form with the site already chosen', (tester) async {
    await _pump(tester, [_report(status: 'CorrectionRequested', reviewNote: 'x')]);

    await tester.tap(find.text('Send again'));
    await tester.pumpAndSettle();

    expect(find.text('Workers and hours'), findsOneWidget);
    // The site of the report is the one shown in the form.
    expect(find.text('Hala B'), findsWidgets);
  });

  testWidgets('a report the office entered is marked as theirs', (tester) async {
    await _pump(tester, [_report(source: 'Office')]);

    expect(find.text('Entered by the office'), findsOneWidget);
  });

  testWidgets('workers are added by name and their hours are totalled', (tester) async {
    await _pump(tester, [_report()]);

    await tester.tap(find.text('Submit report'));
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.text('Add a worker'));
    await tester.tap(find.text('Add a worker'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Adin Čengić'));
    await tester.pumpAndSettle();

    final hours = find.byType(TextField).hitTestable();
    expect(find.text('Adin Čengić'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Hours'), '45');
    await tester.pump();

    expect(find.text('Total: 45 h'), findsOneWidget);
    expect(hours, findsWidgets);
  });

  testWidgets('the report cannot be sent without a picture of the sheet', (tester) async {
    await _pump(tester, [_report()]);

    await tester.tap(find.text('Submit report'));
    await tester.pumpAndSettle();

    final send = find.widgetWithText(FilledButton, 'Send');
    await tester.ensureVisible(send);
    expect(tester.widget<FilledButton>(send).onPressed, isNull);
  });
}
