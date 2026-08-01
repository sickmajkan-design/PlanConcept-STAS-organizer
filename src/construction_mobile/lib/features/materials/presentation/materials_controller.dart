import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/material_repository.dart';
import '../data/models/material.dart';

/// Single toggle offered as a filter chip: warehouse stock not tied to any
/// project.
const materialWarehouseOnlyFilter = 'Warehouse stock only';

class MaterialsController extends FilteredPagedListNotifier<MaterialItem> {
  @override
  Future<PagedList<MaterialItem>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(materialRepositoryProvider).fetchMaterials(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          // The one chip is an on/off toggle: selected means "warehouse stock
          // only", anything else means no filter, which is the API's default.
          unassignedOnly:
              filter == materialWarehouseOnlyFilter ? true : null,
        );
  }
}

final materialsControllerProvider =
    AsyncNotifierProvider<MaterialsController, PagedState<MaterialItem>>(
  MaterialsController.new,
);

final materialDetailProvider =
    FutureProvider.autoDispose.family<MaterialItem, String>((ref, id) {
  return ref.watch(materialRepositoryProvider).fetchMaterial(id);
});
