import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle.dart';

class VehicleRepository {
  VehicleRepository(this._dio);

  final Dio _dio;

  Future<PagedList<Vehicle>> fetchVehicles({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? sortBy,
    bool sortDescending = false,
  }) async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/vehicles',
        queryParameters: <String, dynamic>{
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          'status': ?status,
          'sortBy': ?sortBy,
          if (sortDescending) 'sortDescending': true,
        },
      );

      return PagedList<Vehicle>.fromJson(response.data!, Vehicle.fromJson);
    });
  }

  Future<Vehicle> fetchVehicle(String id) async {
    return _guard(() async {
      final response =
          await _dio.get<Map<String, dynamic>>('/api/vehicles/$id');

      return Vehicle.fromJson(response.data!);
    });
  }

  Future<T> _guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }
}

final vehicleRepositoryProvider = Provider<VehicleRepository>((ref) {
  return VehicleRepository(ref.watch(apiClientProvider));
});
