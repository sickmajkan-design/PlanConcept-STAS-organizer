import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/features/auth/data/auth_repository.dart';
import 'package:construction_mobile/features/employees/data/employee_repository.dart';
import 'package:construction_mobile/features/location/data/location_repository.dart';
import 'package:construction_mobile/features/materials/data/material_repository.dart';
import 'package:construction_mobile/features/notifications/data/notification_repository.dart';
import 'package:construction_mobile/features/projects/data/project_repository.dart';
import 'package:construction_mobile/features/tools/data/tool_repository.dart';
import 'package:construction_mobile/features/vehicles/data/vehicle_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records what a repository put on the wire and answers with a canned body,
/// so the request a repository builds can be asserted without a server.
class _RecordingAdapter implements HttpClientAdapter {
  _RecordingAdapter(this._body, {this.statusCode = 200});

  final Object? _body;
  final int statusCode;

  RequestOptions? lastRequest;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    lastRequest = options;

    return ResponseBody.fromString(
      jsonEncode(_body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

/// The API's paging envelope with a single item, so [PagedList.fromJson] has
/// something well-formed to parse whatever the item type is.
Map<String, dynamic> _emptyPage() => <String, dynamic>{
      'items': <Map<String, dynamic>>[],
      'pageNumber': 1,
      'pageSize': 20,
      'totalCount': 0,
      'totalPages': 0,
      'hasNextPage': false,
      'hasPreviousPage': false,
    };

({Dio dio, _RecordingAdapter adapter}) _client(
  Object? body, {
  int statusCode = 200,
}) {
  final adapter = _RecordingAdapter(body, statusCode: statusCode);
  final dio = Dio(BaseOptions(baseUrl: 'http://api.test'))
    ..httpClientAdapter = adapter;

  return (dio: dio, adapter: adapter);
}

void main() {
  group('paged query building', () {
    test('sends paging, search, sort and the status filter', () async {
      final client = _client(_emptyPage());

      await VehicleRepository(client.dio).fetchVehicles(
        pageNumber: 3,
        pageSize: 50,
        search: 'excavator',
        status: 'InUse',
        sortBy: 'registrationNumber',
        sortDescending: true,
      );

      expect(client.adapter.lastRequest!.path, '/api/vehicles');
      expect(client.adapter.lastRequest!.queryParameters, {
        'pageNumber': 3,
        'pageSize': 50,
        'search': 'excavator',
        'sortBy': 'registrationNumber',
        'sortDescending': true,
        'status': 'InUse',
      });
    });

    test('omits every optional parameter that was not supplied', () async {
      final client = _client(_emptyPage());

      await VehicleRepository(client.dio).fetchVehicles();

      // An absent filter has to stay absent rather than go out empty, or the
      // API would match on a blank value instead of applying its default.
      expect(client.adapter.lastRequest!.queryParameters, {
        'pageNumber': 1,
        'pageSize': 20,
      });
    });

    test('drops a blank search the same way as a missing one', () async {
      final client = _client(_emptyPage());

      await ToolRepository(client.dio).fetchTools(search: '');

      expect(
        client.adapter.lastRequest!.queryParameters.containsKey('search'),
        isFalse,
      );
    });

    test('sortDescending is only sent when it is true', () async {
      final client = _client(_emptyPage());

      await ToolRepository(client.dio)
          .fetchTools(sortBy: 'name', sortDescending: false);

      expect(client.adapter.lastRequest!.queryParameters, {
        'pageNumber': 1,
        'pageSize': 20,
        'sortBy': 'name',
      });
    });

    test('employees send both of their filters', () async {
      final client = _client(_emptyPage());

      await EmployeeRepository(client.dio).fetchEmployees(
        status: 'Active',
        projectId: 'p-1',
      );

      expect(client.adapter.lastRequest!.queryParameters, {
        'pageNumber': 1,
        'pageSize': 20,
        'status': 'Active',
        'projectId': 'p-1',
      });
    });

    test('projects send both of their filters', () async {
      final client = _client(_emptyPage());

      await ProjectRepository(client.dio).fetchProjects(
        status: 'Active',
        employeeId: 'e-1',
      );

      expect(client.adapter.lastRequest!.queryParameters, {
        'pageNumber': 1,
        'pageSize': 20,
        'status': 'Active',
        'employeeId': 'e-1',
      });
    });

    test('materials send unassignedOnly only when it is on', () async {
      final on = _client(_emptyPage());
      await MaterialRepository(on.dio).fetchMaterials(unassignedOnly: true);
      expect(on.adapter.lastRequest!.queryParameters['unassignedOnly'], true);

      for (final off in <bool?>[false, null]) {
        final client = _client(_emptyPage());
        await MaterialRepository(client.dio).fetchMaterials(unassignedOnly: off);

        expect(
          client.adapter.lastRequest!.queryParameters
              .containsKey('unassignedOnly'),
          isFalse,
          reason: 'unassignedOnly: $off must mean "no filter"',
        );
      }
    });

    test('notifications send unreadOnly only when it is on', () async {
      final on = _client(_emptyPage());
      await NotificationRepository(on.dio).fetchNotifications(unreadOnly: true);
      expect(on.adapter.lastRequest!.queryParameters['unreadOnly'], true);

      final off = _client(_emptyPage());
      await NotificationRepository(off.dio).fetchNotifications();
      expect(
        off.adapter.lastRequest!.queryParameters.containsKey('unreadOnly'),
        isFalse,
      );
    });
  });

  group('single-resource paths', () {
    test('vehicle detail', () async {
      final client = _client(const <String, dynamic>{});
      try {
        await VehicleRepository(client.dio).fetchVehicle('v-1');
      } catch (_) {
        // The canned body is not a full vehicle; only the path matters here.
      }

      expect(client.adapter.lastRequest!.path, '/api/vehicles/v-1');
    });

    test('a QR code is escaped before it goes into the path', () async {
      final client = _client(const <String, dynamic>{});
      try {
        await ToolRepository(client.dio).fetchToolByQrCode('TL 001/A');
      } catch (_) {
        // Same: the canned body is not a full tool.
      }

      expect(client.adapter.lastRequest!.path, '/api/tools/by-qr/TL%20001%2FA');
    });
  });

  group('write calls', () {
    test('login posts the credentials', () async {
      final client = _client(const <String, dynamic>{});
      try {
        await AuthRepository(client.dio)
            .login(email: 'a@b.com', password: 'secret');
      } catch (_) {
        // The canned body is not a full auth response.
      }

      expect(client.adapter.lastRequest!.path, '/api/auth/login');
      expect(client.adapter.lastRequest!.method, 'POST');
      expect(client.adapter.lastRequest!.data,
          {'email': 'a@b.com', 'password': 'secret'});
    });

    test('logout posts the refresh token', () async {
      final client = _client(null, statusCode: 204);

      await AuthRepository(client.dio).logout('refresh-token');

      expect(client.adapter.lastRequest!.path, '/api/auth/logout');
      expect(client.adapter.lastRequest!.data, {'refreshToken': 'refresh-token'});
    });

    test('a device token registration carries the platform', () async {
      final client = _client(null, statusCode: 204);

      await NotificationRepository(client.dio)
          .registerDeviceToken(token: 'tok', platform: 'Android');

      expect(client.adapter.lastRequest!.path,
          '/api/notifications/device-tokens');
      expect(client.adapter.lastRequest!.data,
          {'token': 'tok', 'platform': 'Android'});
    });

    test('a location batch is posted under "pings"', () async {
      final client = _client(null, statusCode: 202);

      await LocationRepository(client.dio).report([
        LocationPing(
          latitude: 45.81,
          longitude: 15.98,
          accuracy: 8,
          timestamp: DateTime.utc(2026, 1, 2, 3, 4, 5),
        ),
      ]);

      final data = client.adapter.lastRequest!.data as Map<String, dynamic>;
      expect(client.adapter.lastRequest!.path, '/api/locations');
      expect(data['pings'], [
        {
          'latitude': 45.81,
          'longitude': 15.98,
          'accuracy': 8.0,
          'timestamp': '2026-01-02T03:04:05.000Z',
        },
      ]);
    });

    test('an empty batch never reaches the network', () async {
      final client = _client(null, statusCode: 202);

      await LocationRepository(client.dio).report(const []);

      expect(client.adapter.lastRequest, isNull);
    });
  });

  group('failure handling', () {
    test('an HTTP error surfaces as an ApiException, not a DioException',
        () async {
      final client = _client(
        {'title': 'Not found', 'status': 404},
        statusCode: 404,
      );

      await expectLater(
        VehicleRepository(client.dio).fetchVehicles(),
        throwsA(isA<ApiException>()),
      );
    });

    test('a write failure is converted too', () async {
      final client = _client({'title': 'Unauthorized'}, statusCode: 401);

      await expectLater(
        AuthRepository(client.dio).login(email: 'a@b.com', password: 'x'),
        throwsA(isA<ApiException>()),
      );
    });
  });
}
