import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/work_item.dart';

class WorkItemRepository extends ApiRepository {
  const WorkItemRepository(super.dio);

  /// The caller's own work. The API narrows only a Worker to their own rows, so the employee is
  /// named: a foreman or manager would otherwise see every open item as theirs.
  Future<PagedList<WorkItem>> fetchMine({
    String? assignedEmployeeId,
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool openOnly = true,
  }) {
    return getPaged(
      '/api/v1/workitems',
      WorkItem.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        filters: {
          'openOnly': openOnly ? true : null,
          'assignedEmployeeId': assignedEmployeeId,
        },
      ),
    );
  }

  Future<WorkItem> changeStatus(String id, String status) {
    return postJson(
      '/api/v1/workitems/$id/status',
      WorkItem.fromJson,
      data: <String, dynamic>{'status': status},
    );
  }

  /// Reports a defect from site. The only kind a Worker may raise.
  Future<WorkItem> reportDefect({
    required String projectId,
    required String title,
    String? description,
    double? latitude,
    double? longitude,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/workitems',
      WorkItem.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'kind': 'Defect',
        'title': title,
        'projectId': projectId,
        'description': ?description,
        'priority': 'Normal',
        // Sent only as a pair: the database refuses half a position.
        if (latitude != null && longitude != null) ...<String, dynamic>{
          'latitude': latitude,
          'longitude': longitude,
        },
      },
    );
  }
}

final workItemRepositoryProvider = Provider<WorkItemRepository>((ref) {
  return WorkItemRepository(ref.watch(apiClientProvider));
});
