import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle_toll.dart';

/// Vignettes, tunnels and passages for a vehicle. Read-only on mobile — the
/// admin panel is where they are added, renewed and removed.
class VehicleTollRepository extends ApiRepository {
  const VehicleTollRepository(super.dio);

  /// One vehicle's tolls, expired/expiring-soon first (the API's own order —
  /// keep it). Not paged: a vehicle carries a handful.
  Future<List<VehicleToll>> fetch(String vehicleId) {
    return guard(() async {
      final response = await dio.get<List<dynamic>>(
        '/api/v1/vehicletolls',
        queryParameters: {'vehicleId': vehicleId},
      );

      return (response.data ?? const [])
          .cast<Map<String, dynamic>>()
          .map(VehicleToll.fromJson)
          .toList();
    });
  }
}

final vehicleTollRepositoryProvider = Provider<VehicleTollRepository>((ref) {
  return VehicleTollRepository(ref.watch(apiClientProvider));
});
