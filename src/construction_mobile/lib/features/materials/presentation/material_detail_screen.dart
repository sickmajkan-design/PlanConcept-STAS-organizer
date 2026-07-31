import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../data/models/material.dart' as models;
import 'materials_controller.dart';
import 'materials_screen.dart' show formatQuantity;

class MaterialDetailScreen extends ConsumerWidget {
  const MaterialDetailScreen({super.key, required this.materialId});

  final String materialId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(materialDetailProvider(materialId));

    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.name ?? 'Material')),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(materialDetailProvider(materialId)),
          ),
          data: (material) => RefreshIndicator(
            onRefresh: () async =>
                ref.invalidate(materialDetailProvider(materialId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _Header(material: material),
                const SizedBox(height: 20),
                _Section(
                  title: 'Stock',
                  children: [
                    _InfoTile(
                      icon: Icons.warehouse_outlined,
                      label: 'Warehouse',
                      value: material.warehouse,
                    ),
                    _InfoTile(
                      icon: Icons.update_outlined,
                      label: 'Last updated',
                      value: formatDateTime(material.lastUpdated),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: 'Project',
                  children: [
                    if (material.isAssignedToProject)
                      ListTile(
                        leading: const Icon(Icons.apartment_outlined),
                        title: Text(material.projectName!),
                        onTap: () => context.push(
                          AppRoutes.projectDetail(material.projectId!),
                        ),
                      )
                    else
                      const ListTile(
                        leading: Icon(Icons.warehouse_outlined),
                        title: Text('Warehouse stock, not tied to a project'),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.material});

  final models.MaterialItem material;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            CircleAvatar(
              radius: 32,
              backgroundColor: theme.colorScheme.primaryContainer,
              child: Icon(
                Icons.inventory_2_outlined,
                size: 30,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    material.name,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${formatQuantity(material.quantity)} ${material.unit}',
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w700,
                      color: theme.colorScheme.primary,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Text(
            title,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ),
        Card(child: Column(children: children)),
      ],
    );
  }
}

class _InfoTile extends StatelessWidget {
  const _InfoTile({required this.icon, required this.label, this.value});

  final IconData icon;
  final String label;
  final String? value;

  @override
  Widget build(BuildContext context) {
    final hasValue = (value ?? '').trim().isNotEmpty;

    return ListTile(
      leading: Icon(icon),
      title: Text(label, style: Theme.of(context).textTheme.bodySmall),
      subtitle: Text(
        hasValue ? value! : '—',
        style: Theme.of(context).textTheme.bodyLarge,
      ),
      dense: true,
    );
  }
}
