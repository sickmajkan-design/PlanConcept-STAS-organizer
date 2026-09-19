import 'package:flutter_test/flutter_test.dart';
import 'package:construction_mobile/features/costs/data/models/vehicle_expense.dart';

Map<String, dynamic> _json({Map<String, dynamic> extra = const {}}) => {
      'id': '1',
      'vehicleId': 'v1',
      'vehicleName': 'Iveco (DEMO-001)',
      'kind': 'Service',
      'amount': 100.0,
      'occurredOn': '2026-09-19',
      'createdAt': '2026-09-19T07:00:00Z',
      ...extra,
    };

void main() {
  group('VehicleExpense review status', () {
    test('reads the status and the reason a reviewer gave', () {
      final expense = VehicleExpense.fromJson(_json(extra: {
        'status': 'Rejected',
        'reviewNote': 'Receipt missing.',
      }));

      expect(expense.status, 'Rejected');
      expect(expense.isRejected, isTrue);
      expect(expense.reviewNote, 'Receipt missing.');
    });

    test('is Pending when an API that predates the workflow leaves it out', () {
      final expense = VehicleExpense.fromJson(_json());

      expect(expense.status, 'Pending');
      expect(expense.isRejected, isFalse);
      expect(expense.reviewNote, isNull);
    });

    test('an approved cost is not shown as rejected', () {
      final expense = VehicleExpense.fromJson(_json(extra: {'status': 'Approved'}));

      expect(expense.isRejected, isFalse);
    });
  });
}
