import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/attachments/data/models/attachment.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/document_requests/data/document_request_repository.dart';
import 'package:construction_mobile/features/my_documents/data/my_documents_repository.dart';
import 'package:construction_mobile/features/my_documents/presentation/my_documents_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeRepository extends MyDocumentsRepository {
  _FakeRepository(this.result) : super(Dio());

  final MyDocuments result;

  @override
  Future<MyDocuments> fetch() async => result;
}

class _NoRequests extends DocumentRequestRepository {
  _NoRequests() : super(Dio());

  @override
  Future<List<DocumentRequest>> fetchMine() async => const [];
}

Future<void> _pump(WidgetTester tester, MyDocuments data) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        myDocumentsRepositoryProvider.overrideWithValue(_FakeRepository(data)),
        documentRequestRepositoryProvider.overrideWithValue(_NoRequests()),
        currentUserProvider.overrideWithValue(
          User(id: 'me', email: 'me@example.test', role: 'Worker'),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const MyDocumentsScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

Attachment _document(String id, {String? title, String? expiresAt}) => Attachment(
      id: id,
      fileName: '$id.pdf',
      contentType: 'application/pdf',
      sizeBytes: 1000,
      category: 'Certificate',
      title: title,
      expiresAt: expiresAt,
      ownerType: 'Employee',
      ownerId: 'e1',
      createdAt: DateTime.utc(2026, 10, 1),
    );

void main() {
  testWidgets('says plainly that nothing has been shared yet', (tester) async {
    await _pump(tester, const MyDocuments());

    expect(find.text('Nothing has been shared with you yet.'), findsOneWidget);
  });

  testWidgets('shows my documents by title and the vehicle with its dates and tolls', (tester) async {
    await _pump(
      tester,
      MyDocuments(
        documents: [_document('a', title: 'Zaštita na radu', expiresAt: '2030-01-01')],
        vehicles: [
          MyVehicle(
            id: 'v1',
            name: 'Ford Transit',
            registrationNumber: 'ZG-1234-AB',
            insuranceValidUntil: DateTime(2030, 1, 1),
            tolls: const [
              MyVehicleToll(type: 'Vignette', country: 'SI', paid: true),
              MyVehicleToll(type: 'Tunnel', country: 'AT', routeSegment: 'Karawanken', paid: false),
            ],
            documents: [_document('policy', title: 'Polica osiguranja')],
          ),
        ],
      ),
    );

    expect(find.text('Zaštita na radu'), findsOneWidget);
    expect(find.text('Ford Transit · ZG-1234-AB'), findsOneWidget);
    expect(find.text('Polica osiguranja'), findsOneWidget);
    expect(find.text('Not paid'), findsOneWidget);
    expect(find.textContaining('Vignette · SI'), findsOneWidget);
    expect(find.textContaining('Tunnel · AT · Karawanken'), findsOneWidget);
  });

  test('a response with no vehicles or documents parses to an empty answer', () {
    final parsed = MyDocuments.fromJson(const <String, dynamic>{});

    expect(parsed.isEmpty, isTrue);
  });
}
