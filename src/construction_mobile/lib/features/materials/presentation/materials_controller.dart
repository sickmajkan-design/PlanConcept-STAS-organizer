import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/material_repository.dart';
import '../data/models/material.dart';

/// Single toggle offered as a filter chip: warehouse stock not tied to any
/// project.
const materialWarehouseOnlyFilter = 'Warehouse stock only';

class MaterialsController extends PagedListNotifier<MaterialItem> {
  bool _warehouseOnly = false;

  String? get selectedFilter =>
      _warehouseOnly ? materialWarehouseOnlyFilter : null;

  @override
  Future<PagedList<MaterialItem>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(materialRepositoryProvider).fetchMaterials(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          unassignedOnly: _warehouseOnly ? true : null,
        );
  }

  void toggleWarehouseOnly(String? filter) {
    final next = filter == materialWarehouseOnlyFilter;

    if (next == _warehouseOnly) {
      return;
    }

    _warehouseOnly = next;
    ref.invalidateSelf();
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
