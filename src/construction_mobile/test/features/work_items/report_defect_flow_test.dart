import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/core/network/offline_cache.dart';
import 'package:construction_mobile/core/outbox/outbox_queue.dart';
import 'package:construction_mobile/features/notifications/presentation/pending_acknowledgments_controller.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/outbox/outbox_controller.dart';
import 'package:construction_mobile/features/work_items/data/models/work_item.dart';
import 'package:construction_mobile/features/work_items/data/work_item_repository.dart';
import 'package:construction_mobile/features/work_items/presentation/report_defect.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';

/// Reporting a defect, pressed rather than called.
///
/// The one kind of work a Worker may raise, and the reason the app is on the
/// site at all rather than in the office: the person standing in front of the
/// crack is the one who can record it.
///
/// The photo half is not covered here. Picking one goes through
/// `ImagePicker()` constructed inline, so faking it needs the platform
/// interface rather than a provider override — the text and failure paths
/// below are what these tests hold, and the picture stays on the device
/// checklist.
class _NoPosition extends GeolocatorPlatform {
  @override
  Future<LocationPermission> checkPermission() async =>
      LocationPermission.denied;
}

class _FakeWorkItems implements WorkItemRepository {
  _FakeWorkItems({this.refuseWith});

  ApiException? refuseWith;

  /// The key each attempt carried; a retry must reuse the first one.
  final List<String?> keys = <String?>[];

  final List<({String projectId, String title, String? description})> reported =
      <({String projectId, String title, String? description})>[];

  @override
  Future<WorkItem> reportDefect({
    required String projectId,
    required String title,
    String? description,
    double? latitude,
    double? longitude,
    String? idempotencyKey,
  }) async {
    keys.add(idempotencyKey);
    reported.add((
      projectId: projectId,
      title: title,
      description: description,
    ));

    if (refuseWith != null) {
      throw refuseWith!;
    }

    return WorkItem(
      id: '019fae10-0000-7000-8000-000000000002',
      kind: 'Defect',
      title: title,
      description: description,
      projectId: projectId,
      priority: 'Normal',
      status: 'Open',
      createdAt: DateTime.now().toUtc(),
    );
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

const _projectId = '019fad80-0000-7000-8000-000000000003';

class _MemoryOutboxStore implements OutboxStore {
  String? value;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async => value = written;

  @override
  Future<void> clear() async => value = null;
}

const _worker = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'ivan@construction.local',
  role: 'Worker',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
);

Future<void> _pumpButton(
  WidgetTester tester,
  _FakeWorkItems repository, {
  _MemoryOutboxStore? store,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        workItemRepositoryProvider.overrideWithValue(repository),
        currentUserProvider.overrideWithValue(_worker),
        // A signed-in user makes the button ask whether an unconfirmed notice
        // is blocking it, which opens the API client and, with it, the offline
        // cache — a five-second wait on a directory only the platform can name.
        pendingAcknowledgmentsProvider.overrideWith((ref) async => const []),
        offlineCacheProvider.overrideWithValue(Future<OfflineCache?>.value(null)),
        outboxQueueProvider.overrideWithValue(
          OutboxQueue(store ?? _MemoryOutboxStore()),
        ),
      ],
      child: const MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        locale: Locale('en'),
        home: Scaffold(
          body: Center(child: ReportDefectButton(projectId: _projectId)),
        ),
      ),
    ),
  );
  await tester.pump();
}

Future<void> _openSheet(WidgetTester tester) async {
  await tester.tap(find.text('Report a defect'));
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 300));
}

void main() {
  setUp(() => GeolocatorPlatform.instance = _NoPosition());

  testWidgets('a defect reaches the API with what was typed', (tester) async {
    final repository = _FakeWorkItems();

    await _pumpButton(tester, repository);
    await _openSheet(tester);

    await tester.enterText(
      find.widgetWithText(TextField, 'What is wrong'),
      'Crack in the retaining wall',
    );
    await tester.enterText(
      find.widgetWithText(TextField, 'Details (optional)'),
      'Runs the full height, west side',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Report'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(repository.reported, hasLength(1));
    expect(repository.reported.single.projectId, _projectId);
    expect(repository.reported.single.title, 'Crack in the retaining wall');
    expect(
      repository.reported.single.description,
      'Runs the full height, west side',
    );
    expect(find.text('Defect reported.'), findsOneWidget);
  });

  testWidgets('an empty title is refused here rather than by the server',
      (tester) async {
    final repository = _FakeWorkItems();

    await _pumpButton(tester, repository);
    await _openSheet(tester);

    await tester.tap(find.widgetWithText(FilledButton, 'Report'));
    await tester.pump();

    expect(find.text('Describe the problem in a few words.'), findsOneWidget);
    expect(
      repository.reported,
      isEmpty,
      reason: 'and what was typed is still on screen, not lost to a round trip',
    );
  });

  /// The same shape as `C10` on the clock-out sheet, and the same reasoning.
  ///
  /// A dismissal and a Cancel both come back as null, so what would be thrown
  /// away here — a typed description and a photograph taken in front of the
  /// defect — would go without a word, in front of somebody standing on
  /// scaffolding who is not going to type it again.
  testWidgets('the sheet ignores a tap outside it', (tester) async {
    final repository = _FakeWorkItems();

    await _pumpButton(tester, repository);
    await _openSheet(tester);

    await tester.enterText(
      find.widgetWithText(TextField, 'What is wrong'),
      'Crack in the retaining wall',
    );

    await tester.tapAt(const Offset(400, 40));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.widgetWithText(FilledButton, 'Report'), findsOneWidget);
    expect(
      find.text('Crack in the retaining wall'),
      findsOneWidget,
      reason: 'what was typed is still there',
    );

    // Cancel still closes it, or the reporter would be trapped in the sheet.
    await tester.tap(find.widgetWithText(TextButton, 'Cancel'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.widgetWithText(FilledButton, 'Report'), findsNothing);
    expect(repository.reported, isEmpty);
  });

  testWidgets('a blank description is sent as nothing, not as empty',
      (tester) async {
    final repository = _FakeWorkItems();

    await _pumpButton(tester, repository);
    await _openSheet(tester);

    await tester.enterText(
      find.widgetWithText(TextField, 'What is wrong'),
      'Loose handrail',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Report'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(repository.reported.single.description, isNull);
  });

  testWidgets('a refused report says why and is not queued', (tester) async {
    final repository = _FakeWorkItems(
      refuseWith: ApiException(
        'nope',
        statusCode: 403,
        kind: ApiFailureKind.forbidden,
      ),
    );
    final store = _MemoryOutboxStore();

    await _pumpButton(tester, repository, store: store);
    await _openSheet(tester);

    await tester.enterText(
      find.widgetWithText(TextField, 'What is wrong'),
      'Loose handrail',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Report'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(
      find.text('You do not have permission to perform this action.'),
      findsOneWidget,
    );
    expect(store.value, isNull, reason: 'a refusal is not kept for later');

    // And the button comes back, so the report can be made again.
    final button = tester.widget<TextButton>(
      find.widgetWithText(TextButton, 'Report a defect'),
    );
    expect(button.onPressed, isNotNull);
  });

  group('with no signal', () {
    testWidgets('the report is kept on the phone and says so', (tester) async {
      final repository = _FakeWorkItems(
        refuseWith: ApiException('offline', kind: ApiFailureKind.offline),
      );
      final store = _MemoryOutboxStore();

      await _pumpButton(tester, repository, store: store);
      await _openSheet(tester);

      await tester.enterText(
        find.widgetWithText(TextField, 'What is wrong'),
        'Loose handrail',
      );
      await tester.tap(find.widgetWithText(FilledButton, 'Report'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      expect(
        find.text('Recorded on this phone. It will be sent when there is signal.'),
        findsOneWidget,
      );
      expect(store.value, isNotNull, reason: 'and it is on disk');
    });

    testWidgets('it goes once signal returns, under the same key, only once',
        (tester) async {
      final repository = _FakeWorkItems(
        refuseWith: ApiException('offline', kind: ApiFailureKind.offline),
      );
      final store = _MemoryOutboxStore();

      await _pumpButton(tester, repository, store: store);
      await _openSheet(tester);

      await tester.enterText(
        find.widgetWithText(TextField, 'What is wrong'),
        'Loose handrail',
      );
      await tester.tap(find.widgetWithText(FilledButton, 'Report'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      final firstKey = repository.keys.single;

      repository.refuseWith = null;

      await tester.pump(OutboxController.retryInterval);
      await tester.pump(const Duration(milliseconds: 100));

      expect(repository.reported, hasLength(2), reason: 'the attempt, then the retry');
      expect(
        repository.keys.last,
        firstKey,
        reason: 'a new key would file the same defect twice if the first '
            'attempt had in fact arrived',
      );
      expect(store.value, isNull, reason: 'and it leaves the phone');

      await tester.pump(OutboxController.retryInterval);
      expect(repository.reported, hasLength(2), reason: 'nothing left to send');
    });
  });
}
