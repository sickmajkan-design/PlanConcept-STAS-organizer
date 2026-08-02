import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/material.dart';
import 'materials_controller.dart';

/// Trims trailing zeros from a stock quantity, e.g. `12.500` -> `12.5`,
/// `40.000` -> `40`.
String formatQuantity(double value) {
  var text = value.toStringAsFixed(3);
  text = text.replaceFirst(RegExp(r'0+$'), '');
  text = text.replaceFirst(RegExp(r'\.$'), '');
  return text;
}

class MaterialsScreen extends ConsumerWidget {
  const MaterialsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(materialsControllerProvider.notifier);
    final state = ref.watch(materialsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.navMaterials)),
      body: SafeArea(
        child: PagedListView<MaterialItem>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: 'No materials match your search.',
          emptyIcon: Icons.inventory_2_outlined,
          header: ListSearchHeader(
            hintText: context.l10n.materialsSearchHint,
            onSearchChanged: controller.search,
            filters: const [materialWarehouseOnlyFilter],
            selectedFilter: controller.filter,
            onFilterSelected: controller.applyFilter,
          ),
          itemBuilder: (context, material) => _MaterialCard(material: material),
        ),
      ),
    );
  }
}

class _MaterialCard extends StatelessWidget {
  const _MaterialCard({required this.material});

  final MaterialItem material;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(AppRoutes.materialDetail(material.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Icon(
                  Icons.inventory_2_outlined,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      material.name,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      material.isAssignedToProject
                          ? material.projectName!
                          : (material.warehouse ?? 'Warehouse stock'),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Text(
                '${formatQuantity(material.quantity)} ${material.unit}',
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
