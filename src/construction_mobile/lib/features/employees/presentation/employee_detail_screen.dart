import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../attachments/presentation/attachment_section.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/employee.dart';
import 'employees_controller.dart';

class EmployeeDetailScreen extends ConsumerWidget {
  const EmployeeDetailScreen({super.key, required this.employeeId});

  final String employeeId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(employeeDetailProvider(employeeId));

    return Scaffold(
      appBar: AppBar(
        title: Text(detail.value?.fullName ?? 'Employee'),
      ),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(employeeDetailProvider(employeeId)),
          ),
          data: (employee) => RefreshIndicator(
            onRefresh: () async =>
                ref.invalidate(employeeDetailProvider(employeeId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _Header(employee: employee),
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.commonContact,
                  children: [
                    InfoTile(
                      icon: Icons.phone_outlined,
                      label: context.l10n.employeePhone,
                      value: employee.phone,
                    ),
                    InfoTile(
                      icon: Icons.alternate_email,
                      label: context.l10n.authEmail,
                      value: employee.email,
                    ),
                    InfoTile(
                      icon: Icons.home_outlined,
                      label: context.l10n.employeeAddress,
                      value: employee.address,
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.commonEmployment,
                  children: [
                    InfoTile(
                      icon: Icons.badge_outlined,
                      label: context.l10n.employeeNumber,
                      value: employee.employeeNumber,
                    ),
                    InfoTile(
                      icon: Icons.work_outline,
                      label: context.l10n.employeePosition,
                      value: employee.position,
                    ),
                    InfoTile(
                      icon: Icons.event_available_outlined,
                      label: context.l10n.employeeEmployedSince,
                      value: formatDate(employee.employmentDate),
                    ),
                    InfoTile(
                      icon: Icons.cake_outlined,
                      label: context.l10n.employeeDateOfBirth,
                      value: employee.dateOfBirth == null
                          ? null
                          : formatDate(employee.dateOfBirth),
                    ),
                    InfoTile(
                      icon: Icons.account_circle_outlined,
                      label: context.l10n.employeeAppAccount,
                      value: employee.hasUserAccount ? 'Yes' : 'No',
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                // Read-only: the API serves an employee their own documents
                // and refuses everyone else's below Admin, so this is either
                // the signed-in worker's own file or an administrator looking.
                _Section(
                  title: context.l10n.attachmentsTitle,
                  children: [
                    AttachmentSection(
                      ownerType: 'Employee',
                      ownerId: employee.id,
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _ProjectsSection(projects: employee.projects),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.employee});

  final EmployeeDetail employee;

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
              child: Text(
                employee.initials,
                style: theme.textTheme.titleLarge?.copyWith(
                  color: theme.colorScheme.onPrimaryContainer,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    employee.fullName,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    employee.position,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 10),
                  StatusChip(status: employee.status, kind: EnumKind.employeeStatus),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProjectsSection extends StatelessWidget {
  const _ProjectsSection({required this.projects});

  final List<EmployeeProjectAssignment> projects;

  @override
  Widget build(BuildContext context) {
    if (projects.isEmpty) {
      return _Section(
        title: context.l10n.navProjects,
        children: [
          ListTile(
            leading: Icon(Icons.work_off_outlined),
            title: Text(context.l10n.toolNotOnProject),
          ),
        ],
      );
    }

    return _Section(
      title: 'Projects (${projects.length})',
      children: [
        for (final assignment in projects)
          ListTile(
            leading: const Icon(Icons.apartment),
            title: Text(assignment.projectName),
            subtitle: Text('Assigned ${formatDate(assignment.assignedAt)}'),
            trailing: StatusChip(status: assignment.projectStatus, kind: EnumKind.projectStatus, dense: true),
            onTap: () =>
                context.push(AppRoutes.projectDetail(assignment.projectId)),
          ),
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
