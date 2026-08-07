import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle.dart';

class VehicleRepository extends ApiRepository {
  const VehicleRepository(super.dio);

  Future<PagedList<Vehicle>> fetchVehicles({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? sortBy,
    bool sortDescending = false,
  }) {
    return getPaged(
      '/api/v1/vehicles',
      Vehicle.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        sortBy: sortBy,
        sortDescending: sortDescending,
        filters: {'status': status},
      ),
    );
  }

  Future<Vehicle> fetchVehicle(String id) {
    return getJson('/api/v1/vehicles/$id', Vehicle.fromJson);
  }
}

final vehicleRepositoryProvider = Provider<VehicleRepository>((ref) {
  return VehicleRepository(ref.watch(apiClientProvider));
});
