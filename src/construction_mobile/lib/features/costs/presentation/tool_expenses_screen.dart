import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/tool_expense.dart';
import 'record_tool_expense_sheet.dart';
import 'tool_expenses_controller.dart';

/// What the tool fleet has cost — see `VehicleExpensesScreen` for the pattern
/// this mirrors. No fuel, so no filter chip and no litres/price-per-litre.
class ToolExpensesScreen extends ConsumerWidget {
  const ToolExpensesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final controller = ref.read(toolExpensesControllerProvider.notifier);
    final state = ref.watch(toolExpensesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.toolExpensesTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => showRecordToolExpenseSheet(context),
        icon: const Icon(Icons.add),
        label: Text(l10n.toolExpensesRecord),
      ),
      body: SafeArea(
        child: PagedListView<ToolExpense>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.toolExpensesEmpty,
          emptyIcon: Icons.handyman_outlined,
          itemBuilder: (context, expense) => _ExpenseCard(expense: expense),
        ),
      ),
    );
  }
}

class _ExpenseCard extends StatelessWidget {
  const _ExpenseCard({required this.expense});

  final ToolExpense expense;

  @override
  Widget build(BuildContext context) {
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
                Icon(
                  Icons.build_outlined,
                  size: 20,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    expense.toolName,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                Text(
                  formatAmount(expense.amount),
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: [
                Chip(
                  label: Text(
                    enumLabel(l10n, EnumKind.toolExpenseKind, expense.kind),
                  ),
                  visualDensity: VisualDensity.compact,
                ),
                Chip(
                  label: Text(formatDate(expense.occurred)),
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            if ((expense.note ?? '').isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(
                expense.note!,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
