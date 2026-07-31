import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/tool.dart';
import 'tools_controller.dart';

class ToolsScreen extends ConsumerWidget {
  const ToolsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(toolsControllerProvider.notifier);
    final state = ref.watch(toolsControllerProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tools'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code_scanner_outlined),
            tooltip: 'Look up by QR code',
            onPressed: () => context.push(AppRoutes.toolLookup),
          ),
        ],
      ),
      body: SafeArea(
        child: PagedListView<Tool>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: 'No tools match your search.',
          emptyIcon: Icons.handyman_outlined,
          header: ListSearchHeader(
            hintText: 'Name, category, serial number…',
            onSearchChanged: controller.search,
            filters: toolStatusFilters,
            selectedFilter: controller.statusFilter,
            onFilterSelected: controller.filterByStatus,
          ),
          itemBuilder: (context, tool) => _ToolCard(tool: tool),
        ),
      ),
    );
  }
}

class _ToolCard extends StatelessWidget {
  const _ToolCard({required this.tool});

  final Tool tool;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final subtitle = tool.assignedEmployeeName ??
        tool.assignedProjectName ??
        (tool.category ?? tool.serialNumber ?? 'No assignment');

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(AppRoutes.toolDetail(tool.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Icon(
                  Icons.handyman_outlined,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      tool.name,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              StatusChip(status: tool.status, dense: true),
            ],
          ),
        ),
      ),
    );
  }
}
