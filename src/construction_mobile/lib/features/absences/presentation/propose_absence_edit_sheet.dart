import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../data/models/absence.dart';
import 'my_absences_controller.dart';
import 'my_schedule_controller.dart';

/// How far ahead the picker allows. Mirrors `AbsenceRules.MaxLeadDays`.
const _maxLeadDays = 550;

/// How far back. Mirrors `AbsenceRules.MaxBackdatingDays`.
const _maxBackdatingDays = 90;

/// Sheet text, hand-mapped by language rather than routed through the
/// generated `AppLocalizations` — see the note on `_BulletinText` in
/// `bulletin_screen.dart` for why: this was written without a Flutter
/// toolchain available to run `flutter gen-l10n`. Moving these into the
/// `.arb` files is the right cleanup once someone with the toolchain touches
/// this feature.
class _ProposeEditText {
  const _ProposeEditText({
    required this.title,
    required this.hint,
    required this.pickDates,
    required this.reason,
    required this.send,
    required this.sent,
  });

  final String title;
  final String hint;
  final String pickDates;
  final String reason;
  final String send;
  final String sent;

  static const _sr = _ProposeEditText(
    title: 'Predloži nove datume',
    hint:
        'Ovo još ne mijenja odsustvo — uprava treba da potvrdi prije nego što se promijeni.',
    pickDates: 'Izaberi datume',
    reason: 'Novi razlog (opciono)',
    send: 'Pošalji predlog',
    sent: 'Predlog je poslat.',
  );

  static const _en = _ProposeEditText(
    title: 'Propose new dates',
    hint:
        "This doesn't change the leave yet — management still has to confirm before it changes.",
    pickDates: 'Pick dates',
    reason: 'New reason (optional)',
    send: 'Send proposal',
    sent: 'Proposal sent.',
  );

  static _ProposeEditText of(BuildContext context) =>
      Localizations.localeOf(context).languageCode == 'sr' ? _sr : _en;
}

Future<void> showProposeAbsenceEditSheet(
  BuildContext context,
  WidgetRef ref,
  Absence absence,
) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _ProposeAbsenceEditSheet(absence: absence),
  );
}

class _ProposeAbsenceEditSheet extends ConsumerStatefulWidget {
  const _ProposeAbsenceEditSheet({required this.absence});

  final Absence absence;

  @override
  ConsumerState<_ProposeAbsenceEditSheet> createState() =>
      _ProposeAbsenceEditSheetState();
}

class _ProposeAbsenceEditSheetState
    extends ConsumerState<_ProposeAbsenceEditSheet> {
  final _reasonController = TextEditingController();
  DateTimeRange? _range;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    final start = widget.absence.start;
    final end = widget.absence.end;
    if (start != null && end != null) {
      _range = DateTimeRange(start: start, end: end);
    }
    _reasonController.text = widget.absence.reason ?? '';
  }

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final text = _ProposeEditText.of(context);
    final theme = Theme.of(context);

    return Padding(
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
              text.title,
              style:
                  theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            Text(
              text.hint,
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickRange,
              icon: const Icon(Icons.date_range_outlined),
              label: Text(
                _range == null
                    ? text.pickDates
                    : '${formatDate(_range!.start)} – ${formatDate(_range!.end)}',
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _reasonController,
              maxLines: 3,
              maxLength: 1000,
              decoration: InputDecoration(labelText: text.reason),
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || _range == null ? null : _send,
                child: Text(text.send),
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
    if (range == null) return;

    final text = _ProposeEditText.of(context);
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final reason = _reasonController.text.trim();

    setState(() => _busy = true);

    try {
      await ref.read(myAbsencesControllerProvider.notifier).proposeEdit(
            absence: widget.absence,
            startDate: range.start,
            endDate: range.end,
            reason: reason.isEmpty ? null : reason,
          );

      ref.invalidate(myScheduleControllerProvider);

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(text.sent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
