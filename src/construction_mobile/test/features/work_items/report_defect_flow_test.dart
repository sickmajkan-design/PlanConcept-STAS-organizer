import 'package:construction_mobile/core/network/api_exception.dart';
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

  final List<({String projectId, String title, String? description})> reported =
      <({String projectId, String title, String? description})>[];

  @override
  Future<WorkItem> reportDefect({
    required String projectId,
    required String title,
    String? description,
    double? latitude,
    double? longitude,
  }) async {
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

Future<void> _pumpButton(WidgetTester tester, _FakeWorkItems repository) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        workItemRepositoryProvider.overrideWithValue(repository),
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

  testWidgets('a refused report says why, in the reader\'s language',
      (tester) async {
    final repository = _FakeWorkItems(
      refuseWith: ApiException('offline', kind: ApiFailureKind.offline),
    );

    await _pumpButton(tester, repository);
    await _openSheet(tester);

    await tester.enterText(
      find.widgetWithText(TextField, 'What is wrong'),
      'Loose handrail',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Report'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(
      find.text(
        'No connection to the server. Check your network and try again.',
      ),
      findsOneWidget,
    );

    // And the button comes back, so the report can be made again in signal.
    final button = tester.widget<TextButton>(
      find.widgetWithText(TextButton, 'Report a defect'),
    );
    expect(button.onPressed, isNotNull);
  });
}
