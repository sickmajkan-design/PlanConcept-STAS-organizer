import 'dart:convert';
import 'dart:typed_data';

import 'package:construction_mobile/features/vehicles/data/models/vehicle.dart';
import 'package:construction_mobile/features/vehicles/data/vehicle_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records the request a repository built and answers with a canned vehicle.
class _RecordingAdapter implements HttpClientAdapter {
  RequestOptions? lastRequest;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    lastRequest = options;

    return ResponseBody.fromString(
      jsonEncode(<String, dynamic>{
        'id': 'v1',
        'brand': 'Iveco',
        'model': 'Daily',
        'registrationNumber': 'ZG-1',
        'tdNumber': '15',
        'fuelType': 'Diesel',
        'status': 'Available',
        'createdAt': '2026-10-06T08:00:00Z',
      }),
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

VehicleRepository _repository(_RecordingAdapter adapter) => VehicleRepository(
      Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter,
    );

Map<String, dynamic> _body(_RecordingAdapter adapter) =>
    adapter.lastRequest!.data as Map<String, dynamic>;

void main() {
  group('saving a vehicle from the phone', () {
    test('a new vehicle carries its TD number, which the API requires', () async {
      final adapter = _RecordingAdapter();

      await _repository(adapter).create(
        brand: 'Iveco',
        model: 'Daily',
        registrationNumber: 'ZG-1',
        tdNumber: '15',
        fuelType: 'Diesel',
        registrationValidUntil: '2027-03-31',
      );

      expect(_body(adapter)['tdNumber'], '15');
      expect(_body(adapter)['registrationValidUntil'], '2027-03-31');
    });

    test('an edit hands back what the form does not edit, so the web panel\'s data survives', () async {
      final adapter = _RecordingAdapter();

      await _repository(adapter).update(
        'v1',
        brand: 'Iveco',
        model: 'Daily',
        registrationNumber: 'ZG-1',
        tdNumber: '15',
        fuelType: 'Diesel',
        status: 'Available',
        ownershipType: 'Owned',
        qrCode: 'VH-ABC',
        gpsProvider: 'Teltonika',
        gpsTrackingUrl: 'https://gps.example/v1',
        branchId: 'b1',
      );

      final body = _body(adapter);
      expect(body['qrCode'], 'VH-ABC');
      expect(body['gpsProvider'], 'Teltonika');
      expect(body['gpsTrackingUrl'], 'https://gps.example/v1');
      expect(body['branchId'], 'b1');
    });

    test('a date cleared on the phone is sent as empty, not left out', () async {
      // The update replaces the whole record, so leaving a date out is how it is cleared.
      final adapter = _RecordingAdapter();

      await _repository(adapter).update(
        'v1',
        brand: 'Iveco',
        model: 'Daily',
        registrationNumber: 'ZG-1',
        tdNumber: '15',
        fuelType: 'Diesel',
        status: 'Available',
        ownershipType: 'Owned',
        insuranceValidUntil: null,
      );

      expect(_body(adapter).containsKey('insuranceValidUntil'), isTrue);
      expect(_body(adapter)['insuranceValidUntil'], isNull);
    });
  });

  group('the vehicle model', () {
    test('reads the new dates and says whether a rental end applies', () {
      final vehicle = Vehicle.fromJson(<String, dynamic>{
        'id': 'v1',
        'brand': 'Iveco',
        'model': 'Daily',
        'registrationNumber': 'ZG-1',
        'tdNumber': '15',
        'fuelType': 'Diesel',
        'status': 'Available',
        'ownershipType': 'Leased',
        'registrationValidUntil': '2027-03-31',
        'rentedUntil': '2027-06-30',
        'createdAt': '2026-10-06T08:00:00Z',
      });

      expect(vehicle.tdNumber, '15');
      expect(vehicle.registrationValidUntil, '2027-03-31');
      expect(vehicle.isHeldOnRental, isTrue);
    });

    test('an owned vehicle has no rental end to show', () {
      final vehicle = Vehicle.fromJson(<String, dynamic>{
        'id': 'v1',
        'brand': 'Iveco',
        'model': 'Daily',
        'registrationNumber': 'ZG-1',
        'fuelType': 'Diesel',
        'status': 'Available',
        'createdAt': '2026-10-06T08:00:00Z',
      });

      expect(vehicle.isHeldOnRental, isFalse);
      expect(vehicle.tdNumber, isNull);
    });
  });
}
