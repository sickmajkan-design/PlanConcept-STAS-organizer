import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../data/models/ledger.dart';
import 'ledgers_controller.dart';

/// The list of monthly ledgers — view-only on mobile. Editing a ledger's
/// columns/sections/rows stays a desktop-only workflow; a phone screen is
/// the wrong shape for a wide free-form spreadsheet.
class LedgersScreen extends ConsumerWidget {
  const LedgersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final controller = ref.read(ledgersControllerProvider.notifier);
    final state = ref.watch(ledgersControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.ledgersTitle)),
      body: SafeArea(
        child: PagedListView<LedgerSummary>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: l10n.ledgersEmpty,
          emptyIcon: Icons.table_chart_outlined,
          header: ListSearchHeader(
            hintText: l10n.ledgersSearchHint,
            onSearchChanged: controller.search,
          ),
          itemBuilder: (context, ledger) => _LedgerCard(ledger: ledger),
        ),
      ),
    );
  }
}

class _LedgerCard extends StatelessWidget {
  const _LedgerCard({required this.ledger});

  final LedgerSummary ledger;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(AppRoutes.ledgerDetail(ledger.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Icon(
                  Icons.table_chart_outlined,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      ledger.name,
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${ledger.month.toString().padLeft(2, '0')}/${ledger.year}',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
