import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
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

class LocationRepository extends ApiRepository {
  const LocationRepository(super.dio);

  /// The API rejects larger batches.
  static const int maxBatchSize = 120;

  /// Sends a batch of pings. The employee is identified by the JWT, never by
  /// the payload.
  Future<void> report(List<LocationPing> pings) async {
    if (pings.isEmpty) {
      return;
    }

    return postVoid(
      '/api/locations',
      data: {'pings': pings.map((ping) => ping.toJson()).toList()},
    );
  }
}

final locationRepositoryProvider = Provider<LocationRepository>((ref) {
  return LocationRepository(ref.watch(apiClientProvider));
});
