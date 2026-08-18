import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/features/work_items/data/models/work_item.dart';
import 'package:construction_mobile/features/work_items/data/work_item_repository.dart';
import 'package:construction_mobile/features/work_items/presentation/my_work_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Moving a task on, pressed rather than called.
///
/// The list a worker checks between jobs, and the buttons underneath each row
/// are the only way anything on it ever changes state. What the phone offers
/// is deliberately not the whole workflow: closing an item is a supervisor's
/// call, so `Resolved` is as far as a handset goes.

class _FakeWorkItems implements WorkItemRepository {
  _FakeWorkItems({required this.items, this.refuseWith});

  List<WorkItem> items;
  ApiException? refuseWith;

  final List<({String id, String status})> moves =
      <({String id, String status})>[];

  /// What the last fetch was asked for, so the finished-work chip can be
  /// checked rather than assumed.
  bool? lastOpenOnly;

  @override
  Future<PagedList<WorkItem>> fetchMine({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool openOnly = true,
  }) async {
    lastOpenOnly = openOnly;

    return PagedList<WorkItem>(
      items: items,
      pageNumber: 1,
      pageSize: pageSize,
      totalCount: items.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }

  @override
  Future<WorkItem> changeStatus(String id, String status) async {
    moves.add((id: id, status: status));

    if (refuseWith != null) {
      throw refuseWith!;
    }

    items = items
        .map((item) => item.id == id ? item.copyWith(status: status) : item)
        .toList();

    return items.firstWhere((item) => item.id == id);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

WorkItem _item({String status = 'Open'}) => WorkItem(
      id: '019fae10-0000-7000-8000-000000000004',
      kind: 'Defect',
      title: 'Crack in the retaining wall',
      priority: 'Normal',
      status: status,
      createdAt: DateTime.now().toUtc(),
    );

Future<void> _pumpMyWork(WidgetTester tester, _FakeWorkItems repository) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        workItemRepositoryProvider.overrideWithValue(repository),
      ],
      child: const MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        locale: Locale('en'),
        home: MyWorkScreen(),
      ),
    ),
  );

  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

void main() {
  testWidgets('an open item offers the two moves a phone may make',
      (tester) async {
    await _pumpMyWork(tester, _FakeWorkItems(items: [_item()]));

    expect(find.text('Crack in the retaining wall'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'In progress'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Done, to check'), findsOneWidget);

    // Closing belongs to a supervisor, so it is not on the handset at all.
    expect(find.widgetWithText(OutlinedButton, 'Closed'), findsNothing);
  });

  testWidgets('pressing a move sends it and the row follows', (tester) async {
    final repository = _FakeWorkItems(items: [_item()]);

    await _pumpMyWork(tester, repository);

    await tester.tap(find.widgetWithText(OutlinedButton, 'In progress'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(repository.moves, hasLength(1));
    expect(repository.moves.single.status, 'InProgress');

    // The list reloads after a move, so the row cannot disagree with the
    // server about where the item now is.
    expect(find.widgetWithText(OutlinedButton, 'Open'), findsOneWidget);
  });

  testWidgets('a refused move says why and leaves the row where it was',
      (tester) async {
    final repository = _FakeWorkItems(
      items: [_item()],
      refuseWith: ApiException('offline', kind: ApiFailureKind.offline),
    );

    await _pumpMyWork(tester, repository);

    await tester.tap(find.widgetWithText(OutlinedButton, 'In progress'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(
      find.text(
        'No connection to the server. Check your network and try again.',
      ),
      findsOneWidget,
    );
    expect(
      find.widgetWithText(OutlinedButton, 'In progress'),
      findsOneWidget,
      reason: 'the move can be made again once there is signal',
    );
  });

  testWidgets('the list asks for unfinished work until the chip widens it',
      (tester) async {
    final repository = _FakeWorkItems(items: [_item()]);

    await _pumpMyWork(tester, repository);

    expect(repository.lastOpenOnly, isTrue);

    await tester.tap(find.widgetWithText(FilterChip, 'Include finished'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(repository.lastOpenOnly, isFalse);
  });

  testWidgets('an empty list says so rather than showing nothing',
      (tester) async {
    await _pumpMyWork(tester, _FakeWorkItems(items: const []));

    expect(find.text('Nothing on your list.'), findsOneWidget);
  });
}
