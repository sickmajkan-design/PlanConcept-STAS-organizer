import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/tool.dart';
import 'tools_controller.dart';

class ToolDetailScreen extends ConsumerWidget {
  const ToolDetailScreen({super.key, required this.toolId});

  final String toolId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(toolDetailProvider(toolId));

    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.name ?? 'Tool')),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(toolDetailProvider(toolId)),
          ),
          data: (tool) => RefreshIndicator(
            onRefresh: () async => ref.invalidate(toolDetailProvider(toolId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _Header(tool: tool),
                const SizedBox(height: 20),
                _ToolInfo(tool: tool),
                const SizedBox(height: 20),
                _AssignmentSection(tool: tool),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.tool});

  final Tool tool;

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
                Icons.handyman_outlined,
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
                    tool.name,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    tool.category ?? 'Uncategorised',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 10),
                  StatusChip(status: tool.status, kind: EnumKind.toolStatus),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ToolInfo extends StatelessWidget {
  const _ToolInfo({required this.tool});

  final Tool tool;

  @override
  Widget build(BuildContext context) {
    return _Section(
      title: 'Tool',
      children: [
        InfoTile(
          icon: Icons.confirmation_number_outlined,
          label: context.l10n.toolSerialNumber,
          value: tool.serialNumber,
        ),
        InfoTile(
          icon: Icons.qr_code_outlined,
          label: context.l10n.toolQrCode,
          value: tool.qrCode,
        ),
      ],
    );
  }
}

class _AssignmentSection extends StatelessWidget {
  const _AssignmentSection({required this.tool});

  final Tool tool;

  @override
  Widget build(BuildContext context) {
    return _Section(
      title: context.l10n.commonAssignment,
      children: [
        if (tool.isAssignedToEmployee)
          ListTile(
            leading: const Icon(Icons.person_outline),
            title: Text(tool.assignedEmployeeName!),
            subtitle: Text(tool.assignedEmployeeNumber ?? ''),
            onTap: () => context.push(
              AppRoutes.employeeDetail(tool.assignedEmployeeId!),
            ),
          )
        else
          ListTile(
            leading: const Icon(Icons.person_off_outlined),
            title: Text(context.l10n.toolNotHeld),
          ),
        if (tool.isAssignedToProject) ...[
          const Divider(height: 1, indent: 20, endIndent: 20),
          ListTile(
            leading: const Icon(Icons.apartment_outlined),
            title: Text(tool.assignedProjectName!),
            onTap: () => context.push(
              AppRoutes.projectDetail(tool.assignedProjectId!),
            ),
          ),
        ],
      ],
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
