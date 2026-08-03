import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/absence.dart';
import 'my_absences_controller.dart';
import 'request_absence_sheet.dart';

/// The employee's own time off: what they asked for, and what came back.
class MyAbsencesScreen extends ConsumerWidget {
  const MyAbsencesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final controller = ref.read(myAbsencesControllerProvider.notifier);
    final state = ref.watch(myAbsencesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.absencesTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => showRequestAbsenceSheet(context, ref),
        icon: const Icon(Icons.add),
        label: Text(l10n.absencesRequest),
      ),
      body: SafeArea(
        child: PagedListView<Absence>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.absencesEmpty,
          emptyIcon: Icons.event_busy_outlined,
          // A chip row without a search box: the endpoint has no text search,
          // and offering one that does nothing is worse than offering none.
          header: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
            child: Align(
              alignment: Alignment.centerLeft,
              child: FilterChip(
                label: Text(l10n.absencesPendingOnly),
                selected: controller.filter == absencePendingFilter,
                onSelected: (selected) => controller
                    .applyFilter(selected ? absencePendingFilter : null),
              ),
            ),
          ),
          itemBuilder: (context, absence) => _AbsenceCard(absence: absence),
        ),
      ),
    );
  }
}

class _AbsenceCard extends ConsumerWidget {
  const _AbsenceCard({required this.absence});

  final Absence absence;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    enumLabel(l10n, EnumKind.absenceType, absence.type),
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                Chip(
                  label: Text(
                    enumLabel(l10n, EnumKind.absenceStatus, absence.status),
                  ),
                  backgroundColor: switch (absence.status) {
                    'Approved' => theme.colorScheme.primaryContainer,
                    'Rejected' => theme.colorScheme.errorContainer,
                    _ => null,
                  },
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            const SizedBox(height: 6),
            Text(
              '${formatDate(absence.start)} – ${formatDate(absence.end)}',
              style: theme.textTheme.bodyMedium,
            ),
            Text(
              l10n.absencesDayCount(absence.dayCount),
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            if ((absence.reason ?? '').isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(
                absence.reason!,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            // Why it was refused matters more than that it was, so the note is
            // shown on the card rather than behind a tap.
            if ((absence.reviewNote ?? '').isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (absence.reviewedByName != null)
                      Text(
                        l10n.absencesAnsweredBy(absence.reviewedByName!),
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    Text(absence.reviewNote!, style: theme.textTheme.bodySmall),
                  ],
                ),
              ),
            ],
            if (absence.canWithdraw) ...[
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: () => _confirmWithdraw(context, ref),
                  icon: const Icon(Icons.undo, size: 18),
                  label: Text(l10n.absencesWithdraw),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _confirmWithdraw(BuildContext context, WidgetRef ref) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.absencesWithdrawTitle),
        content: Text(l10n.absencesWithdrawBody),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(l10n.absencesWithdraw),
          ),
        ],
      ),
    );

    if (confirmed != true) {
      return;
    }

    try {
      await ref.read(myAbsencesControllerProvider.notifier).withdraw(absence);
      messenger.showSnackBar(SnackBar(content: Text(l10n.absencesWithdrawn)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.message)));
    }
  }
}
