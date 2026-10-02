import 'package:construction_mobile/features/employees/data/models/employee.dart';
import 'package:construction_mobile/features/projects/data/models/project.dart';
import 'package:flutter_test/flutter_test.dart';

/// The API now says which business unit employs a person and which one a site belongs to. Both are
/// optional: an older server, or a person or site in no unit, simply has none.
void main() {
  Map<String, dynamic> employee({Map<String, dynamic> extra = const {}}) => {
        'id': '1',
        'employeeNumber': 'E-1',
        'firstName': 'Ana',
        'lastName': 'Ilić',
        'fullName': 'Ana Ilić',
        'employmentDate': '2026-01-10',
        'position': 'Zidar',
        'status': 'Active',
        'createdAt': '2026-01-10T08:00:00Z',
        ...extra,
      };

  test('an employee carries the unit that employs them', () {
    final parsed = Employee.fromJson(employee(extra: {
      'branchId': 'b1',
      'branchName': 'Plan Concept Beograd',
      'branchColor': '#3457D5',
    }));

    expect(parsed.branchName, 'Plan Concept Beograd');
    expect(parsed.branchColor, '#3457D5');
  });

  test('an employee in no unit, or from an older server, has none', () {
    expect(Employee.fromJson(employee()).branchName, isNull);
    expect(Employee.fromJson(employee(extra: {'branchName': null})).branchId, isNull);
  });

  test('the detail view reads it too', () {
    final detail = EmployeeDetail.fromJson(employee(extra: {'branchName': 'Sarajevo'}));

    expect(detail.branchName, 'Sarajevo');
  });

  test('a site carries its unit, and works without one', () {
    final base = {
      'id': 'p1',
      'name': 'Gradilište A',
      'status': 'Active',
      'createdAt': '2026-01-10T08:00:00Z',
    };

    expect(Project.fromJson({...base, 'branchName': 'Sarajevo', 'branchColor': '#0F8A5F'}).branchName, 'Sarajevo');
    expect(Project.fromJson(base).branchName, isNull);
  });
}
