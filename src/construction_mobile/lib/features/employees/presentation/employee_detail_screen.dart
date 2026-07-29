import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
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
                  title: 'Contact',
                  children: [
                    _InfoTile(
                      icon: Icons.phone_outlined,
                      label: 'Phone',
                      value: employee.phone,
                    ),
                    _InfoTile(
                      icon: Icons.alternate_email,
                      label: 'Email',
                      value: employee.email,
                    ),
                    _InfoTile(
                      icon: Icons.home_outlined,
                      label: 'Address',
                      value: employee.address,
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: 'Employment',
                  children: [
                    _InfoTile(
                      icon: Icons.badge_outlined,
                      label: 'Employee number',
                      value: employee.employeeNumber,
                    ),
                    _InfoTile(
                      icon: Icons.work_outline,
                      label: 'Position',
                      value: employee.position,
                    ),
                    _InfoTile(
                      icon: Icons.event_available_outlined,
                      label: 'Employed since',
                      value: formatDate(employee.employmentDate),
                    ),
                    _InfoTile(
                      icon: Icons.cake_outlined,
                      label: 'Date of birth',
                      value: employee.dateOfBirth == null
                          ? null
                          : formatDate(employee.dateOfBirth),
                    ),
                    _InfoTile(
                      icon: Icons.account_circle_outlined,
                      label: 'App account',
                      value: employee.hasUserAccount ? 'Yes' : 'No',
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
                  StatusChip(status: employee.status),
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
      return const _Section(
        title: 'Projects',
        children: [
          ListTile(
            leading: Icon(Icons.work_off_outlined),
            title: Text('Not assigned to any project'),
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
            trailing: StatusChip(status: assignment.projectStatus, dense: true),
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
