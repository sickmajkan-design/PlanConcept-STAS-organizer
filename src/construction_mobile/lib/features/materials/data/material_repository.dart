import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/material.dart';

class MaterialRepository extends ApiRepository {
  const MaterialRepository(super.dio);

  Future<PagedList<MaterialItem>> fetchMaterials({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool? unassignedOnly,
    String? sortBy,
    bool sortDescending = false,
  }) {
    return getPaged(
      '/api/v1/materials',
      MaterialItem.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        sortBy: sortBy,
        sortDescending: sortDescending,
        // Only ever sent to turn the filter on: false and null both mean
        // "no filter", which is the API's default.
        filters: {'unassignedOnly': unassignedOnly == true ? true : null},
      ),
    );
  }

  Future<MaterialItem> fetchMaterial(String id) {
    return getJson('/api/v1/materials/$id', MaterialItem.fromJson);
  }

  Future<MaterialItem> create({
    required String name,
    required String unit,
    required double quantity,
    String? warehouse,
    double? unitPrice,
    String? projectId,
  }) {
    return postJson(
      '/api/v1/materials',
      MaterialItem.fromJson,
      data: {
        'name': name,
        'unit': unit,
        'quantity': quantity,
        'warehouse': ?warehouse,
        'unitPrice': ?unitPrice,
        'projectId': ?projectId,
      },
    );
  }

  Future<MaterialItem> update(
    String id, {
    required String name,
    required String unit,
    required double quantity,
    String? warehouse,
    double? unitPrice,
    String? projectId,
  }) {
    return putJson(
      '/api/v1/materials/$id',
      MaterialItem.fromJson,
      data: {
        'name': name,
        'unit': unit,
        'quantity': quantity,
        'warehouse': ?warehouse,
        'unitPrice': ?unitPrice,
        'projectId': ?projectId,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/materials/$id');
  }
}

final materialRepositoryProvider = Provider<MaterialRepository>((ref) {
  return MaterialRepository(ref.watch(apiClientProvider));
});
