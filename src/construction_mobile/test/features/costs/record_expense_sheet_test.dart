import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/theme/app_theme.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/costs/presentation/record_expense_sheet.dart';
import 'package:construction_mobile/features/vehicles/data/models/vehicle.dart';
import 'package:construction_mobile/features/vehicles/data/vehicle_repository.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// A fill-up is checked against the fuel-card statement afterwards, so the sheet will not send one
/// without the litres, the odometer reading and a photograph of the receipt. Anything else (a
/// repair, a service) needs none of the three.
class _FakeVehicles extends VehicleRepository {
  _FakeVehicles({this.mine = const []}) : super(Dio());

  /// What `fetchMyVehicles` returns: the vehicles signed out to a Worker.
  final List<Vehicle> mine;

  @override
  Future<List<Vehicle>> fetchMyVehicles() async => mine;

  @override
  Future<PagedList<Vehicle>> fetchVehicles({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return PagedList<Vehicle>(
      items: [
        Vehicle(
          id: 'v1',
          brand: 'Iveco',
          model: 'Daily',
          registrationNumber: 'ZG-1',
          fuelType: 'Diesel',
          status: 'Available',
          createdAt: DateTime.utc(2026, 1, 1),
        ),
      ],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );
  }
}

Future<void> _openSheet(
  WidgetTester tester, {
  String role = 'Foreman',
  List<Vehicle> mine = const [],
  bool pickVehicle = true,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        vehicleRepositoryProvider.overrideWithValue(_FakeVehicles(mine: mine)),
        currentUserProvider.overrideWithValue(
          User(id: 'me', email: 'me@example.test', role: role),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('en'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              onPressed: () => showRecordExpenseSheet(context),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();

  if (!pickVehicle) {
    return;
  }

  // Pick the only vehicle.
  await tester.tap(find.byType(DropdownButtonFormField<String>).first);
  await tester.pumpAndSettle();
  await tester.tap(find.textContaining('Iveco Daily').last);
  await tester.pumpAndSettle();
}

Finder _field(String label) => find.widgetWithText(TextField, label);

FilledButton _sendButton(WidgetTester tester) =>
    tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Record'));

void main() {
  testWidgets('a fill-up cannot be sent without the receipt photo, even with everything else filled in',
      (tester) async {
    await _openSheet(tester);

    await tester.enterText(_field('Amount'), '120.50');
    await tester.enterText(_field('Litres'), '80');
    await tester.enterText(_field('Odometer (km)'), '150000');
    await tester.pump();

    expect(find.text('A fill-up needs a photo of the receipt.'), findsOneWidget);
    expect(_sendButton(tester).onPressed, isNull);
  });

  testWidgets('a fill-up cannot be sent without the odometer reading', (tester) async {
    await _openSheet(tester);

    await tester.enterText(_field('Amount'), '120.50');
    await tester.enterText(_field('Litres'), '80');
    await tester.pump();

    expect(find.text('Enter the odometer reading (km).'), findsOneWidget);
    expect(_sendButton(tester).onPressed, isNull);
  });

  testWidgets('a repair needs neither the odometer nor a receipt photo', (tester) async {
    await _openSheet(tester);

    await tester.tap(find.byType(DropdownButtonFormField<String>).last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Repair').last);
    await tester.pumpAndSettle();

    await tester.enterText(_field('Amount'), '300');
    await tester.pump();

    expect(find.text('A fill-up needs a photo of the receipt.'), findsNothing);
    expect(_sendButton(tester).onPressed, isNotNull);
  });

  testWidgets('a worker with no vehicle signed out is told what to do first', (tester) async {
    await _openSheet(tester, role: 'Worker', pickVehicle: false);

    expect(
      find.text('No vehicle is signed out to you. Scan its QR label and take it first.'),
      findsOneWidget,
    );
    expect(_sendButton(tester).onPressed, isNull);
  });

  testWidgets('a worker is offered the vehicle in their hands and fuel only', (tester) async {
    await _openSheet(
      tester,
      role: 'Worker',
      mine: [
        Vehicle(
          id: 'v9',
          brand: 'Iveco',
          model: 'Daily',
          registrationNumber: 'ZG-9',
          fuelType: 'Diesel',
          status: 'Assigned',
          createdAt: DateTime.utc(2026, 1, 1),
        ),
      ],
    );

    // The kind list holds fuel alone, so there is nothing else to pick.
    await tester.tap(find.byType(DropdownButtonFormField<String>).last);
    await tester.pumpAndSettle();

    expect(find.text('Repair'), findsNothing);
    expect(find.text('Service'), findsNothing);
  });
}
