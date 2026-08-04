import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/vehicle_expense.dart';
import 'record_expense_sheet.dart';
import 'vehicle_expenses_controller.dart';

/// What the fleet has cost, and the button for adding to it from the pump.
///
/// The one place in the costing module where a phone beats the office screen:
/// the person filling the tank is standing next to the receipt and the
/// odometer, and anything they have to remember and type in later gets typed
/// in wrong or not at all.
class VehicleExpensesScreen extends ConsumerWidget {
  const VehicleExpensesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final controller = ref.read(vehicleExpensesControllerProvider.notifier);
    final state = ref.watch(vehicleExpensesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.vehicleExpensesTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => showRecordExpenseSheet(context),
        icon: const Icon(Icons.add),
        label: Text(l10n.vehicleExpensesRecord),
      ),
      body: SafeArea(
        child: PagedListView<VehicleExpense>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.vehicleExpensesEmpty,
          emptyIcon: Icons.local_gas_station_outlined,
          // A chip row without a search box: the endpoint has no text search,
          // and offering one that does nothing is worse than offering none.
          header: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
            child: Align(
              alignment: Alignment.centerLeft,
              child: FilterChip(
                label: Text(l10n.vehicleExpensesFuelOnly),
                selected: controller.filter == vehicleExpenseFuelFilter,
                onSelected: (selected) => controller
                    .applyFilter(selected ? vehicleExpenseFuelFilter : null),
              ),
            ),
          ),
          itemBuilder: (context, expense) => _ExpenseCard(expense: expense),
        ),
      ),
    );
  }
}

class _ExpenseCard extends StatelessWidget {
  const _ExpenseCard({required this.expense});

  final VehicleExpense expense;

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
                  expense.isFuel
                      ? Icons.local_gas_station_outlined
                      : Icons.build_outlined,
                  size: 20,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    expense.vehicleName,
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
                    enumLabel(l10n, EnumKind.vehicleExpenseKind, expense.kind),
                  ),
                  visualDensity: VisualDensity.compact,
                ),
                Chip(
                  label: Text(formatDate(expense.occurred)),
                  visualDensity: VisualDensity.compact,
                ),
                if (expense.litres != null)
                  Chip(
                    label: Text(
                      '${formatQuantity(expense.litres)} l',
                    ),
                    visualDensity: VisualDensity.compact,
                  ),
                if (expense.pricePerLitre != null)
                  Chip(
                    label: Text(
                      l10n.vehicleExpensesPerLitre(
                        formatAmount(expense.pricePerLitre),
                      ),
                    ),
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
