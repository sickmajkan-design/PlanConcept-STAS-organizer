import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import 'my_absences_controller.dart';
import 'my_schedule_controller.dart';

/// The kinds a worker may ask for. `Other` is deliberately left out: on a
/// phone it produces requests nobody can act on without a phone call, and the
/// office can record anything unusual on the admin panel.
const _requestableTypes = <String>[
  'AnnualLeave',
  'SickLeave',
  'UnpaidLeave',
  'PaidSpecialLeave',
  'Training',
];

/// How far ahead the picker allows. Mirrors `AbsenceRules.MaxLeadDays`.
const _maxLeadDays = 550;

/// How far back. Mirrors `AbsenceRules.MaxBackdatingDays`; sick leave is
/// usually entered after the fact.
const _maxBackdatingDays = 90;

Future<void> showRequestAbsenceSheet(BuildContext context, WidgetRef ref) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _RequestAbsenceSheet(),
  );
}

class _RequestAbsenceSheet extends ConsumerStatefulWidget {
  const _RequestAbsenceSheet();

  @override
  ConsumerState<_RequestAbsenceSheet> createState() =>
      _RequestAbsenceSheetState();
}

class _RequestAbsenceSheetState extends ConsumerState<_RequestAbsenceSheet> {
  final _reasonController = TextEditingController();

  String _type = 'AnnualLeave';
  DateTimeRange? _range;
  bool _busy = false;

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return Padding(
      // Lifts the sheet clear of the keyboard when the reason field has focus.
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.absencesRequest,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _type,
              decoration: InputDecoration(labelText: l10n.absencesType),
              items: [
                for (final type in _requestableTypes)
                  DropdownMenuItem(
                    value: type,
                    child: Text(enumLabel(l10n, EnumKind.absenceType, type)),
                  ),
              ],
              onChanged: (value) {
                if (value != null) {
                  setState(() => _type = value);
                }
              },
            ),
            const SizedBox(height: 12),
            // One range picker rather than two date fields: it cannot produce
            // an end before a start, so that mistake never reaches the API.
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickRange,
              icon: const Icon(Icons.date_range_outlined),
              label: Text(
                _range == null
                    ? l10n.absencesPickDates
                    : '${formatDate(_range!.start)} – ${formatDate(_range!.end)}',
              ),
            ),
            if (_range != null) ...[
              const SizedBox(height: 4),
              Text(
                l10n.absencesDayCount(
                  _range!.end.difference(_range!.start).inDays + 1,
                ),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            const SizedBox(height: 12),
            TextField(
              controller: _reasonController,
              maxLines: 3,
              maxLength: 1000,
              decoration: InputDecoration(labelText: l10n.absencesReason),
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || _range == null ? null : _send,
                child: Text(l10n.absencesSend),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickRange() async {
    final today = DateTime.now();
    final start = DateTime(today.year, today.month, today.day);

    final picked = await showDateRangePicker(
      context: context,
      initialDateRange: _range,
      firstDate: start.subtract(const Duration(days: _maxBackdatingDays)),
      lastDate: start.add(const Duration(days: _maxLeadDays)),
    );

    if (picked != null && mounted) {
      setState(() => _range = picked);
    }
  }

  Future<void> _send() async {
    final range = _range;

    if (range == null) {
      return;
    }

    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final reason = _reasonController.text.trim();

    setState(() => _busy = true);

    try {
      await ref.read(myAbsencesControllerProvider.notifier).request(
            type: _type,
            startDate: range.start,
            endDate: range.end,
            reason: reason.isEmpty ? null : reason,
          );

      // A request is not granted leave, so it will not appear on the schedule
      // yet — but it will the moment somebody approves it, and the screen
      // behind this one should not be showing a stale window either way.
      ref.invalidate(myScheduleControllerProvider);

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.absencesSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
