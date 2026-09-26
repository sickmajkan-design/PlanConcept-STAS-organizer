import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/refunds/data/refund.dart';
import 'package:construction_mobile/features/refunds/data/refund_repository.dart';
import 'package:construction_mobile/features/refunds/presentation/refunds_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Money: the office decides, nobody decides their own request, and the person who asked can
/// only withdraw it. The API refuses the rest; the screen should not draw it.
class _FakeRefunds extends RefundRepository {
  _FakeRefunds(this.refunds) : super(Dio());

  final List<Refund> refunds;
  final List<(String, String)> reviews = <(String, String)>[];

  @override
  Future<PagedList<Refund>> fetch({int pageNumber = 1, int pageSize = 100}) async {
    return PagedList<Refund>(
      items: refunds,
      pageNumber: 1,
      pageSize: pageSize,
      totalCount: refunds.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }

  @override
  Future<Refund> review(String id, String status, {String? note}) async {
    reviews.add((id, status));

    return refunds.first;
  }
}

Refund _refund({String by = 'someone-else', String status = 'Requested'}) => Refund(
      id: 'r1',
      employeeName: 'Ana Novak',
      requestedByUserId: by,
      amount: 25.5,
      currency: 'EUR',
      expenseDate: DateTime.utc(2026, 9, 20),
      description: 'Radne rukavice',
      status: status,
    );

Future<_FakeRefunds> _pump(WidgetTester tester, {required String role, required Refund refund}) async {
  final fake = _FakeRefunds([refund]);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        refundRepositoryProvider.overrideWithValue(fake),
        currentUserProvider.overrideWithValue(User(id: 'me', email: 'me@example.test', role: role)),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const RefundsScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();

  return fake;
}

void main() {
  testWidgets('shows who asked, how much and why', (tester) async {
    await _pump(tester, role: 'Worker', refund: _refund());

    expect(find.text('Ana Novak'), findsOneWidget);
    expect(find.text('Radne rukavice'), findsOneWidget);
    expect(find.text('25.50 EUR'), findsOneWidget);
  });

  testWidgets('the office approves somebody else\'s request', (tester) async {
    final fake = await _pump(tester, role: 'Admin', refund: _refund());

    await tester.tap(find.text('Approve'));
    await tester.pumpAndSettle();

    expect(fake.reviews, [('r1', 'Approved')]);
  });

  testWidgets('nobody decides their own request, they can only withdraw it', (tester) async {
    await _pump(tester, role: 'Admin', refund: _refund(by: 'me'));

    expect(find.text('Approve'), findsNothing);
    expect(find.text('Withdraw'), findsOneWidget);
  });

  testWidgets('a worker cannot approve', (tester) async {
    await _pump(tester, role: 'Worker', refund: _refund());

    expect(find.text('Approve'), findsNothing);
    expect(find.text('Withdraw'), findsNothing);
  });
}
