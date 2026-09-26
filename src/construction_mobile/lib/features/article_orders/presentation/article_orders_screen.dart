import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/article_order.dart';
import 'article_orders_controller.dart';
import 'request_articles_sheet.dart';

const _openStatuses = <String>{'Requested', 'Ordered', 'InDelivery'};

/// Articles a person needs for the job. Anyone asks; the office orders and sends; the person
/// who asked presses "Received" when it arrives. Open requests first, since that is what
/// somebody opens the screen for.
class ArticleOrdersScreen extends ConsumerStatefulWidget {
  const ArticleOrdersScreen({super.key});

  @override
  ConsumerState<ArticleOrdersScreen> createState() => _ArticleOrdersScreenState();
}

class _ArticleOrdersScreenState extends ConsumerState<ArticleOrdersScreen> {
  bool _openOnly = true;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final state = ref.watch(articleOrdersControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navArticleOrders)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => showRequestArticlesSheet(context),
        icon: const Icon(Icons.add),
        label: Text(l10n.articleOrdersNew),
      ),
      body: SafeArea(
        child: state.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(articleOrdersControllerProvider),
          ),
          data: (orders) {
            final shown = _openOnly
                ? orders.where((o) => _openStatuses.contains(o.status)).toList()
                : orders;

            return RefreshIndicator(
              onRefresh: () => ref.read(articleOrdersControllerProvider.notifier).refresh(),
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 96),
                children: [
                  // The same chip as the other lists: one filter, on or off.
                  Align(
                    alignment: Alignment.centerLeft,
                    child: FilterChip(
                      label: Text(l10n.articleOrdersOpenOnly),
                      selected: _openOnly,
                      onSelected: (selected) => setState(() => _openOnly = selected),
                    ),
                  ),
                  const SizedBox(height: 8),
                  if (shown.isEmpty)
                    SizedBox(
                      height: MediaQuery.sizeOf(context).height * 0.5,
                      child: EmptyView(message: l10n.articleOrdersEmpty, icon: Icons.shopping_bag_outlined),
                    )
                  else
                    for (final order in shown) _OrderCard(order: order),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

String articleOrderStatusLabel(BuildContext context, String status) {
  final l10n = context.l10n;

  return switch (status) {
    'Requested' => l10n.articleOrderStatusRequested,
    'Ordered' => l10n.articleOrderStatusOrdered,
    'InDelivery' => l10n.articleOrderStatusInDelivery,
    'Delivered' => l10n.articleOrderStatusDelivered,
    'Rejected' => l10n.articleOrderStatusRejected,
    'Cancelled' => l10n.articleOrderStatusCancelled,
    _ => status,
  };
}

class _OrderCard extends ConsumerWidget {
  const _OrderCard({required this.order});

  final ArticleOrder order;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final user = ref.watch(currentUserProvider);
    final manages = user?.isProjectManagerAndAbove ?? false;
    final own = user != null && order.requestedByUserId == user.id;

    Future<void> move(String status, {String? note}) async {
      final messenger = ScaffoldMessenger.of(context);

      try {
        await ref.read(articleOrdersControllerProvider.notifier).setStatus(order, status, note: note);
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
                    order.requestedByName,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                if (order.urgent) ...[
                  Chip(
                    label: Text(l10n.articleOrdersUrgent),
                    visualDensity: VisualDensity.compact,
                    backgroundColor: theme.colorScheme.errorContainer,
                  ),
                  const SizedBox(width: 4),
                ],
                Chip(
                  label: Text(articleOrderStatusLabel(context, order.status)),
                  backgroundColor: switch (order.status) {
                    'Delivered' => theme.colorScheme.primaryContainer,
                    'Rejected' => theme.colorScheme.errorContainer,
                    _ => null,
                  },
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            const SizedBox(height: 4),
            for (final item in order.items) Text('• ${item.summary}'),
            const SizedBox(height: 8),
            Text(
              [formatDate(order.createdAt), if (order.projectName != null) order.projectName!].join(' · '),
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            if ((order.note ?? '').isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(order.note!),
            ],
            if (order.status == 'Rejected' && (order.reviewNote ?? '').isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(order.reviewNote!, style: TextStyle(color: theme.colorScheme.error)),
            ],
            const SizedBox(height: 8),
            Wrap(
              alignment: WrapAlignment.end,
              spacing: 8,
              runSpacing: 4,
              children: [
                if (manages && order.status == 'Requested')
                  OutlinedButton(onPressed: () => move('Ordered'), child: Text(l10n.articleOrdersActionOrder)),
                if (manages && order.status == 'Ordered')
                  OutlinedButton(onPressed: () => move('InDelivery'), child: Text(l10n.articleOrdersActionShip)),
                if ((own || manages) && order.status == 'InDelivery')
                  OutlinedButton(
                    onPressed: () => move('Delivered'),
                    child: Text(own ? l10n.articleOrdersActionReceived : l10n.articleOrdersActionDelivered),
                  ),
                if (manages && (order.status == 'Requested' || order.status == 'Ordered'))
                  TextButton(
                    onPressed: () => _decline(context, move),
                    child: Text(l10n.articleOrdersActionDecline),
                  ),
                if (own && order.status == 'Requested')
                  TextButton.icon(
                    onPressed: () => move('Cancelled'),
                    icon: const Icon(Icons.undo),
                    label: Text(l10n.articleOrdersActionWithdraw),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _decline(BuildContext context, Future<void> Function(String, {String? note}) move) async {
    final l10n = context.l10n;
    final controller = TextEditingController();

    final reason = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.articleOrdersDeclineTitle),
        content: TextField(
          controller: controller,
          autofocus: true,
          maxLines: 3,
          maxLength: 1000,
          decoration: InputDecoration(labelText: l10n.articleOrdersDeclineReason),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext), child: Text(l10n.commonCancel)),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, controller.text.trim()),
            child: Text(l10n.articleOrdersActionDecline),
          ),
        ],
      ),
    );

    controller.dispose();

    if (reason != null && reason.isNotEmpty) {
      await move('Rejected', note: reason);
    }
  }
}
