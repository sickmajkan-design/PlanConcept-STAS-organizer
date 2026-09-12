import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/confirm_dialog.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/material_repository.dart';
import '../data/models/material.dart' as models;
import 'material_form_sheet.dart';
import 'materials_controller.dart';

class MaterialDetailScreen extends ConsumerWidget {
  const MaterialDetailScreen({super.key, required this.materialId});

  final String materialId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(materialDetailProvider(materialId));
    final user = ref.watch(currentUserProvider);
    final canEdit = user?.isProjectManagerAndAbove ?? false;
    final canDelete = user?.isAdminAndAbove ?? false;
    final loadedMaterial = detail.value;

    return Scaffold(
      appBar: AppBar(
        title: Text(loadedMaterial?.name ?? context.l10n.commonMaterial),
        actions: [
          if (loadedMaterial != null) ...[
            if (canEdit)
              IconButton(
                icon: const Icon(Icons.edit_outlined),
                tooltip: context.l10n.commonEdit,
                onPressed: () => showMaterialFormSheet(
                  context,
                  ref,
                  existing: loadedMaterial,
                ),
              ),
            if (canDelete)
              PopupMenuButton<void>(
                itemBuilder: (context) => [
                  PopupMenuItem(
                    onTap: () => Future.microtask(
                      () => _deleteMaterial(context, ref, loadedMaterial),
                    ),
                    child: Text(
                      context.l10n.commonDelete,
                      style:
                          TextStyle(color: Theme.of(context).colorScheme.error),
                    ),
                  ),
                ],
              ),
          ],
        ],
      ),
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
                  title: context.l10n.materialStock,
                  children: [
                    InfoTile(
                      icon: Icons.warehouse_outlined,
                      label: context.l10n.materialWarehouse,
                      value: material.warehouse,
                    ),
                    InfoTile(
                      icon: Icons.update_outlined,
                      label: context.l10n.materialLastUpdated,
                      value: formatDateTime(material.lastUpdated),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.commonProject,
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
                      ListTile(
                        leading: Icon(Icons.warehouse_outlined),
                        title: Text(context.l10n.materialWarehouseNote),
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

Future<void> _deleteMaterial(
  BuildContext context,
  WidgetRef ref,
  models.MaterialItem material,
) async {
  final l10n = context.l10n;

  final confirmed = await showConfirmDialog(
    context,
    title: l10n.materialDeleteTitle,
    body: l10n.materialDeleteBody(material.name),
    destructive: true,
  );

  if (!confirmed || !context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);
  final router = GoRouter.of(context);

  try {
    await ref.read(materialRepositoryProvider).remove(material.id);
    await ref.read(materialsControllerProvider.notifier).refresh();

    if (router.canPop()) {
      router.pop();
    }
  } on ApiException catch (exception) {
    messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
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
