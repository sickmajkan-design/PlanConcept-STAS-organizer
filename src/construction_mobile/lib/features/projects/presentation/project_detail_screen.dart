import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../attachments/presentation/add_site_photo.dart';
import '../../attachments/presentation/attachment_section.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/project.dart';
import 'projects_controller.dart';

class ProjectDetailScreen extends ConsumerWidget {
  const ProjectDetailScreen({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(projectDetailProvider(projectId));

    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.name ?? 'Project')),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(projectDetailProvider(projectId)),
          ),
          data: (project) => RefreshIndicator(
            onRefresh: () async =>
                ref.invalidate(projectDetailProvider(projectId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _Header(project: project),
                if ((project.description ?? '').isNotEmpty) ...[
                  const SizedBox(height: 20),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Text(project.description!),
                    ),
                  ),
                ],
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.commonDetails,
                  children: [
                    InfoTile(
                      icon: Icons.business_outlined,
                      label: context.l10n.projectClient,
                      value: project.client,
                    ),
                    InfoTile(
                      icon: Icons.place_outlined,
                      label: context.l10n.employeeAddress,
                      value: project.address,
                    ),
                    InfoTile(
                      icon: Icons.my_location,
                      label: context.l10n.projectCoordinates,
                      value: project.hasCoordinates
                          ? '${project.latitude!.toStringAsFixed(5)}, '
                              '${project.longitude!.toStringAsFixed(5)}'
                          : null,
                    ),
                    InfoTile(
                      icon: Icons.play_circle_outline,
                      label: context.l10n.projectStartDate,
                      value: project.startDate == null
                          ? null
                          : formatDate(project.startDate),
                    ),
                    InfoTile(
                      icon: Icons.flag_outlined,
                      label: context.l10n.projectEndDate,
                      value: project.endDate == null
                          ? null
                          : formatDate(project.endDate),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.attachmentsTitle,
                  children: [
                    AttachmentSection(
                      ownerType: 'Project',
                      ownerId: project.id,
                    ),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(8, 0, 8, 8),
                      child: Align(
                        alignment: Alignment.centerLeft,
                        child: AddSitePhotoButton(projectId: project.id),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _CrewSection(employees: project.employees),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.project});

  final ProjectDetail project;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              project.name,
              style: theme.textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                StatusChip(status: project.status, kind: EnumKind.projectStatus),
                const SizedBox(width: 12),
                Icon(
                  Icons.people_outline,
                  size: 18,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 4),
                Text(
                  '${project.employeeCount} assigned',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CrewSection extends StatelessWidget {
  const _CrewSection({required this.employees});

  final List<ProjectEmployee> employees;

  @override
  Widget build(BuildContext context) {
    if (employees.isEmpty) {
      return _Section(
        title: 'Crew',
        children: [
          ListTile(
            leading: Icon(Icons.person_off_outlined),
            title: Text(context.l10n.projectCrewEmpty),
          ),
        ],
      );
    }

    return _Section(
      title: 'Crew (${employees.length})',
      children: [
        for (final member in employees)
          ListTile(
            leading: CircleAvatar(
              backgroundColor:
                  Theme.of(context).colorScheme.surfaceContainerHighest,
              child: Text(
                initialsOf(member.fullName.split(' ').firstOrNull,
                    member.fullName.split(' ').lastOrNull),
                style: Theme.of(context).textTheme.labelLarge,
              ),
            ),
            title: Text(member.fullName),
            subtitle: Text('${member.position} · ${member.employeeNumber}'),
            trailing: StatusChip(status: member.status, kind: EnumKind.employeeStatus, dense: true),
            onTap: () =>
                context.push(AppRoutes.employeeDetail(member.employeeId)),
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
