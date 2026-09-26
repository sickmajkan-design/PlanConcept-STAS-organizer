import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../data/models/clock_in_site.dart';
import '../data/models/time_entry.dart';
import '../data/time_entry_repository.dart';
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
          header: const ShiftCard(),
          itemBuilder: (context, entry) => _TimeEntryCard(entry: entry),
        ),
      ),
    );
  }
}

/// The running-shift card, with the elapsed time ticking while it is open.
class ShiftCard extends ConsumerStatefulWidget {
  const ShiftCard({super.key, this.hero = false});

  /// The big dark version for the home screen: the timer is the point, and
  /// the one button under it is the whole interaction.
  final bool hero;

  @override
  ConsumerState<ShiftCard> createState() => _ShiftCardState();
}

class _ShiftCardState extends ConsumerState<ShiftCard> {
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

    if (widget.hero) {
      return _buildHero(context, async, state);
    }

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
                icon: state.isBusy
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Icon(running ? Icons.stop : Icons.play_arrow),
                label: Text(state.isBusy ? l10n.shiftWorking : (running ? l10n.shiftClockOut : l10n.shiftClockIn)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHero(
    BuildContext context,
    AsyncValue<ShiftState> async,
    ShiftState state,
  ) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final running = state.isRunning;
    final shift = state.shift;

    const ink = AppTheme.heroCardText;
    final muted = ink.withValues(alpha: 0.72);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 20),
      decoration: BoxDecoration(
        color: AppTheme.heroCard,
        borderRadius: BorderRadius.circular(24),
        boxShadow: const [
          BoxShadow(color: Color(0x33000000), blurRadius: 18, offset: Offset(0, 8)),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (running)
                Container(
                  width: 10,
                  height: 10,
                  decoration: const BoxDecoration(
                    color: Color(0xFF5DCB6E),
                    shape: BoxShape.circle,
                  ),
                )
              else
                Icon(Icons.engineering_outlined, color: ink, size: 22),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  running ? l10n.homeOnShift : l10n.homeOffShift,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: ink,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          if (running && state.startedAt != null) ...[
            const SizedBox(height: 12),
            Text(
              _elapsedLabel(l10n, state.startedAt!),
              style: theme.textTheme.displaySmall?.copyWith(
                color: ink,
                fontWeight: FontWeight.w800,
                height: 1.05,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              [
                l10n.shiftSince(formatTime(state.startedAt!)),
                if (shift?.projectName != null) shift!.projectName!,
              ].join(' \u00b7 '),
              style: theme.textTheme.bodyLarge?.copyWith(color: muted),
            ),
          ] else ...[
            const SizedBox(height: 6),
            Text(
              l10n.homeOffShiftHint,
              style: theme.textTheme.bodyLarge?.copyWith(color: muted),
            ),
          ],
          if (state.isWaitingToSend) ...[
            const SizedBox(height: 12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.cloud_off_outlined, size: 16, color: muted),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    l10n.shiftWaitingToSend,
                    style: theme.textTheme.bodySmall?.copyWith(color: muted),
                  ),
                ),
              ],
            ),
          ],
          if (state.failure != null) ...[
            const SizedBox(height: 12),
            Text(
              state.failure!.describe(l10n),
              style: theme.textTheme.bodyMedium?.copyWith(
                color: const Color(0xFFFFB59A),
              ),
            ),
          ],
          const SizedBox(height: 20),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: AppTheme.heroCardText,
                foregroundColor: AppTheme.heroCard,
                disabledBackgroundColor: ink.withValues(alpha: 0.3),
                disabledForegroundColor: AppTheme.heroCard.withValues(alpha: 0.6),
              ),
              onPressed: state.isBusy || async.isLoading
                  ? null
                  : () => running ? _clockOut(context) : _clockIn(),
              icon: state.isBusy
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Icon(running ? Icons.stop : Icons.play_arrow),
              label: Text(state.isBusy ? l10n.shiftWorking : (running ? l10n.shiftClockOut : l10n.shiftClockIn)),
            ),
          ),
        ],
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
    if (blockedByPendingAcknowledgment(context, ref)) return;

    // A worker posted to several sites today says which one; with one or none the server
    // places the shift itself, so nothing is asked.
    final sites = await _sitesToChooseFrom();

    String? projectId;

    if (sites.length > 1) {
      if (!mounted) return;

      // Not dismissible, for the reason the break sheet is not: a stray tap must not read as a
      // clock-in that silently did nothing. Cancelling is the explicit button.
      projectId = await showModalBottomSheet<String>(
        context: context,
        isDismissible: false,
        enableDrag: false,
        builder: (_) => SitePickerSheet(sites: sites),
      );

      if (projectId == null) return;
    }

    await ref.read(shiftControllerProvider.notifier).clockIn(projectId: projectId);
    ref.read(myTimeEntriesControllerProvider.notifier).refresh();
  }

  /// Never throws and never holds the shift back: with no signal the answer is "none", and the
  /// shift is recorded and placed by the server when it arrives.
  Future<List<ClockInSite>> _sitesToChooseFrom() async {
    try {
      return await ref.read(timeEntryRepositoryProvider).fetchClockInSites();
    } catch (_) {
      return const [];
    }
  }

  Future<void> _clockOut(BuildContext context) async {
    if (blockedByPendingAcknowledgment(context, ref)) return;

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
  void initState() {
    super.initState();

    // The 0 is a default, not something to be typed after: with the cursor
    // behind it, "30" became "030".
    _controller.selection =
        TextSelection(baseOffset: 0, extentOffset: _controller.text.length);
  }

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
                style: AppTheme.inlineFilledButton,
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
                if (entry.autoClosed)
                  Chip(
                    label: Text(l10n.shiftAutoClosed),
                    visualDensity: VisualDensity.compact,
                    backgroundColor: theme.colorScheme.errorContainer,
                    labelStyle: TextStyle(color: theme.colorScheme.onErrorContainer),
                  ),
                if (entry.locationCorrect == false)
                  Chip(
                    label: Text(l10n.shiftLocationMismatch),
                    visualDensity: VisualDensity.compact,
                    backgroundColor: theme.colorScheme.errorContainer,
                    labelStyle: TextStyle(color: theme.colorScheme.onErrorContainer),
                  ),
                if (entry.timeCorrect == false)
                  Chip(
                    label: Text(l10n.shiftTimeMismatch),
                    visualDensity: VisualDensity.compact,
                    backgroundColor: theme.colorScheme.errorContainer,
                    labelStyle: TextStyle(color: theme.colorScheme.onErrorContainer),
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

/// Which site the worker is on, when they are posted to more than one.
class SitePickerSheet extends StatelessWidget {
  const SitePickerSheet({super.key, required this.sites});

  final List<ClockInSite> sites;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.shiftPickSiteTitle,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Text(
              l10n.shiftPickSiteHint,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 8),
            for (final site in sites)
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.apartment_outlined),
                title: Text(site.name),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => Navigator.of(context).pop(site.id),
              ),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
