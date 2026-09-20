import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/notifications/presentation/notification_deep_link.dart';
import 'package:flutter_test/flutter_test.dart';

AppNotification _notification({
  required String type,
  String? dataJson,
}) {
  return AppNotification(
    id: '019fadb0-0000-7000-8000-000000000001',
    type: type,
    title: 'title',
    body: 'body',
    dataJson: dataJson,
    createdAt: DateTime.now().toUtc(),
  );
}

void main() {
  group('deepLinkFor', () {
    test('opens the project a ProjectAssigned notification points at', () {
      final target = deepLinkFor(
        _notification(
          type: 'ProjectAssigned',
          dataJson: '{"projectId":"019fad7f-1111-7000-8000-000000000002",'
              '"employeeId":"019fad73-2222-7000-8000-000000000003"}',
        ),
        canViewDirectory: true,
      );

      expect(target, '/projects/019fad7f-1111-7000-8000-000000000002');
    });

    test('opens the employee an EmployeeAssigned notification points at', () {
      final target = deepLinkFor(
        _notification(
          type: 'EmployeeAssigned',
          dataJson: '{"projectId":"019fad7f-1111-7000-8000-000000000002",'
              '"employeeId":"019fad73-2222-7000-8000-000000000003"}',
        ),
        canViewDirectory: true,
      );

      expect(target, '/employees/019fad73-2222-7000-8000-000000000003');
    });

    test('withholds directory links from roles the API would refuse', () {
      final target = deepLinkFor(
        _notification(
          type: 'ProjectAssigned',
          dataJson: '{"projectId":"019fad7f-1111-7000-8000-000000000002"}',
        ),
        canViewDirectory: false,
      );

      expect(target, isNull);
    });

    test('has nowhere to go for a general announcement', () {
      expect(
        deepLinkFor(
          _notification(type: 'GeneralAnnouncement'),
          canViewDirectory: true,
        ),
        isNull,
      );
    });

    test('ignores a payload without the expected id', () {
      expect(
        deepLinkFor(
          _notification(type: 'ProjectAssigned', dataJson: '{"other":1}'),
          canViewDirectory: true,
        ),
        isNull,
      );
    });

    test('survives a malformed payload', () {
      expect(
        deepLinkFor(
          _notification(type: 'ProjectAssigned', dataJson: 'not json'),
          canViewDirectory: true,
        ),
        isNull,
      );
    });

    test('links a vehicle assignment to the vehicle, for those who can open the directory', () {
      expect(
        deepLinkFor(
          _notification(
            type: 'VehicleAssigned',
            dataJson: '{"vehicleId":"019fad8c-3333-7000-8000-000000000004"}',
          ),
          canViewDirectory: true,
        ),
        '/vehicles/019fad8c-3333-7000-8000-000000000004',
      );
    });

    test('takes a worker to their own screens without needing the directory', () {
      String? link(String type) => deepLinkFor(
            _notification(type: type, dataJson: '{"x":"y"}'),
            canViewDirectory: false,
          );

      expect(link('AccommodationAssigned'), '/my-housing');
      expect(link('TimeEntryRejected'), '/time-entries');
      expect(link('AbsenceDecided'), '/absences');
      expect(link('WeeklyReportDue'), '/weekly-reports');
      expect(link('BulletinPosted'), '/bulletin');
      expect(link('TaskAssigned'), '/work-items');
      expect(link('VehicleAssigned'), isNull);
    });
  });
}
