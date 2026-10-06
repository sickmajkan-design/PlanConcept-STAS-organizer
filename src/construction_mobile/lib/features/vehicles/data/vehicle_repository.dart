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

  Future<Vehicle> create({
    required String brand,
    required String model,
    required String registrationNumber,
    required String tdNumber,
    String? vin,
    required String fuelType,
    String status = 'Available',
    String ownershipType = 'Owned',
    String? registrationValidUntil,
    String? rentedUntil,
    String? technicalInspectionValidUntil,
    String? insuranceValidUntil,
    String? nextServiceDue,
  }) {
    return postJson(
      '/api/v1/vehicles',
      Vehicle.fromJson,
      data: {
        'brand': brand,
        'model': model,
        'registrationNumber': registrationNumber,
        'tdNumber': tdNumber,
        'vin': ?vin,
        'fuelType': fuelType,
        'status': status,
        'ownershipType': ownershipType,
        'registrationValidUntil': ?registrationValidUntil,
        'rentedUntil': ?rentedUntil,
        'technicalInspectionValidUntil': ?technicalInspectionValidUntil,
        'insuranceValidUntil': ?insuranceValidUntil,
        'nextServiceDue': ?nextServiceDue,
      },
    );
  }

  Future<Vehicle> update(
    String id, {
    required String brand,
    required String model,
    required String registrationNumber,
    required String tdNumber,
    String? vin,
    required String fuelType,
    required String status,
    required String ownershipType,
    String? registrationValidUntil,
    String? rentedUntil,
    String? technicalInspectionValidUntil,
    String? insuranceValidUntil,
    String? nextServiceDue,
    // What the form does not edit, handed back as it was: the update replaces the whole record.
    String? qrCode,
    String? gpsProvider,
    String? gpsTrackingUrl,
    String? branchId,
  }) {
    return putJson(
      '/api/v1/vehicles/$id',
      Vehicle.fromJson,
      data: {
        'brand': brand,
        'model': model,
        'registrationNumber': registrationNumber,
        'tdNumber': tdNumber,
        'vin': ?vin,
        'fuelType': fuelType,
        'status': status,
        'ownershipType': ownershipType,
        'registrationValidUntil': registrationValidUntil,
        'rentedUntil': rentedUntil,
        'technicalInspectionValidUntil': technicalInspectionValidUntil,
        'insuranceValidUntil': insuranceValidUntil,
        'nextServiceDue': nextServiceDue,
        'qrCode': ?qrCode,
        'gpsProvider': ?gpsProvider,
        'gpsTrackingUrl': ?gpsTrackingUrl,
        'branchId': ?branchId,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/vehicles/$id');
  }

  /// The vehicles signed out to the caller. Open to every employee, since a Worker is not served the
  /// fleet list but still needs the vehicle in their hands (to record its fuel).
  Future<List<Vehicle>> fetchMyVehicles() {
    return guard(() async {
      final response = await dio.get<List<dynamic>>('/api/v1/vehicle-fuel/mine');

      return [
        for (final json in response.data!) Vehicle.fromJson(json as Map<String, dynamic>),
      ];
    });
  }

  /// Looks a vehicle up by its QR label. Open to every authenticated
  /// employee, including roles without directory access.
  Future<Vehicle> fetchVehicleByQrCode(String qrCode) {
    return getJson(
      '/api/v1/vehicles/by-qr/${Uri.encodeComponent(qrCode)}',
      Vehicle.fromJson,
    );
  }

  /// Checks the vehicle out to the caller. Self-service only — the API
  /// always resolves the target employee from the caller's own session.
  Future<Vehicle> checkOutToMe(String id, {required String idempotencyKey}) {
    return postJson(
      '/api/v1/vehicles/$id/check-out-to-me',
      Vehicle.fromJson,
      idempotencyKey: idempotencyKey,
    );
  }

  /// Returns a vehicle that is currently checked out to the caller.
  Future<Vehicle> returnVehicle(String id, {required String idempotencyKey}) {
    return postJson(
      '/api/v1/vehicles/$id/return',
      Vehicle.fromJson,
      idempotencyKey: idempotencyKey,
    );
  }

  /// Hands the vehicle to another employee. Project Manager and above — the
  /// same rule the admin panel's assign action already enforces.
  Future<Vehicle> assignToEmployee(
    String id,
    String employeeId, {
    required String idempotencyKey,
  }) {
    return postJson(
      '/api/v1/vehicles/$id/assign/$employeeId',
      Vehicle.fromJson,
      idempotencyKey: idempotencyKey,
    );
  }

  /// Places the vehicle on a project. Any employee holding it keeps holding it.
  Future<Vehicle> assignToProject(
    String id,
    String projectId, {
    required String idempotencyKey,
  }) {
    return postJson(
      '/api/v1/vehicles/$id/assign-project/$projectId',
      Vehicle.fromJson,
      idempotencyKey: idempotencyKey,
    );
  }
}

final vehicleRepositoryProvider = Provider<VehicleRepository>((ref) {
  return VehicleRepository(ref.watch(apiClientProvider));
});
