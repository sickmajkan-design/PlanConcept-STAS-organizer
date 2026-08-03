import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/employee.dart';
import 'employees_controller.dart';

class EmployeesScreen extends ConsumerWidget {
  const EmployeesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(employeesControllerProvider.notifier);
    final state = ref.watch(employeesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.navEmployees)),
      body: SafeArea(
        child: PagedListView<Employee>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: context.l10n.employeesEmpty,
          emptyIcon: Icons.people_outline,
          header: ListSearchHeader(
            hintText: context.l10n.employeesSearchHint,
            onSearchChanged: controller.search,
            filters: employeeStatusFilters,
            selectedFilter: controller.filter,
            onFilterSelected: controller.applyFilter,
          ),
          itemBuilder: (context, employee) => _EmployeeCard(employee: employee),
        ),
      ),
    );
  }
}

class _EmployeeCard extends StatelessWidget {
  const _EmployeeCard({required this.employee});

  final Employee employee;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(AppRoutes.employeeDetail(employee.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Text(
                  employee.initials,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: theme.colorScheme.onPrimaryContainer,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      employee.fullName,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${employee.position} · ${employee.employeeNumber}',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              StatusChip(status: employee.status, kind: EnumKind.employeeStatus, dense: true),
            ],
          ),
        ),
      ),
    );
  }
}
