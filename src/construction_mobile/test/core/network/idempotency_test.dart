import 'dart:convert';
import 'dart:typed_data';

import 'package:construction_mobile/core/network/idempotency.dart';
import 'package:construction_mobile/features/costs/data/vehicle_expense_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records the request a repository built and answers with a canned body.
class _RecordingAdapter implements HttpClientAdapter {
  _RecordingAdapter(this._body);

  final Object? _body;

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
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

Map<String, dynamic> _expense() => <String, dynamic>{
      'id': '019fad65-d635-76f2-880f-d8d25aea67d0',
      'vehicleId': '019fad65-d635-76f2-880f-d8d25aea67d1',
      'vehicleName': 'BG-123-AB',
      'kind': 'Fuel',
      'amount': 8500.0,
      'occurredOn': '2026-08-09',
      'litres': 45.0,
      'pricePerLitre': null,
      'odometerKm': 120450,
      'supplier': 'NIS',
      'note': null,
      'recordedByName': null,
      'createdAt': '2026-08-09T08:00:00Z',
    };

void main() {
  group('newIdempotencyKey', () {
    test('is long enough for the API to accept', () {
      // The API refuses anything under 8 characters rather than ignoring it,
      // so a key that quietly got shorter would fail every write — but only
      // against a real server.
      expect(newIdempotencyKey().length, 32);
    });

    test('does not repeat', () {
      final keys = List.generate(500, (_) => newIdempotencyKey()).toSet();

      // Two phones starting the same screen in the same millisecond is an
      // ordinary event at the start of a shift, and a shared key would have
      // one worker's expense answered with the other's.
      expect(keys.length, 500);
    });
  });

  group('a recorded vehicle expense', () {
    test('carries the key it was given', () async {
      final adapter = _RecordingAdapter(_expense());
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
        ..httpClientAdapter = adapter;

      await VehicleExpenseRepository(dio).record(
        vehicleId: '019fad65-d635-76f2-880f-d8d25aea67d1',
        kind: 'Fuel',
        amount: 8500,
        litres: 45,
        idempotencyKey: 'a-key-long-enough-for-the-api',
      );

      expect(
        adapter.lastRequest!.headers[idempotencyHeader],
        'a-key-long-enough-for-the-api',
      );
    });

    test('sends no header when no key was given', () async {
      // Threading it through is optional at the repository, so the absence
      // has to be as deliberate as the presence.
      final adapter = _RecordingAdapter(_expense());
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
        ..httpClientAdapter = adapter;

      await VehicleExpenseRepository(dio).record(
        vehicleId: '019fad65-d635-76f2-880f-d8d25aea67d1',
        kind: 'Service',
        amount: 12000,
      );

      expect(adapter.lastRequest!.headers.containsKey(idempotencyHeader), isFalse);
    });
  });
}
