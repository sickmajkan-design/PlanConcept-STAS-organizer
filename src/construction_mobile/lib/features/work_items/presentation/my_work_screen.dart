import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import 'package:image_picker/image_picker.dart';

import '../../attachments/data/attachment_repository.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../data/models/work_item.dart';
import 'my_work_controller.dart';

/// What this employee has to do, and the two buttons for moving it on.
class MyWorkScreen extends ConsumerWidget {
  const MyWorkScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(myWorkControllerProvider.notifier);
    final state = ref.watch(myWorkControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.navWorkItems)),
      body: SafeArea(
        child: PagedListView<WorkItem>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: context.l10n.workItemsEmpty,
          emptyIcon: Icons.checklist_outlined,
          header: ListSearchHeader(
            hintText: context.l10n.workItemsIncludeFinished,
            onSearchChanged: controller.search,
            filters: const [workIncludeFinishedFilter],
            selectedFilter: controller.filter,
            onFilterSelected: controller.applyFilter,
          ),
          itemBuilder: (context, item) => _WorkItemCard(item: item),
        ),
      ),
    );
  }
}

class _WorkItemCard extends ConsumerWidget {
  const _WorkItemCard({required this.item});

  final WorkItem item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final overdue = item.isOverdueOn(DateTime.now());

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  item.isDefect ? Icons.report_problem_outlined : Icons.check_circle_outline,
                  size: 20,
                  color: item.isDefect
                      ? theme.colorScheme.error
                      : theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    item.title,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
            if ((item.description ?? '').isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                item.description!,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: [
                Chip(
                  label: Text(
                    enumLabel(l10n, EnumKind.workItemStatus, item.status),
                  ),
                  visualDensity: VisualDensity.compact,
                ),
                Chip(
                  label: Text(item.projectName ?? l10n.workItemsNoProject),
                  visualDensity: VisualDensity.compact,
                ),
                if (item.due != null)
                  Chip(
                    label: Text(
                      overdue
                          ? l10n.workItemsOverdue
                          : l10n.workItemsDue(formatDate(item.due)),
                    ),
                    backgroundColor:
                        overdue ? theme.colorScheme.errorContainer : null,
                    visualDensity: VisualDensity.compact,
                  ),
                if (item.attachmentCount > 0)
                  Chip(
                    label: Text(l10n.workItemsPhotoCount(item.attachmentCount)),
                    visualDensity: VisualDensity.compact,
                  ),
              ],
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              children: [
                // Available on anything of theirs, not only on a defect they
                // raised: a photograph of the work as it stands is the
                // cheapest progress report there is.
                OutlinedButton.icon(
                  onPressed: () => _addPhoto(context, ref),
                  icon: const Icon(Icons.photo_camera_outlined, size: 18),
                  label: Text(l10n.workItemsAddPhoto),
                ),
              ],
            ),
            if (item.nextStates.isNotEmpty) ...[
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                children: [
                  for (final next in item.nextStates)
                    OutlinedButton(
                      onPressed: () => _move(context, ref, next),
                      child: Text(
                        enumLabel(l10n, EnumKind.workItemStatus, next),
                      ),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  /// Photographs this item. The camera first, the gallery as a fallback.
  Future<void> _addPhoto(BuildContext context, WidgetRef ref) async {
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final picker = ImagePicker();

    XFile? picked;

    try {
      picked = await picker.pickImage(
        source: ImageSource.camera,
        maxWidth: 1920,
        imageQuality: 85,
      );
    } on Exception {
      picked = await picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1920,
        imageQuality: 85,
      );
    }

    if (picked == null) {
      return;
    }

    try {
      await ref.read(attachmentRepositoryProvider).uploadPhoto(
            ownerType: 'WorkItem',
            ownerId: item.id,
            filePath: picked.path,
            fileName: picked.name,
          );

      // Reloads so the photo count on the card matches what was just added.
      await ref.read(myWorkControllerProvider.notifier).refresh();

      messenger.showSnackBar(SnackBar(content: Text(l10n.attachmentsUploaded)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }

  Future<void> _move(BuildContext context, WidgetRef ref, String status) async {
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final messenger = ScaffoldMessenger.of(context);
    final l10n = context.l10n;

    try {
      await ref.read(myWorkControllerProvider.notifier).changeStatus(item, status);
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}
