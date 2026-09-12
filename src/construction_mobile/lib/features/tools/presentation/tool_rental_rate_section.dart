import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/confirm_dialog.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/tool_rental_rate.dart';
import '../data/tool_rental_rate_repository.dart';
import 'tools_controller.dart';

final _toolRentalRatesProvider = FutureProvider.autoDispose
    .family<List<ToolRentalRate>, String>((ref, toolId) async {
  final page = await ref
      .watch(toolRentalRateRepositoryProvider)
      .fetch(toolId: toolId, pageSize: 10);
  return page.items;
});

/// The rent/lease rate history for a tool — see `VehicleRentalRateSection`
/// for the pattern this mirrors. Shown only once the tool itself is marked
/// `Rented`.
class ToolRentalRateSection extends ConsumerWidget {
  const ToolRentalRateSection({super.key, required this.toolId});

  final String toolId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final user = ref.watch(currentUserProvider);
    final canRecord = user?.canViewDirectory ?? false;
    final canDelete = user?.isProjectManagerAndAbove ?? false;
    final rates = ref.watch(_toolRentalRatesProvider(toolId));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  l10n.rentalRatesTitle,
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
              ),
              if (canRecord)
                TextButton.icon(
                  onPressed: () => showToolRentalRateFormSheet(
                    context,
                    ref,
                    toolId: toolId,
                  ),
                  icon: const Icon(Icons.add, size: 18),
                  label: Text(l10n.rentalRatesAdd),
                ),
            ],
          ),
        ),
        Card(
          child: rates.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (_, _) => const SizedBox.shrink(),
            data: (items) {
              if (items.isEmpty) {
                return ListTile(
                  leading: const Icon(Icons.request_quote_outlined),
                  title: Text(l10n.rentalRatesEmpty),
                );
              }

              return Column(
                children: [
                  for (final rate in items)
                    ListTile(
                      leading: const Icon(Icons.request_quote_outlined),
                      title: Text(
                        rate.provider?.isNotEmpty == true
                            ? '${formatAmount(rate.monthlyAmount)} · ${rate.provider}'
                            : formatAmount(rate.monthlyAmount),
                      ),
                      subtitle: Text(
                        '${formatDate(rate.startDate)} – '
                        '${rate.endDate == null ? l10n.rentalRatesOpenEnded : formatDate(rate.endDate)}',
                      ),
                      onTap: canRecord
                          ? () => showToolRentalRateFormSheet(
                                context,
                                ref,
                                toolId: toolId,
                                existing: rate,
                              )
                          : null,
                      trailing: canDelete
                          ? IconButton(
                              icon: const Icon(Icons.delete_outline),
                              onPressed: () => _deleteRate(context, ref, rate),
                            )
                          : null,
                    ),
                ],
              );
            },
          ),
        ),
      ],
    );
  }
}

Future<void> _deleteRate(
  BuildContext context,
  WidgetRef ref,
  ToolRentalRate rate,
) async {
  final l10n = context.l10n;

  final confirmed = await showConfirmDialog(
    context,
    title: l10n.rentalRatesDeleteTitle,
    body: l10n.rentalRatesDeleteBody,
    destructive: true,
  );

  if (!confirmed || !context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);

  try {
    await ref.read(toolRentalRateRepositoryProvider).remove(rate.id);
    ref.invalidate(_toolRentalRatesProvider(rate.toolId));
    ref.invalidate(toolDetailProvider(rate.toolId));
    await ref.read(toolsControllerProvider.notifier).refresh();
  } on ApiException catch (exception) {
    messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
  }
}

Future<void> showToolRentalRateFormSheet(
  BuildContext context,
  WidgetRef ref, {
  required String toolId,
  ToolRentalRate? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _RentalRateFormSheet(toolId: toolId, existing: existing),
  );
}

class _RentalRateFormSheet extends ConsumerStatefulWidget {
  const _RentalRateFormSheet({required this.toolId, this.existing});

  final String toolId;
  final ToolRentalRate? existing;

  @override
  ConsumerState<_RentalRateFormSheet> createState() =>
      _RentalRateFormSheetState();
}

class _RentalRateFormSheetState extends ConsumerState<_RentalRateFormSheet> {
  late final _amountController = TextEditingController(
    text: widget.existing?.monthlyAmount.toString(),
  );
  late final _providerController =
      TextEditingController(text: widget.existing?.provider);
  late final _noteController = TextEditingController(text: widget.existing?.note);

  DateTime? _startDate;
  DateTime? _endDate;

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _startDate = widget.existing?.startDate;
    _endDate = widget.existing?.endDate;
  }

  @override
  void dispose() {
    _amountController.dispose();
    _providerController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final amount = double.tryParse(_amountController.text.trim());
    final canSubmit = !_busy && amount != null && amount > 0;

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
              _isEditing ? l10n.rentalRatesEditTitle : l10n.rentalRatesAdd,
              style:
                  theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            if (_error != null) ...[
              Text(
                _error!.describe(l10n),
                style: TextStyle(color: theme.colorScheme.error),
              ),
              const SizedBox(height: 12),
            ],
            TextField(
              controller: _amountController,
              enabled: !_busy,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration:
                  InputDecoration(labelText: l10n.rentalRatesMonthlyAmount),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _providerController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.rentalRatesProvider),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _DatePickerField(
                    label: l10n.rentalRatesStartDate,
                    value: _startDate,
                    enabled: !_busy,
                    onChanged: (value) => setState(() => _startDate = value),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _DatePickerField(
                    label: l10n.rentalRatesEndDate,
                    value: _endDate,
                    enabled: !_busy,
                    onChanged: (value) => setState(() => _endDate = value),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _noteController,
              enabled: !_busy,
              maxLines: 2,
              decoration: InputDecoration(labelText: l10n.rentalRatesNote),
            ),
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: canSubmit ? _submit : null,
                child: _busy
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(l10n.commonSave),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final repository = ref.read(toolRentalRateRepositoryProvider);
    final amount = double.parse(_amountController.text.trim());

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          monthlyAmount: amount,
          provider: _providerController.text.trim().isEmpty
              ? null
              : _providerController.text.trim(),
          startDate: _startDate ?? widget.existing!.startDate,
          endDate: _endDate,
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        );
      } else {
        await repository.set(
          toolId: widget.toolId,
          monthlyAmount: amount,
          provider: _providerController.text.trim().isEmpty
              ? null
              : _providerController.text.trim(),
          startDate: _startDate,
          endDate: _endDate,
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        );
      }

      ref.invalidate(_toolRentalRatesProvider(widget.toolId));
      ref.invalidate(toolDetailProvider(widget.toolId));
      await ref.read(toolsControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.rentalRatesSaved)));
    } on ApiException catch (exception) {
      setState(() {
        _error = exception;
        _busy = false;
      });
    }
  }
}

class _DatePickerField extends StatelessWidget {
  const _DatePickerField({
    required this.label,
    required this.value,
    required this.enabled,
    required this.onChanged,
  });

  final String label;
  final DateTime? value;
  final bool enabled;
  final ValueChanged<DateTime?> onChanged;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: enabled
          ? () async {
              final picked = await showDatePicker(
                context: context,
                initialDate: value ?? DateTime.now(),
                firstDate: DateTime(2000),
                lastDate: DateTime(2100),
              );
              if (picked != null) onChanged(picked);
            }
          : null,
      child: InputDecorator(
        decoration: InputDecoration(labelText: label),
        child: Text(value == null ? '' : formatDate(value)),
      ),
    );
  }
}
