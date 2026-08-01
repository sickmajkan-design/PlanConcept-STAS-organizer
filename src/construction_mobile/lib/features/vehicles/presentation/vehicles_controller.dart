import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/vehicle.dart';
import '../data/vehicle_repository.dart';

/// Vehicle statuses offered as filter chips, mirroring the API's enum.
const vehicleStatusFilters = <String>[
  'Available',
  'Assigned',
  'InService',
  'OutOfService',
];

class VehiclesController extends FilteredPagedListNotifier<Vehicle> {
  @override
  Future<PagedList<Vehicle>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(vehicleRepositoryProvider).fetchVehicles(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          status: filter,
        );
  }
}

final vehiclesControllerProvider =
    AsyncNotifierProvider<VehiclesController, PagedState<Vehicle>>(
  VehiclesController.new,
);

final vehicleDetailProvider =
    FutureProvider.autoDispose.family<Vehicle, String>((ref, id) {
  return ref.watch(vehicleRepositoryProvider).fetchVehicle(id);
});
