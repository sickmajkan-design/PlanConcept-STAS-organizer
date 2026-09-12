import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../data/models/ledger.dart';
import 'ledgers_controller.dart';

/// One month's ledger: the summary panel, then every section (a company or
/// site) as an expandable card of its rows — a phone-shaped stand-in for the
/// desktop's wide spreadsheet grid.
class LedgerDetailScreen extends ConsumerWidget {
  const LedgerDetailScreen({super.key, required this.ledgerId});

  final String ledgerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final detail = ref.watch(ledgerDetailProvider(ledgerId));

    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.name ?? l10n.ledgersTitle)),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(ledgerDetailProvider(ledgerId)),
          ),
          data: (ledger) => RefreshIndicator(
            onRefresh: () async => ref.invalidate(ledgerDetailProvider(ledgerId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if ((ledger.note ?? '').isNotEmpty) ...[
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Text(ledger.note!),
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
                _SummaryPanel(ledgerId: ledgerId),
                const SizedBox(height: 20),
                Text(
                  l10n.ledgersSectionsTitle,
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
                const SizedBox(height: 8),
                for (final section in ledger.sections)
                  _SectionTile(
                    ledgerId: ledgerId,
                    section: section,
                    columns: ledger.columns,
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _SummaryPanel extends ConsumerWidget {
  const _SummaryPanel({required this.ledgerId});

  final String ledgerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final summary = ref.watch(ledgerSummaryPanelProvider(ledgerId));

    return summary.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (panel) {
        if (panel.boxes.isEmpty) {
          return const SizedBox.shrink();
        }

        return Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (final box in panel.boxes)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Expanded(child: Text(box.label)),
                        Text(
                          formatAmount(box.value),
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                  ),
                const Divider(height: 20),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        l10n.ledgersNetTotal,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                    Text(
                      formatAmount(panel.netTotal),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _SectionTile extends StatefulWidget {
  const _SectionTile({
    required this.ledgerId,
    required this.section,
    required this.columns,
  });

  final String ledgerId;
  final LedgerSection section;
  final List<LedgerColumn> columns;

  @override
  State<_SectionTile> createState() => _SectionTileState();
}

class _SectionTileState extends State<_SectionTile> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final section = widget.section;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Column(
        children: [
          ListTile(
            leading: const Icon(Icons.folder_outlined),
            title: Text(section.name),
            subtitle: section.projectName != null
                ? Text(section.projectName!)
                : null,
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(l10n.ledgersRowCount(section.rowCount)),
                Icon(_expanded ? Icons.expand_less : Icons.expand_more),
              ],
            ),
            onTap: () => setState(() => _expanded = !_expanded),
          ),
          if (_expanded)
            _SectionRows(
              ledgerId: widget.ledgerId,
              sectionId: section.id,
              columns: widget.columns,
            ),
        ],
      ),
    );
  }
}

class _SectionRows extends ConsumerWidget {
  const _SectionRows({
    required this.ledgerId,
    required this.sectionId,
    required this.columns,
  });

  final String ledgerId;
  final String sectionId;
  final List<LedgerColumn> columns;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final rows = ref.watch(
      ledgerSectionRowsProvider((ledgerId: ledgerId, sectionId: sectionId)),
    );

    return rows.when(
      loading: () => const Padding(
        padding: EdgeInsets.all(16),
        child: Center(child: CircularProgressIndicator()),
      ),
      error: (error, _) => Padding(
        padding: const EdgeInsets.all(16),
        child: Text(
          error is ApiException ? error.describe(l10n) : l10n.errorUnknown,
        ),
      ),
      data: (section) {
        if (section.rows.isEmpty) {
          return Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Text(l10n.ledgersSectionEmpty),
          );
        }

        return Column(
          children: [
            const Divider(height: 1),
            for (final row in section.rows) _LedgerRowTile(row: row, columns: columns),
          ],
        );
      },
    );
  }
}

class _LedgerRowTile extends StatelessWidget {
  const _LedgerRowTile({required this.row, required this.columns});

  final LedgerRow row;
  final List<LedgerColumn> columns;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final columnNameById = {for (final c in columns) c.id: c.name};

    final nonEmptyCells =
        row.cells.where((cell) => (cell.value ?? '').trim().isNotEmpty).toList();

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            row.label,
            style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
          ),
          if (row.linkedName != null)
            Text(
              row.linkedName!,
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          if (nonEmptyCells.isNotEmpty) ...[
            const SizedBox(height: 6),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: [
                for (final cell in nonEmptyCells)
                  Chip(
                    label: Text(
                      '${columnNameById[cell.columnId] ?? '?'}: ${cell.value}',
                    ),
                    visualDensity: VisualDensity.compact,
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
