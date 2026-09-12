import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/time_entry.dart';
import '../data/time_entry_repository.dart';

final teamTodayProvider =
    FutureProvider.autoDispose<List<TimeEntry>>((ref) {
  return ref.watch(timeEntryRepositoryProvider).fetchTeamToday();
});

/// Who clocked in/out today, on a Foreman's own site(s) — read-only, so they
/// can check the app's record against whatever they track on paper. Approving
/// hours is a separate, stricter screen (desktop only, for now) — this is
/// just visibility.
class TeamTodayScreen extends ConsumerWidget {
  const TeamTodayScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final entries = ref.watch(teamTodayProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.teamTodayTitle)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => ref.refresh(teamTodayProvider.future),
          child: entries.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => ListView(
              children: [
                SizedBox(
                  height: MediaQuery.of(context).size.height * 0.7,
                  child: FailureView(
                    error: error,
                    onRetry: () => ref.invalidate(teamTodayProvider),
                  ),
                ),
              ],
            ),
            data: (rows) => rows.isEmpty
                ? ListView(
                    children: [
                      SizedBox(
                        height: MediaQuery.of(context).size.height * 0.7,
                        child: EmptyView(
                          message: l10n.teamTodayEmpty,
                          icon: Icons.groups_outlined,
                        ),
                      ),
                    ],
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: rows.length,
                    itemBuilder: (context, index) =>
                        _TeamEntryCard(entry: rows[index]),
                  ),
          ),
        ),
      ),
    );
  }
}

class _TeamEntryCard extends StatelessWidget {
  const _TeamEntryCard({required this.entry});

  final TimeEntry entry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = context.l10n;

    final timeRange = entry.isRunning
        ? l10n.teamTodayStillWorking(formatTime(entry.startedAt))
        : '${formatTime(entry.startedAt)} – ${formatTime(entry.endedAt)}';

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
                    entry.employeeName,
                    style: theme.textTheme.titleSmall
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
                if (entry.needsAttention)
                  Icon(
                    Icons.flag_outlined,
                    size: 18,
                    color: theme.colorScheme.error,
                  ),
                const SizedBox(width: 6),
                StatusChip(
                  status: entry.status,
                  kind: EnumKind.timeEntryStatus,
                  dense: true,
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              entry.projectName ?? l10n.workItemsNoProject,
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(
                  Icons.schedule_outlined,
                  size: 16,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 6),
                Text(timeRange, style: theme.textTheme.bodySmall),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
