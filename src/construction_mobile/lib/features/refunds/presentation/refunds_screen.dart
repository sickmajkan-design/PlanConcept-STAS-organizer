import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/refund.dart';
import 'refunds_controller.dart';
import 'request_refund_sheet.dart';

/// Money a person spent for the firm. They ask with the reason and a photograph of the receipt;
/// the office approves or declines; an approved one is paid with a payroll month.
class RefundsScreen extends ConsumerWidget {
  const RefundsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final state = ref.watch(refundsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navRefunds)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => showRequestRefundSheet(context),
        icon: const Icon(Icons.add),
        label: Text(l10n.refundsNew),
      ),
      body: SafeArea(
        child: state.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(refundsControllerProvider),
          ),
          data: (refunds) => RefreshIndicator(
            onRefresh: () => ref.read(refundsControllerProvider.notifier).refresh(),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 96),
              physics: const AlwaysScrollableScrollPhysics(),
              children: refunds.isEmpty
                  ? [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.5,
                        child: EmptyView(message: l10n.refundsEmpty, icon: Icons.request_quote_outlined),
                      ),
                    ]
                  : [for (final refund in refunds) _RefundCard(refund: refund)],
            ),
          ),
        ),
      ),
    );
  }
}

String refundStatusLabel(BuildContext context, String status) {
  final l10n = context.l10n;

  return switch (status) {
    'Requested' => l10n.refundStatusRequested,
    'Approved' => l10n.refundStatusApproved,
    'Rejected' => l10n.refundStatusRejected,
    'Cancelled' => l10n.refundStatusCancelled,
    _ => status,
  };
}

class _RefundCard extends ConsumerWidget {
  const _RefundCard({required this.refund});

  final Refund refund;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final user = ref.watch(currentUserProvider);
    final own = user != null && refund.requestedByUserId == user.id;
    final manages = (user?.isProjectManagerAndAbove ?? false) && !own;
    final waiting = refund.status == 'Requested';

    Future<void> act(String status, {String? note}) async {
      final messenger = ScaffoldMessenger.of(context);

      try {
        await ref.read(refundsControllerProvider.notifier).review(refund, status, note: note);
      } on ApiException catch (exception) {
        messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
      }
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    refund.employeeName,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                Text(
                  '${refund.amount.toStringAsFixed(2)} ${refund.currency}',
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(refund.description),
            const SizedBox(height: 4),
            Text(
              [
                formatDate(refund.expenseDate),
                if (refund.status == 'Approved' && refund.payrollMonth != null)
                  l10n.refundPaidWith(
                    '${refund.payrollMonth.toString().padLeft(2, '0')}.${refund.payrollYear}',
                  ),
              ].join(' · '),
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 8),
            Chip(
              label: Text(refundStatusLabel(context, refund.status)),
              backgroundColor: switch (refund.status) {
                'Approved' => theme.colorScheme.primaryContainer,
                'Rejected' => theme.colorScheme.errorContainer,
                _ => null,
              },
              visualDensity: VisualDensity.compact,
            ),
            if (refund.status == 'Rejected' && (refund.reviewNote ?? '').isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(refund.reviewNote!, style: TextStyle(color: theme.colorScheme.error)),
            ],
            if (waiting && (manages || own)) ...[
              const SizedBox(height: 8),
              Wrap(
                alignment: WrapAlignment.end,
                spacing: 8,
                runSpacing: 4,
                children: [
                  if (manages) ...[
                    OutlinedButton(onPressed: () => act('Approved'), child: Text(l10n.refundApprove)),
                    TextButton(
                      onPressed: () => _decline(context, act),
                      child: Text(l10n.refundDecline),
                    ),
                  ],
                  if (own)
                    TextButton.icon(
                      onPressed: () => act('Cancelled'),
                      icon: const Icon(Icons.undo),
                      label: Text(l10n.refundWithdraw),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _decline(BuildContext context, Future<void> Function(String, {String? note}) act) async {
    final l10n = context.l10n;
    final controller = TextEditingController();

    final reason = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.refundDeclineTitle),
        content: TextField(
          controller: controller,
          autofocus: true,
          maxLines: 3,
          maxLength: 1000,
          decoration: InputDecoration(labelText: l10n.refundDeclineReason),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext), child: Text(l10n.commonCancel)),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, controller.text.trim()),
            child: Text(l10n.refundDecline),
          ),
        ],
      ),
    );

    controller.dispose();

    if (reason != null && reason.isNotEmpty) {
      await act('Rejected', note: reason);
    }
  }
}
