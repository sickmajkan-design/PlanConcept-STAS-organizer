import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../core/widgets/status_chip.dart';
import '../../../l10n/app_localizations.dart';
import '../data/models/weekly_site_report.dart';
import 'submit_weekly_report_sheet.dart';
import 'weekly_reports_controller.dart';

/// Where a foreman files the week's proof-of-work: signed hours, Aufmaß, or
/// anything else the office bills against — and, below the submit button,
/// what they have sent already. Full review still lives on the admin panel;
/// this list only answers "what did I send, and has anyone looked at it yet."
class WeeklyReportsScreen extends ConsumerWidget {
  const WeeklyReportsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final controller = ref.read(weeklyReportsControllerProvider.notifier);
    final state = ref.watch(weeklyReportsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.weeklyReportsTitle)),
      body: SafeArea(
        child: PagedListView<WeeklySiteReport>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.weeklyReportsHistoryEmpty,
          emptyIcon: Icons.assignment_turned_in_outlined,
          header: _SubmitHeader(l10n: l10n),
          itemBuilder: (context, report) => _WeeklyReportCard(report: report),
        ),
      ),
    );
  }
}

class _SubmitHeader extends ConsumerWidget {
  const _SubmitHeader({required this.l10n});

  final AppLocalizations l10n;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.weeklyReportsDescription,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: () => showSubmitWeeklyReportSheet(context, ref),
              icon: const Icon(Icons.upload_file_outlined),
              label: Text(l10n.weeklyReportsSubmit),
            ),
          ),
          const SizedBox(height: 20),
          Text(l10n.weeklyReportsHistoryTitle, style: theme.textTheme.titleMedium),
        ],
      ),
    );
  }
}

class _WeeklyReportCard extends StatelessWidget {
  const _WeeklyReportCard({required this.report});

  final WeeklySiteReport report;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = context.l10n;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    report.projectName,
                    style: theme.textTheme.titleSmall
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
                StatusChip(
                  status: report.status,
                  kind: EnumKind.weeklyReportStatus,
                  dense: true,
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              l10n.weeklyReportsIsoWeekLabel(report.isoWeek, report.isoYear),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(
                  Icons.description_outlined,
                  size: 16,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    '${enumLabel(l10n, EnumKind.weeklyReportType, report.type)} · '
                    '${report.fileName}',
                    style: theme.textTheme.bodySmall,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
            if (report.note != null && report.note!.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(report.note!, style: theme.textTheme.bodySmall),
            ],
            const SizedBox(height: 8),
            Text(
              report.processedAt != null
                  ? l10n.weeklyReportsProcessedOn(formatDate(report.processedAt))
                  : formatDateTime(report.createdAt),
              style: theme.textTheme.labelSmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }
}
