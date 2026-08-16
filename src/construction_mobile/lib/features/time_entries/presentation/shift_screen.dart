import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/time_entry.dart';
import 'my_time_entries_controller.dart';
import 'shift_controller.dart';

/// Clock in, clock out, and the entries this employee has recorded.
class ShiftScreen extends ConsumerWidget {
  const ShiftScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider);
    final l10n = context.l10n;

    if (user != null && !user.isEmployee) {
      // An admin account has no employee record, so the API would refuse
      // every call this screen makes. Saying so beats a wall of 403s.
      return Scaffold(
        appBar: AppBar(title: Text(l10n.shiftTitle)),
        body: Padding(
          padding: const EdgeInsets.all(24),
          child: Center(child: Text(l10n.shiftNotAnEmployee)),
        ),
      );
    }

    final controller = ref.read(myTimeEntriesControllerProvider.notifier);
    final entries = ref.watch(myTimeEntriesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.shiftTitle)),
      body: SafeArea(
        child: PagedListView<TimeEntry>(
          state: entries,
          onRefresh: () async {
            // The card and the list are separate providers reading the same
            // rows; refreshing one without the other shows a finished shift
            // as still running.
            ref.invalidate(shiftControllerProvider);
            await controller.refresh();
          },
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.shiftHistoryEmpty,
          emptyIcon: Icons.schedule_outlined,
          header: const _ShiftCard(),
          itemBuilder: (context, entry) => _TimeEntryCard(entry: entry),
        ),
      ),
    );
  }
}

/// The running-shift card, with the elapsed time ticking while it is open.
class _ShiftCard extends ConsumerStatefulWidget {
  const _ShiftCard();

  @override
  ConsumerState<_ShiftCard> createState() => _ShiftCardState();
}

class _ShiftCardState extends ConsumerState<_ShiftCard> {
  Timer? _ticker;

  @override
  void initState() {
    super.initState();

    // Only the display advances; the shift itself is server state. A minute
    // is as often as the number can change.
    _ticker = Timer.periodic(const Duration(minutes: 1), (_) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final async = ref.watch(shiftControllerProvider);
    final state = async.value ?? const ShiftState();
    final shift = state.shift;
    final running = state.isRunning;

    return Card(
      margin: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  running ? Icons.play_circle_fill : Icons.pause_circle_outline,
                  color: running
                      ? theme.colorScheme.tertiary
                      : theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    running ? l10n.shiftRunning : l10n.shiftOff,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
            if (running && state.startedAt != null) ...[
              const SizedBox(height: 12),
              Text(
                _elapsedLabel(l10n, state.startedAt!),
                style: theme.textTheme.headlineMedium
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              Text(
                l10n.shiftSince(formatTime(state.startedAt!)),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              if (shift?.projectName != null)
                Text(
                  shift!.projectName!,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
            ],
            // Said before any failure, because it is not one: the worker did
            // clock in, the office simply does not know yet.
            if (state.isWaitingToSend) ...[
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.cloud_off_outlined,
                    size: 16,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      l10n.shiftWaitingToSend,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                ],
              ),
            ],
            if (state.failure != null) ...[
              const SizedBox(height: 8),
              Text(
                state.failure!.describe(l10n),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.error),
              ),
            ],
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                // Disabled only while a call is in flight, so a double tap
                // cannot open two shifts.
                onPressed: state.isBusy || async.isLoading
                    ? null
                    : () => running ? _clockOut(context) : _clockIn(),
                icon: Icon(running ? Icons.stop : Icons.play_arrow),
                label: Text(running ? l10n.shiftClockOut : l10n.shiftClockIn),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Measured from whichever start this handset knows about — the server's,
  /// or the one recorded here while there was no signal.
  String _elapsedLabel(AppLocalizations l10n, DateTime startedAt) {
    var elapsed = DateTime.now().toUtc().difference(startedAt.toUtc());

    // A phone whose clock is behind the server's would otherwise count
    // backwards.
    if (elapsed.isNegative) {
      elapsed = Duration.zero;
    }

    return l10n.shiftElapsed(elapsed.inHours, elapsed.inMinutes % 60);
  }

  Future<void> _clockIn() async {
    await ref.read(shiftControllerProvider.notifier).clockIn();
    ref.read(myTimeEntriesControllerProvider.notifier).refresh();
  }

  Future<void> _clockOut(BuildContext context) async {
    // Not dismissible by a tap outside or a swipe/back gesture: those read as
    // an accidental close, not a decision, and previously produced the exact
    // same silent "nothing happened" result as pressing Cancel — the only way
    // out is the explicit Cancel button, so a dismissal is never mistaken for
    // "clock out didn't work."
    final breakMinutes = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      isDismissible: false,
      enableDrag: false,
      builder: (_) => const _ClockOutSheet(),
    );

    if (breakMinutes == null) {
      return;
    }

    await ref.read(shiftControllerProvider.notifier).clockOut(
          breakMinutes: breakMinutes,
        );

    ref.read(myTimeEntriesControllerProvider.notifier).refresh();
  }
}

/// Asks for the unpaid break before ending the shift.
///
/// A sheet rather than a straight-through clock-out because the break is the
/// one number the server cannot know, and asking for it afterwards means a
/// correction someone has to approve.
class _ClockOutSheet extends StatefulWidget {
  const _ClockOutSheet();

  @override
  State<_ClockOutSheet> createState() => _ClockOutSheetState();
}

class _ClockOutSheetState extends State<_ClockOutSheet> {
  final _controller = TextEditingController(text: '0');

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return Padding(
      padding: EdgeInsets.fromLTRB(
        24,
        24,
        24,
        24 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.shiftClockOutTitle,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 16),
          TextField(
            controller: _controller,
            keyboardType: TextInputType.number,
            autofocus: true,
            decoration: InputDecoration(
              labelText: l10n.shiftBreakLabel,
              helperText: l10n.shiftBreakHint,
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
              const Spacer(),
              FilledButton(
                onPressed: () {
                  // A blank or unreadable field means no break, which is the
                  // common case; refusing to close the shift over it would
                  // strand someone at the gate.
                  final minutes = int.tryParse(_controller.text.trim()) ?? 0;
                  Navigator.of(context).pop(minutes < 0 ? 0 : minutes);
                },
                child: Text(l10n.shiftConfirm),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _TimeEntryCard extends StatelessWidget {
  const _TimeEntryCard({required this.entry});

  final TimeEntry entry;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final worked = entry.workedMinutes;
    final workedLabel = worked == null
        ? enumLabel(l10n, EnumKind.timeEntryStatus, entry.status)
        : l10n.shiftElapsed(worked ~/ 60, worked % 60);

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
                    formatDate(entry.startedAt),
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                Text(
                  workedLabel,
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              '${formatTime(entry.startedAt)} – '
              '${entry.endedAt == null ? '…' : formatTime(entry.endedAt!)}'
              ' · ${entry.projectName ?? l10n.shiftNoProject}',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: [
                Chip(
                  label: Text(
                    enumLabel(l10n, EnumKind.timeEntryStatus, entry.status),
                  ),
                  visualDensity: VisualDensity.compact,
                ),
                Chip(
                  label:
                      Text(enumLabel(l10n, EnumKind.workType, entry.workType)),
                  visualDensity: VisualDensity.compact,
                ),
                if (entry.breakMinutes > 0)
                  Chip(
                    label: Text(l10n.shiftBreakMinutes(entry.breakMinutes)),
                    visualDensity: VisualDensity.compact,
                  ),
              ],
            ),
            if (entry.reviewNote != null) ...[
              const SizedBox(height: 8),
              Text(
                l10n.shiftSentBack(entry.reviewNote!),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
