import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import 'article_orders_controller.dart';

Future<void> showRequestArticlesSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _RequestArticlesSheet(),
  );
}

class _Line {
  final name = TextEditingController();
  final quantity = TextEditingController(text: '1');
  final unit = TextEditingController();
  final note = TextEditingController();

  void dispose() {
    name.dispose();
    quantity.dispose();
    unit.dispose();
    note.dispose();
  }

  double get amount => double.tryParse(quantity.text.trim().replaceAll(',', '.')) ?? 0;

  bool get valid => name.text.trim().isNotEmpty && amount > 0;
}

class _RequestArticlesSheet extends ConsumerStatefulWidget {
  const _RequestArticlesSheet();

  @override
  ConsumerState<_RequestArticlesSheet> createState() => _RequestArticlesSheetState();
}

class _RequestArticlesSheetState extends ConsumerState<_RequestArticlesSheet> {
  final _lines = <_Line>[_Line()];
  final _note = TextEditingController();
  bool _urgent = false;
  bool _busy = false;

  @override
  void dispose() {
    for (final line in _lines) {
      line.dispose();
    }
    _note.dispose();
    super.dispose();
  }

  bool get _valid => _lines.every((line) => line.valid);

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
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
              l10n.articleOrdersNew,
              style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            for (var i = 0; i < _lines.length; i++) ...[
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 3,
                    child: TextField(
                      controller: _lines[i].name,
                      maxLength: 200,
                      onChanged: (_) => setState(() {}),
                      decoration: InputDecoration(labelText: l10n.articleOrdersItemName),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextField(
                      controller: _lines[i].quantity,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      onChanged: (_) => setState(() {}),
                      decoration: InputDecoration(labelText: l10n.articleOrdersQuantity),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextField(
                      controller: _lines[i].unit,
                      maxLength: 30,
                      decoration: InputDecoration(labelText: l10n.articleOrdersUnit),
                    ),
                  ),
                ],
              ),
              TextField(
                controller: _lines[i].note,
                maxLength: 300,
                decoration: InputDecoration(labelText: l10n.articleOrdersItemNote),
              ),
              if (_lines.length > 1)
                Align(
                  alignment: Alignment.centerRight,
                  child: TextButton.icon(
                    onPressed: () => setState(() => _lines.removeAt(i).dispose()),
                    icon: const Icon(Icons.delete_outline),
                    label: Text(l10n.articleOrdersRemoveItem),
                  ),
                ),
              const SizedBox(height: 8),
            ],
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton.icon(
                onPressed: _lines.length >= 30 ? null : () => setState(() => _lines.add(_Line())),
                icon: const Icon(Icons.add),
                label: Text(l10n.articleOrdersAddItem),
              ),
            ),
            TextField(
              controller: _note,
              maxLines: 2,
              maxLength: 1000,
              decoration: InputDecoration(labelText: l10n.articleOrdersNote),
            ),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(l10n.articleOrdersUrgent),
              value: _urgent,
              onChanged: (value) => setState(() => _urgent = value),
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || !_valid ? null : _send,
                child: Text(l10n.articleOrdersSend),
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
    final note = _note.text.trim();

    setState(() => _busy = true);

    try {
      await ref.read(articleOrdersControllerProvider.notifier).create(
        items: [
          for (final line in _lines)
            <String, dynamic>{
              'name': line.name.text.trim(),
              'quantity': line.amount,
              'unit': line.unit.text.trim().isEmpty ? null : line.unit.text.trim(),
              'note': line.note.text.trim().isEmpty ? null : line.note.text.trim(),
            },
        ],
        urgent: _urgent,
        note: note.isEmpty ? null : note,
      );

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.articleOrdersSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
