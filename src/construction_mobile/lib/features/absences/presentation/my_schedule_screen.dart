import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../data/models/schedule.dart';
import 'my_schedule_controller.dart';

/// Where this employee is posted over the next fortnight, and when they are
/// away.
///
/// A list rather than the admin panel's grid: on a phone the question is
/// "where am I on Thursday", and a seven-column board answers it worse than a
/// line of text does.
class MyScheduleScreen extends ConsumerWidget {
  const MyScheduleScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final state = ref.watch(myScheduleControllerProvider);
    final controller = ref.read(myScheduleControllerProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.scheduleTitle),
        actions: [
          IconButton(
            icon: const Icon(Icons.event_busy_outlined),
            tooltip: l10n.navAbsences,
            onPressed: () => context.push(AppRoutes.absences),
          ),
        ],
      ),
      body: SafeArea(
        child: state.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) =>
              FailureView(error: error, onRetry: controller.refresh),
          data: (schedule) => RefreshIndicator(
            onRefresh: controller.refresh,
            child: _ScheduleBody(row: schedule.mine),
          ),
        ),
      ),
    );
  }
}

class _ScheduleBody extends StatelessWidget {
  const _ScheduleBody({required this.row});

  final ScheduleRow? row;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    if (row == null || row!.isEmpty) {
      // Still scrollable, so pull-to-refresh works on an empty schedule.
      return ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 48),
          Icon(
            Icons.event_available_outlined,
            size: 40,
            color: Theme.of(context).colorScheme.onSurfaceVariant,
          ),
          const SizedBox(height: 12),
          Text(
            l10n.scheduleEmpty,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ],
      );
    }

    return ListView(
      padding: const EdgeInsets.symmetric(vertical: 8),
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 4),
          child: Text(
            l10n.scheduleUpcoming,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ),
        for (final assignment in row!.assignments)
          _PostingCard(assignment: assignment),
        for (final absence in row!.absences) _AwayCard(absence: absence),
      ],
    );
  }
}

class _PostingCard extends StatelessWidget {
  const _PostingCard({required this.assignment});

  final ScheduleAssignment assignment;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        leading: Icon(
          Icons.apartment_outlined,
          color: theme.colorScheme.primary,
        ),
        title: Text(
          assignment.projectName,
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        subtitle: Text(
          _rangeLabel(
            context,
            from: assignment.start,
            to: assignment.end,
            suffix: assignment.continuesAfter ? l10n.scheduleContinues : null,
          ),
        ),
        trailing: Chip(
          label: Text(l10n.scheduleOnSite),
          visualDensity: VisualDensity.compact,
        ),
      ),
    );
  }
}

class _AwayCard extends StatelessWidget {
  const _AwayCard({required this.absence});

  final ScheduleAbsence absence;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      color: theme.colorScheme.surfaceContainerHighest,
      child: ListTile(
        leading: Icon(
          Icons.event_busy_outlined,
          color: theme.colorScheme.onSurfaceVariant,
        ),
        title: Text(
          enumLabel(l10n, EnumKind.absenceType, absence.type),
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        subtitle: Text(
          _rangeLabel(context, from: absence.start, to: absence.end),
        ),
        trailing: Chip(
          label: Text(l10n.scheduleAway),
          visualDensity: VisualDensity.compact,
        ),
      ),
    );
  }
}

/// "03.08.2026. – 07.08.2026.", collapsed to one date when it is a single day.
String _rangeLabel(
  BuildContext context, {
  required DateTime? from,
  required DateTime? to,
  String? suffix,
}) {
  final l10n = context.l10n;
  final start = formatDate(from);
  final label = from != null && to != null && from.isAtSameMomentAs(to)
      ? start
      : l10n.scheduleDateRange(start, formatDate(to));

  return suffix == null ? label : '$label · $suffix';
}
