import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'my_housing.dart';

class HousingRepository extends ApiRepository {
  const HousingRepository(super.dio);

  /// The caller's current or next stay; null when they have none (HTTP 204).
  Future<MyHousing?> fetchMine() {
    return guard(() async {
      final response = await dio.get<Map<String, dynamic>>('/api/v1/accommodations/mine');
      final data = response.data;

      return data == null || data.isEmpty ? null : MyHousing.fromJson(data);
    });
  }
}

final housingRepositoryProvider = Provider<HousingRepository>((ref) {
  return HousingRepository(ref.watch(apiClientProvider));
});

final myHousingProvider = FutureProvider.autoDispose<MyHousing?>((ref) {
  return ref.watch(housingRepositoryProvider).fetchMine();
});
