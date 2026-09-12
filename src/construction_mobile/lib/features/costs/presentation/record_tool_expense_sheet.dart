import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/idempotency.dart';
import '../../tools/data/models/tool.dart';
import '../../tools/data/tool_repository.dart';
import 'tool_expenses_controller.dart';

const _recordableKinds = <String>['Repair', 'Maintenance', 'Calibration', 'Other'];

Future<void> showRecordToolExpenseSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _RecordToolExpenseSheet(),
  );
}

/// The tools a foreman can pick from. Cached for the session: the toolbox
/// does not change between two repairs.
final _toolOptionsProvider = FutureProvider<List<Tool>>((ref) async {
  final page =
      await ref.read(toolRepositoryProvider).fetchTools(pageSize: 100, sortBy: 'name');

  return page.items;
});

class _RecordToolExpenseSheet extends ConsumerStatefulWidget {
  const _RecordToolExpenseSheet();

  @override
  ConsumerState<_RecordToolExpenseSheet> createState() =>
      _RecordToolExpenseSheetState();
}

class _RecordToolExpenseSheetState extends ConsumerState<_RecordToolExpenseSheet> {
  final _amountController = TextEditingController();
  final _supplierController = TextEditingController();
  final _noteController = TextEditingController();

  String _kind = 'Repair';
  String? _toolId;
  bool _busy = false;

  /// Names this attempt at recording the expense — see `_RecordExpenseSheet`
  /// for the full rationale (never regenerated on a retry).
  final String _idempotencyKey = newIdempotencyKey();

  @override
  void dispose() {
    _amountController.dispose();
    _supplierController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  double? get _amount => double.tryParse(_amountController.text.replaceAll(',', '.'));

  bool get _canSubmit => _toolId != null && _amount != null && _amount! >= 0;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final tools = ref.watch(_toolOptionsProvider);

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
              l10n.toolExpensesRecord,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            tools.when(
              loading: () => const LinearProgressIndicator(),
              error: (error, _) => Text(
                error is ApiException ? error.describe(l10n) : l10n.errorUnknown,
                style: TextStyle(color: theme.colorScheme.error),
              ),
              data: (options) => DropdownButtonFormField<String>(
                initialValue: _toolId,
                decoration: InputDecoration(
                  labelText: l10n.toolExpensesTool,
                  helperText:
                      _toolId == null ? l10n.toolExpensesNeedsTool : null,
                ),
                items: [
                  for (final tool in options)
                    DropdownMenuItem(
                      value: tool.id,
                      child: Text(tool.name, overflow: TextOverflow.ellipsis),
                    ),
                ],
                onChanged: (value) => setState(() => _toolId = value),
              ),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _kind,
              decoration: InputDecoration(labelText: l10n.toolExpensesKind),
              items: [
                for (final kind in _recordableKinds)
                  DropdownMenuItem(
                    value: kind,
                    child: Text(enumLabel(l10n, EnumKind.toolExpenseKind, kind)),
                  ),
              ],
              onChanged: (value) {
                if (value != null) setState(() => _kind = value);
              },
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _amountController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: l10n.toolExpensesAmount,
                helperText: _amount == null && _amountController.text.isNotEmpty
                    ? l10n.toolExpensesNeedsAmount
                    : null,
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _supplierController,
              decoration: InputDecoration(labelText: l10n.toolExpensesSupplier),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _noteController,
              maxLines: 2,
              decoration: InputDecoration(labelText: l10n.toolExpensesNote),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || !_canSubmit ? null : _send,
                child: Text(l10n.toolExpensesSend),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _send() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final supplier = _supplierController.text.trim();
    final note = _noteController.text.trim();

    setState(() => _busy = true);

    try {
      await ref.read(toolExpensesControllerProvider.notifier).record(
            toolId: _toolId!,
            kind: _kind,
            amount: _amount!,
            supplier: supplier.isEmpty ? null : supplier,
            note: note.isEmpty ? null : note,
            idempotencyKey: _idempotencyKey,
          );

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.toolExpensesSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
