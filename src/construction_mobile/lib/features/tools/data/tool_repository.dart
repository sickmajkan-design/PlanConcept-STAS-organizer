import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/tool.dart';

class ToolRepository extends ApiRepository {
  const ToolRepository(super.dio);

  Future<PagedList<Tool>> fetchTools({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? sortBy,
    bool sortDescending = false,
  }) {
    return getPaged(
      '/api/v1/tools',
      Tool.fromJson,
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

  Future<Tool> fetchTool(String id) {
    return getJson('/api/v1/tools/$id', Tool.fromJson);
  }

  /// Looks a tool up by its QR label. Open to every authenticated employee,
  /// including roles without directory access.
  Future<Tool> fetchToolByQrCode(String qrCode) {
    return getJson(
      '/api/v1/tools/by-qr/${Uri.encodeComponent(qrCode)}',
      Tool.fromJson,
    );
  }
}

final toolRepositoryProvider = Provider<ToolRepository>((ref) {
  return ToolRepository(ref.watch(apiClientProvider));
});
