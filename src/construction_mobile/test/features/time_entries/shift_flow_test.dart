import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/time_entries/data/models/time_entry.dart';
import 'package:construction_mobile/features/time_entries/data/time_entry_repository.dart';
import 'package:construction_mobile/features/time_entries/presentation/shift_screen.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';

/// Clocking in and out, pressed rather than called.
///
/// This is the screen a worker opens twice a day and the only one whose
/// failure costs somebody their hours. It is also where `C10` lived: the
/// bottom sheet asking for the unpaid break could be dismissed by a tap
/// outside, and a dismissal resolves to the same `null` as pressing Cancel —
/// so from the worker's side, clocking out sometimes just did nothing.
///
/// Nothing in the suite pressed these buttons before. Every test here goes
/// through the widgets, because that is the layer both device-found bugs on
/// this path were in; the controller underneath was correct in each case.

/// A handset that will not give a position.
///
/// The clock-in carries a GPS stamp when one can be had quickly, and works
/// without one on purpose — a worker in a basement must not be unable to start
/// work because the GPS is thinking about it. Refusing here keeps that path
/// deterministic and exercises the branch that matters most on a site.
class _NoPosition extends GeolocatorPlatform {
  @override
  Future<LocationPermission> checkPermission() async =>
      LocationPermission.denied;
}

class _FakeTimeEntries implements TimeEntryRepository {
  _FakeTimeEntries({this.current, this.refuseWith});

  TimeEntry? current;

  /// What the API says no with, if it says no.
  ApiException? refuseWith;

  final List<int> clockOutBreaks = <int>[];
  int clockIns = 0;

  @override
  Future<TimeEntry?> fetchCurrent() async => current;

  @override
  Future<PagedList<TimeEntry>> fetchMine({
    int pageNumber = 1,
    int pageSize = 20,
    String? sortBy,
    bool sortDescending = true,
  }) async {
    return PagedList<TimeEntry>(
      items: const <TimeEntry>[],
      pageNumber: 1,
      pageSize: pageSize,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }

  @override
  Future<TimeEntry> clockIn({
    String? projectId,
    String workType = 'Regular',
    String? note,
    double? latitude,
    double? longitude,
  }) async {
    clockIns++;

    if (refuseWith != null) {
      throw refuseWith!;
    }

    return current = _running();
  }

  @override
  Future<TimeEntry> clockOut({
    int breakMinutes = 0,
    String? note,
    double? latitude,
    double? longitude,
  }) async {
    clockOutBreaks.add(breakMinutes);

    if (refuseWith != null) {
      throw refuseWith!;
    }

    final finished = _running().copyWith(
      endedAt: DateTime.now().toUtc(),
      breakMinutes: breakMinutes,
      workedMinutes: 480 - breakMinutes,
    );

    current = null;
    return finished;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

TimeEntry _running() => TimeEntry(
      id: '019fae00-0000-7000-8000-000000000001',
      employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
      employeeName: 'Ivan Horvat',
      startedAt: DateTime.now().toUtc().subtract(const Duration(hours: 2)),
      breakMinutes: 0,
      workType: 'Regular',
      status: 'Pending',
      createdAt: DateTime.now().toUtc(),
    );

const _worker = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'ivan@construction.local',
  role: 'Worker',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
  firstName: 'Ivan',
  lastName: 'Horvat',
);

Future<void> _pumpShiftScreen(
  WidgetTester tester,
  _FakeTimeEntries repository,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        currentUserProvider.overrideWithValue(_worker),
        timeEntryRepositoryProvider.overrideWithValue(repository),
      ],
      child: const MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        locale: Locale('en'),
        home: ShiftScreen(),
      ),
    ),
  );

  // The card and the list load from separate providers; both are awaited
  // before anything on this screen is pressable.
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

void main() {
  setUp(() => GeolocatorPlatform.instance = _NoPosition());

  testWidgets('clocking in starts a shift and the card says so', (tester) async {
    final repository = _FakeTimeEntries();

    await _pumpShiftScreen(tester, repository);

    expect(find.text('You are not clocked in'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Clock in'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(repository.clockIns, 1);
    expect(find.text('You are clocked in'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Clock out'), findsOneWidget);
  });

  testWidgets('clocking out asks for the break and sends the number given',
      (tester) async {
    final repository = _FakeTimeEntries(current: _running());

    await _pumpShiftScreen(tester, repository);

    expect(find.text('You are clocked in'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Clock out'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('End the shift'), findsOneWidget);

    await tester.enterText(find.byType(TextField), '30');
    await tester.tap(find.widgetWithText(FilledButton, 'Confirm'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(repository.clockOutBreaks, [30]);
    expect(find.text('You are not clocked in'), findsOneWidget);
  });

  /// `C10`, pinned.
  ///
  /// A dismissal and a cancellation both resolve to null, so the screen cannot
  /// tell them apart — which means the sheet must not be dismissible, or a
  /// stray tap on the way to the field reads as "clock out is broken".
  testWidgets('the break sheet ignores a tap outside it', (tester) async {
    final repository = _FakeTimeEntries(current: _running());

    await _pumpShiftScreen(tester, repository);

    await tester.tap(find.widgetWithText(FilledButton, 'Clock out'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('End the shift'), findsOneWidget);

    // The barrier above the sheet, which on a dismissible sheet closes it.
    await tester.tapAt(const Offset(400, 40));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(
      find.text('End the shift'),
      findsOneWidget,
      reason: 'a stray tap must not read as a cancelled clock-out',
    );
    expect(repository.clockOutBreaks, isEmpty);

    // And Cancel still does close it, or the worker would be trapped.
    await tester.tap(find.widgetWithText(TextButton, 'Cancel'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('End the shift'), findsNothing);
    expect(find.text('You are clocked in'), findsOneWidget);
  });

  testWidgets('a blank break is nought, not a refusal', (tester) async {
    final repository = _FakeTimeEntries(current: _running());

    await _pumpShiftScreen(tester, repository);

    await tester.tap(find.widgetWithText(FilledButton, 'Clock out'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    await tester.enterText(find.byType(TextField), '');
    await tester.tap(find.widgetWithText(FilledButton, 'Confirm'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // Somebody standing at the gate is not kept there over an empty field.
    expect(repository.clockOutBreaks, [0]);
  });

  testWidgets('a refused clock-in says why and leaves the button usable',
      (tester) async {
    final repository = _FakeTimeEntries(
      refuseWith: ApiException(
        'offline',
        kind: ApiFailureKind.offline,
      ),
    );

    await _pumpShiftScreen(tester, repository);

    await tester.tap(find.widgetWithText(FilledButton, 'Clock in'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(
      find.text('No connection to the server. Check your network and try again.'),
      findsOneWidget,
    );

    // Still off shift, and still able to try again once there is signal.
    expect(find.text('You are not clocked in'), findsOneWidget);

    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Clock in'),
    );
    expect(button.onPressed, isNotNull);
  });

  testWidgets('an admin account is told instead of being shown 403s',
      (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          currentUserProvider.overrideWithValue(
            const User(
              id: '019fad65-d635-76f2-880f-d8d25aea67d1',
              email: 'admin@construction.local',
              role: 'Admin',
            ),
          ),
          timeEntryRepositoryProvider.overrideWithValue(_FakeTimeEntries()),
        ],
        child: const MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          locale: Locale('en'),
          home: ShiftScreen(),
        ),
      ),
    );
    await tester.pump();

    expect(find.widgetWithText(FilledButton, 'Clock in'), findsNothing);
  });
}
