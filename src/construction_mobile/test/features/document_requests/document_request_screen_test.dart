import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/document_requests/data/document_request_repository.dart';
import 'package:construction_mobile/features/document_requests/presentation/document_request_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeRepository extends DocumentRequestRepository {
  _FakeRepository(this.request) : super(Dio());

  final DocumentRequest request;

  @override
  Future<DocumentRequest> fetchOne(String id) async => request;

  @override
  Future<List<DocumentRequest>> fetchMine() async => [request];
}

Future<void> _pump(WidgetTester tester, DocumentRequest request) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        documentRequestRepositoryProvider.overrideWithValue(_FakeRepository(request)),
        currentUserProvider.overrideWithValue(
          User(id: 'me', email: 'me@example.test', role: 'Worker'),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: DocumentRequestScreen(requestId: request.id),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

DocumentRequest _request(String status, {String? reason, DateTime? due}) => DocumentRequest(
      id: 'r1',
      category: 'MedicalCheck',
      title: 'Ljekarski pregled',
      note: 'do kraja mjeseca',
      dueDate: due,
      status: status,
      attempts: status == 'Open' ? 0 : 1,
      returnReason: reason,
      events: [
        RequestEvent(type: 'Created', at: DateTime.utc(2026, 10, 15, 9), byEmployee: false),
        if (reason != null)
          RequestEvent(
            type: 'Returned',
            at: DateTime.utc(2026, 10, 16, 9),
            byEmployee: false,
            reason: reason,
          ),
      ],
    );

void main() {
  testWidgets('an open request offers the three ways to answer and cannot be sent empty', (tester) async {
    await _pump(tester, _request('Open'));

    expect(find.text('Ljekarski pregled'), findsOneWidget);
    expect(find.text('Waiting for you'), findsOneWidget);
    expect(find.text('Take a photo'), findsOneWidget);
    expect(find.text('From gallery'), findsOneWidget);
    expect(find.text('Choose a file'), findsOneWidget);

    final send = tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Send'));
    expect(send.onPressed, isNull);
  });

  testWidgets('a returned request shows why, and asks for a new document', (tester) async {
    await _pump(tester, _request('Returned', reason: 'Slika je mutna'));

    expect(find.text('Sent back, please send again'), findsOneWidget);
    expect(find.text('Why it was sent back'), findsOneWidget);
    expect(find.text('Slika je mutna'), findsWidgets);
    expect(find.text('Send a new document'), findsOneWidget);
    expect(find.text('Take a photo'), findsOneWidget);
  });

  testWidgets('a request waiting to be checked cannot be answered again', (tester) async {
    await _pump(tester, _request('Submitted'));

    expect(find.text('Sent, waiting to be checked'), findsOneWidget);
    expect(find.text('Take a photo'), findsNothing);
    expect(find.text('Send'), findsNothing);
  });

  testWidgets('an accepted request is closed', (tester) async {
    await _pump(tester, _request('Accepted'));

    expect(find.text('Accepted'), findsWidgets);
    expect(find.text('Take a photo'), findsNothing);
  });

  testWidgets('a request past its date is marked overdue', (tester) async {
    await _pump(tester, _request('Open', due: DateTime(2020, 1, 1)));

    expect(find.text('Overdue'), findsOneWidget);
  });

  test('a response without optional parts parses', () {
    final parsed = DocumentRequest.fromJson(const {'id': 'x', 'status': 'Open'});

    expect(parsed.canBeAnswered, isTrue);
    expect(parsed.events, isEmpty);
  });
}
