import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';

/// One GPS fix captured by the device, in the shape the API expects.
class LocationPing {
  const LocationPing({
    required this.latitude,
    required this.longitude,
    required this.timestamp,
    this.accuracy,
  });

  final double latitude;
  final double longitude;
  final double? accuracy;
  final DateTime timestamp;

  Map<String, dynamic> toJson() => <String, dynamic>{
        'latitude': latitude,
        'longitude': longitude,
        if (accuracy != null) 'accuracy': accuracy,
        'timestamp': timestamp.toUtc().toIso8601String(),
      };
}

class LocationRepository {
  LocationRepository(this._dio);

  /// The API rejects larger batches.
  static const int maxBatchSize = 120;

  final Dio _dio;

  /// Sends a batch of pings. The employee is identified by the JWT, never by
  /// the payload.
  Future<void> report(List<LocationPing> pings) async {
    if (pings.isEmpty) {
      return;
    }

    try {
      await _dio.post<void>(
        '/api/locations',
        data: {'pings': pings.map((ping) => ping.toJson()).toList()},
      );
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }
}

final locationRepositoryProvider = Provider<LocationRepository>((ref) {
  return LocationRepository(ref.watch(apiClientProvider));
});
