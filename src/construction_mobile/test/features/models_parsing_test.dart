import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/employees/data/models/employee.dart';
import 'package:construction_mobile/features/materials/data/models/material.dart';
import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/projects/data/models/project.dart';
import 'package:construction_mobile/features/tools/data/models/tool.dart';
import 'package:construction_mobile/features/vehicles/data/models/vehicle.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Employee', () {
    test('parses the list payload, including date-only fields', () {
      final employee = Employee.fromJson(const {
        'id': '019fad73-e894-791b-a6c3-715bddf61164',
        'employeeNumber': 'EMP-001',
        'firstName': 'Ivan',
        'lastName': 'Horvat',
        'fullName': 'Ivan Horvat',
        'phone': '+385919999999',
        'email': 'ivan.horvat@example.com',
        'address': 'Ilica 1, Zagreb',
        'dateOfBirth': '1988-04-12',
        'employmentDate': '2020-03-01',
        'position': 'Site Manager',
        'status': 'Active',
        'createdAt': '2026-07-29T10:30:00Z',
        'updatedAt': '2026-07-29T11:00:00Z',
      });

      expect(employee.fullName, 'Ivan Horvat');
      expect(employee.employmentDate, DateTime(2020, 3, 1));
      expect(employee.dateOfBirth, DateTime(1988, 4, 12));
      expect(employee.initials, 'IH');
    });

    test('tolerates the optional fields being absent', () {
      final employee = Employee.fromJson(const {
        'id': '019fad74-7cc3-7b5d-8adb-b4a3a2d774d6',
        'employeeNumber': 'EMP-003',
        'firstName': 'Petra',
        'lastName': 'Kovac',
        'fullName': 'Petra Kovac',
        'employmentDate': '2025-02-01',
        'position': 'Worker',
        'status': 'Active',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(employee.phone, isNull);
      expect(employee.updatedAt, isNull);
    });

    test('parses the detail payload with project assignments', () {
      final detail = EmployeeDetail.fromJson(const {
        'id': '019fad73-e894-791b-a6c3-715bddf61164',
        'employeeNumber': 'EMP-001',
        'firstName': 'Ivan',
        'lastName': 'Horvat',
        'fullName': 'Ivan Horvat',
        'employmentDate': '2020-03-01',
        'position': 'Site Manager',
        'status': 'Active',
        'createdAt': '2026-07-29T10:30:00Z',
        'hasUserAccount': true,
        'projects': [
          {
            'projectId': '019fad7f-0000-7000-8000-000000000002',
            'projectName': 'Harbor Bridge Repair',
            'projectStatus': 'Planned',
            'assignedAt': '2026-07-29T11:30:00Z',
          },
        ],
      });

      expect(detail.hasUserAccount, isTrue);
      expect(detail.projects.single.projectName, 'Harbor Bridge Repair');
      expect(detail.initials, 'IH');
    });

    test('defaults the project list when the API omits it', () {
      final detail = EmployeeDetail.fromJson(const {
        'id': '019fad74-7cc3-7b5d-8adb-b4a3a2d774d6',
        'employeeNumber': 'EMP-003',
        'firstName': 'Petra',
        'lastName': 'Kovac',
        'fullName': 'Petra Kovac',
        'employmentDate': '2025-02-01',
        'position': 'Worker',
        'status': 'Active',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(detail.projects, isEmpty);
      expect(detail.hasUserAccount, isFalse);
    });
  });

  group('Project', () {
    test('parses the list payload', () {
      final project = Project.fromJson(const {
        'id': '019fad7f-0000-7000-8000-000000000002',
        'name': 'Riverside Apartments',
        'client': 'Riverside d.o.o.',
        'address': 'Savska cesta 100, Zagreb',
        'latitude': 45.7967,
        'longitude': 15.96,
        'startDate': '2026-03-01',
        'endDate': '2027-09-30',
        'status': 'Active',
        'employeeCount': 3,
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(project.name, 'Riverside Apartments');
      expect(project.employeeCount, 3);
      expect(project.hasCoordinates, isTrue);
    });

    test('reports missing coordinates', () {
      final project = Project.fromJson(const {
        'id': '019fad7f-0000-7000-8000-000000000003',
        'name': 'Harbor Bridge Repair',
        'status': 'Planned',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(project.hasCoordinates, isFalse);
      expect(project.employeeCount, 0);
    });

    test('parses the detail payload with its crew', () {
      final detail = ProjectDetail.fromJson(const {
        'id': '019fad7f-0000-7000-8000-000000000002',
        'name': 'Riverside Apartments',
        'status': 'Active',
        'employeeCount': 1,
        'createdAt': '2026-07-29T10:30:00Z',
        'employees': [
          {
            'employeeId': '019fad73-e894-791b-a6c3-715bddf61164',
            'employeeNumber': 'EMP-001',
            'fullName': 'Ivan Horvat',
            'position': 'Site Manager',
            'status': 'Active',
            'assignedAt': '2026-07-29T11:30:00Z',
          },
        ],
      });

      expect(detail.employees.single.fullName, 'Ivan Horvat');
    });
  });

  group('Vehicle', () {
    test('parses an assigned vehicle', () {
      final vehicle = Vehicle.fromJson(const {
        'id': '019fae00-0000-7000-8000-000000000001',
        'brand': 'Ford',
        'model': 'Transit',
        'registrationNumber': 'ZG1234AB',
        'vin': 'WF0XXXTTGXKA12345',
        'fuelType': 'Diesel',
        'status': 'Assigned',
        'assignedEmployeeId': '019fad73-e894-791b-a6c3-715bddf61164',
        'assignedEmployeeName': 'Ivan Horvat',
        'assignedEmployeeNumber': 'EMP-001',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(vehicle.displayName, 'Ford Transit');
      expect(vehicle.isAssigned, isTrue);
    });

    test('parses an unassigned vehicle', () {
      final vehicle = Vehicle.fromJson(const {
        'id': '019fae00-0000-7000-8000-000000000002',
        'brand': 'Iveco',
        'model': 'Daily',
        'registrationNumber': 'ZG5678CD',
        'fuelType': 'Diesel',
        'status': 'Available',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(vehicle.isAssigned, isFalse);
      expect(vehicle.vin, isNull);
    });
  });

  group('Tool', () {
    test('parses a tool assigned to both an employee and a project', () {
      final tool = Tool.fromJson(const {
        'id': '019fae10-0000-7000-8000-000000000001',
        'name': 'Cordless drill',
        'category': 'Power tools',
        'serialNumber': 'SN-9001',
        'qrCode': 'QR-9001',
        'status': 'Assigned',
        'assignedEmployeeId': '019fad73-e894-791b-a6c3-715bddf61164',
        'assignedEmployeeName': 'Ivan Horvat',
        'assignedEmployeeNumber': 'EMP-001',
        'assignedProjectId': '019fad7f-0000-7000-8000-000000000002',
        'assignedProjectName': 'Riverside Apartments',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(tool.isAssignedToEmployee, isTrue);
      expect(tool.isAssignedToProject, isTrue);
    });

    test('parses an unassigned tool', () {
      final tool = Tool.fromJson(const {
        'id': '019fae10-0000-7000-8000-000000000002',
        'name': 'Angle grinder',
        'status': 'Available',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(tool.isAssignedToEmployee, isFalse);
      expect(tool.isAssignedToProject, isFalse);
      expect(tool.qrCode, isNull);
    });
  });

  group('MaterialItem', () {
    test('parses a material assigned to a project', () {
      final material = MaterialItem.fromJson(const {
        'id': '019fae20-0000-7000-8000-000000000001',
        'name': 'Cement',
        'unit': 'bag',
        'quantity': 120.5,
        'warehouse': 'Main warehouse',
        'projectId': '019fad7f-0000-7000-8000-000000000002',
        'projectName': 'Riverside Apartments',
        'lastUpdated': '2026-07-29T12:00:00Z',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(material.quantity, 120.5);
      expect(material.isAssignedToProject, isTrue);
    });

    test('parses warehouse-only stock', () {
      final material = MaterialItem.fromJson(const {
        'id': '019fae20-0000-7000-8000-000000000002',
        'name': 'Rebar',
        'unit': 'kg',
        'quantity': 500,
        'lastUpdated': '2026-07-29T12:00:00Z',
        'createdAt': '2026-07-29T10:30:00Z',
      });

      expect(material.isAssignedToProject, isFalse);
      expect(material.projectName, isNull);
    });
  });

  group('AppNotification', () {
    test('parses a notification with a deep-link payload', () {
      final notification = AppNotification.fromJson(const {
        'id': '019fadb0-0000-7000-8000-000000000001',
        'type': 'ProjectAssigned',
        'title': 'New project assigned',
        'body': "You have been assigned to project 'Harbor Bridge Repair'.",
        'dataJson': '{"projectId":"019fad7f-0000-7000-8000-000000000002"}',
        'isRead': false,
        'createdAt': '2026-07-29T12:00:00Z',
      });

      expect(notification.type, 'ProjectAssigned');
      expect(notification.isRead, isFalse);
      expect(notification.readAt, isNull);
    });

    test('parses a read announcement without a payload', () {
      final notification = AppNotification.fromJson(const {
        'id': '019fadb0-0000-7000-8000-000000000002',
        'type': 'GeneralAnnouncement',
        'title': 'Safety reminder',
        'body': 'Hard hats are mandatory on all sites starting Monday.',
        'isRead': true,
        'readAt': '2026-07-29T12:30:00Z',
        'createdAt': '2026-07-29T12:00:00Z',
      });

      expect(notification.isRead, isTrue);
      expect(notification.readAt, isNotNull);
      expect(notification.dataJson, isNull);
    });
  });

  group('User role gating', () {
    User userWithRole(String role) => User(
          id: '019fad65-d635-76f2-880f-d8d25aea67d0',
          email: 'user@construction.local',
          role: role,
        );

    test('lets Foreman and above read the directory', () {
      for (final role in ['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman']) {
        expect(userWithRole(role).canViewDirectory, isTrue, reason: role);
      }
    });

    test('withholds the directory from Workers', () {
      expect(userWithRole('Worker').canViewDirectory, isFalse);
    });
  });
}
