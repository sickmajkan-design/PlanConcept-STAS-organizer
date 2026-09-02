import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/absence.dart';
import 'my_absences_controller.dart';
import 'propose_absence_edit_sheet.dart';
import 'request_absence_sheet.dart';

/// Card text for the propose/confirm-edit flow, hand-mapped by language
/// rather than routed through the generated `AppLocalizations` — see the
/// note on `_BulletinText` in `bulletin_screen.dart` for why.
class _EditProposalText {
  const _EditProposalText({
    required this.proposeEdit,
    required this.waitingOnEmployee,
    required this.waitingOnManagement,
    required this.proposed,
    required this.confirm,
    required this.confirmTitle,
    required this.confirmBody,
    required this.decline,
    required this.declineTitle,
    required this.declineBody,
    required this.confirmed,
    required this.declined,
    required this.cancel,
  });

  final String proposeEdit;
  final String waitingOnEmployee;
  final String waitingOnManagement;
  final String Function(String start, String end) proposed;
  final String confirm;
  final String confirmTitle;
  final String Function(String start, String end) confirmBody;
  final String decline;
  final String declineTitle;
  final String declineBody;
  final String confirmed;
  final String declined;
  final String cancel;

  static const _sr = _EditProposalText(
    proposeEdit: 'Predloži izmjenu',
    waitingOnEmployee: 'Čeka vašu potvrdu',
    waitingOnManagement: 'Čeka potvrdu uprave',
    proposed: _proposedSr,
    confirm: 'Potvrdi',
    confirmTitle: 'Potvrditi predloženu izmjenu?',
    confirmBody: _confirmBodySr,
    decline: 'Odbij',
    declineTitle: 'Odbiti predloženu izmjenu?',
    declineBody: 'Odsustvo ostaje kako je trenutno odobreno.',
    confirmed: 'Izmjena je potvrđena.',
    declined: 'Izmjena je odbijena.',
    cancel: 'Otkaži',
  );

  static const _en = _EditProposalText(
    proposeEdit: 'Propose a change',
    waitingOnEmployee: 'Waiting for your confirmation',
    waitingOnManagement: 'Waiting for management to confirm',
    proposed: _proposedEn,
    confirm: 'Confirm',
    confirmTitle: 'Confirm the proposed change?',
    confirmBody: _confirmBodyEn,
    decline: 'Decline',
    declineTitle: 'Decline the proposed change?',
    declineBody: 'The leave stays as it is currently approved.',
    confirmed: 'Change confirmed.',
    declined: 'Change declined.',
    cancel: 'Cancel',
  );

  static String _proposedSr(String start, String end) => 'Predlog: $start – $end';
  static String _proposedEn(String start, String end) => 'Proposed: $start – $end';

  static String _confirmBodySr(String start, String end) =>
      'Novi datumi: $start – $end. Ovo zamjenjuje trenutno odobreno odsustvo.';
  static String _confirmBodyEn(String start, String end) =>
      'New dates: $start – $end. This replaces the currently approved leave.';

  static _EditProposalText of(BuildContext context) =>
      Localizations.localeOf(context).languageCode == 'sr' ? _sr : _en;
}

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
            if (absence.hasPendingEdit) ...[
              const SizedBox(height: 8),
              _PendingEditBanner(absence: absence),
            ] else if (absence.isApproved && absence.type == 'AnnualLeave') ...[
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: () =>
                      showProposeAbsenceEditSheet(context, ref, absence),
                  icon: const Icon(Icons.edit_calendar_outlined, size: 18),
                  label: Text(_EditProposalText.of(context).proposeEdit),
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
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}

/// The staged change on an absence: the proposed dates, who it is waiting
/// on, and — only for the side that has not yet answered — the buttons to
/// confirm or decline it.
class _PendingEditBanner extends ConsumerWidget {
  const _PendingEditBanner({required this.absence});

  final Absence absence;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final text = _EditProposalText.of(context);
    final theme = Theme.of(context);

    // The employee proposed it themselves — nothing for their own app to do
    // but wait for management. Only the opposite case (management proposed
    // it) has a Confirm/Decline pair here.
    final awaitingThisApp = !absence.proposedByEmployee;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: theme.colorScheme.secondaryContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            text.proposed(
              formatDate(absence.proposedStart),
              formatDate(absence.proposedEnd),
            ),
            style: theme.textTheme.bodyMedium
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 2),
          Text(
            awaitingThisApp ? text.waitingOnEmployee : text.waitingOnManagement,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSecondaryContainer,
            ),
          ),
          if (awaitingThisApp) ...[
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => _respond(context, ref, approve: false),
                  child: Text(text.decline),
                ),
                const SizedBox(width: 4),
                FilledButton(
                  onPressed: () => _respond(context, ref, approve: true),
                  child: Text(text.confirm),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _respond(
    BuildContext context,
    WidgetRef ref, {
    required bool approve,
  }) async {
    final text = _EditProposalText.of(context);
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(approve ? text.confirmTitle : text.declineTitle),
        content: Text(
          approve
              ? text.confirmBody(
                  formatDate(absence.proposedStart),
                  formatDate(absence.proposedEnd),
                )
              : text.declineBody,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(text.cancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(approve ? text.confirm : text.decline),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await ref
          .read(myAbsencesControllerProvider.notifier)
          .confirmEdit(absence: absence, approve: approve);

      messenger.showSnackBar(
        SnackBar(content: Text(approve ? text.confirmed : text.declined)),
      );
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}
