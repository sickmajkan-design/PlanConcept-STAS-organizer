import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/article_orders/data/article_order.dart';
import 'package:construction_mobile/features/article_orders/data/article_order_repository.dart';
import 'package:construction_mobile/features/article_orders/presentation/article_orders_screen.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// The orders screen offers each person only the step that is theirs: the office orders and
/// sends, the person who asked presses "I received it". The API refuses the rest; the screen
/// should not draw it.
class _FakeOrders extends ArticleOrderRepository {
  _FakeOrders(this.orders) : super(Dio());

  final List<ArticleOrder> orders;
  final List<(String, String)> moves = <(String, String)>[];

  @override
  Future<PagedList<ArticleOrder>> fetch({int pageNumber = 1, int pageSize = 100}) async {
    return PagedList<ArticleOrder>(
      items: orders,
      pageNumber: 1,
      pageSize: pageSize,
      totalCount: orders.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }

  @override
  Future<ArticleOrder> setStatus(String id, String status, {String? note}) async {
    moves.add((id, status));

    return orders.first;
  }
}

ArticleOrder _order({String status = 'Requested', String by = 'someone-else'}) => ArticleOrder(
      id: 'o1',
      status: status,
      urgent: false,
      requestedByUserId: by,
      requestedByName: 'Ana Novak',
      createdAt: DateTime.utc(2026, 9, 26),
      items: const [
        ArticleOrderItem(id: 'i1', name: 'Radne cipele', quantity: 1, unit: 'par', note: 'broj 43'),
      ],
    );

User _user(String role) => User(id: 'me', email: 'me@example.test', role: role);

Future<_FakeOrders> _pump(WidgetTester tester, {required String role, required ArticleOrder order}) async {
  final fake = _FakeOrders([order]);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        articleOrderRepositoryProvider.overrideWithValue(fake),
        currentUserProvider.overrideWithValue(_user(role)),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const ArticleOrdersScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();

  return fake;
}

void main() {
  testWidgets('shows what was asked for and where it stands', (tester) async {
    await _pump(tester, role: 'Worker', order: _order());

    expect(find.text('Ana Novak'), findsOneWidget);
    expect(find.textContaining('Radne cipele'), findsOneWidget);
    expect(find.text('Requested'), findsWidgets);
  });

  testWidgets('a worker cannot order, but can withdraw their own request', (tester) async {
    await _pump(tester, role: 'Worker', order: _order(by: 'me'));

    expect(find.text('Mark as ordered'), findsNothing);
    expect(find.text('Withdraw'), findsOneWidget);
  });

  testWidgets('the office orders, and the step goes to that request', (tester) async {
    final fake = await _pump(tester, role: 'Admin', order: _order());

    await tester.tap(find.text('Mark as ordered'));
    await tester.pumpAndSettle();

    expect(fake.moves, [('o1', 'Ordered')]);
  });

  testWidgets('the person who asked confirms it arrived once it is in delivery', (tester) async {
    final fake = await _pump(tester, role: 'Worker', order: _order(status: 'InDelivery', by: 'me'));

    await tester.tap(find.text('I received it'));
    await tester.pumpAndSettle();

    expect(fake.moves, [('o1', 'Delivered')]);
  });

  testWidgets('somebody else in delivery is not theirs to confirm', (tester) async {
    await _pump(tester, role: 'Worker', order: _order(status: 'InDelivery'));

    expect(find.text('I received it'), findsNothing);
  });
}
