import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/confirm_dialog.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../customers/data/customer_repository.dart';
import '../data/models/tool.dart';
import '../data/models/tool_rental_out.dart';
import '../data/tool_rental_out_repository.dart';
import 'tools_controller.dart';

final _toolRentalsOutProvider = FutureProvider.autoDispose
    .family<List<ToolRentalOut>, String>((ref, toolId) async {
  final page = await ref
      .watch(toolRentalOutRepositoryProvider)
      .fetch(toolId: toolId, pageSize: 10);
  return page.items;
});

/// Loans of this tool out to another company or person — see
/// `VehicleRentalOutSection` for the pattern this mirrors.
class ToolRentalOutSection extends ConsumerWidget {
  const ToolRentalOutSection({super.key, required this.tool});

  final Tool tool;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final user = ref.watch(currentUserProvider);
    final canRecord = user?.canViewDirectory ?? false;
    final canDelete = user?.isProjectManagerAndAbove ?? false;
    final rentals = ref.watch(_toolRentalsOutProvider(tool.id));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  l10n.rentalOutTitle,
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
              ),
              if (canRecord && tool.status == 'Available')
                TextButton.icon(
                  onPressed: () => showToolRentalOutFormSheet(
                    context,
                    ref,
                    toolId: tool.id,
                  ),
                  icon: const Icon(Icons.handshake_outlined, size: 18),
                  label: Text(l10n.rentalOutAdd),
                ),
            ],
          ),
        ),
        Card(
          child: rentals.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (_, _) => const SizedBox.shrink(),
            data: (items) {
              if (items.isEmpty) {
                return ListTile(
                  leading: const Icon(Icons.handshake_outlined),
                  title: Text(l10n.rentalOutEmpty),
                );
              }

              return Column(
                children: [
                  for (final rental in items)
                    InkWell(
                      onTap: canRecord
                          ? () => showToolRentalOutFormSheet(
                                context,
                                ref,
                                toolId: tool.id,
                                existing: rental,
                              )
                          : null,
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(16, 10, 8, 10),
                        child: Row(
                          children: [
                            const Icon(Icons.handshake_outlined),
                            const SizedBox(width: 16),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${rental.renterDisplayName} · '
                                    '${formatAmount(rental.dailyRate)}/d',
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${formatDate(rental.startDate)} – '
                                    '${rental.endDate == null ? l10n.rentalOutStillOut : formatDate(rental.endDate)}',
                                    style: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.copyWith(
                                          color: Theme.of(context)
                                              .colorScheme
                                              .onSurfaceVariant,
                                        ),
                                  ),
                                ],
                              ),
                            ),
                            if (rental.isOpen && canRecord)
                              IconButton(
                                icon: const Icon(Icons.assignment_return_outlined),
                                tooltip: l10n.rentalOutReturn,
                                onPressed: () =>
                                    _returnRental(context, ref, rental),
                              ),
                            if (canDelete)
                              IconButton(
                                icon: const Icon(Icons.delete_outline),
                                onPressed: () =>
                                    _deleteRental(context, ref, rental),
                              ),
                          ],
                        ),
                      ),
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

Future<void> _returnRental(
  BuildContext context,
  WidgetRef ref,
  ToolRentalOut rental,
) async {
  final l10n = context.l10n;
  final messenger = ScaffoldMessenger.of(context);

  try {
    await ref.read(toolRentalOutRepositoryProvider).returnRental(rental.id);
    ref.invalidate(_toolRentalsOutProvider(rental.toolId));
    ref.invalidate(toolDetailProvider(rental.toolId));
    await ref.read(toolsControllerProvider.notifier).refresh();
    messenger.showSnackBar(SnackBar(content: Text(l10n.rentalOutReturned)));
  } on ApiException catch (exception) {
    messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
  }
}

Future<void> _deleteRental(
  BuildContext context,
  WidgetRef ref,
  ToolRentalOut rental,
) async {
  final l10n = context.l10n;

  final confirmed = await showConfirmDialog(
    context,
    title: l10n.rentalOutDeleteTitle,
    body: l10n.rentalOutDeleteBody,
    destructive: true,
  );

  if (!confirmed || !context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);

  try {
    await ref.read(toolRentalOutRepositoryProvider).remove(rental.id);
    ref.invalidate(_toolRentalsOutProvider(rental.toolId));
    ref.invalidate(toolDetailProvider(rental.toolId));
    await ref.read(toolsControllerProvider.notifier).refresh();
  } on ApiException catch (exception) {
    messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
  }
}

Future<void> showToolRentalOutFormSheet(
  BuildContext context,
  WidgetRef ref, {
  required String toolId,
  ToolRentalOut? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _RentalOutFormSheet(toolId: toolId, existing: existing),
  );
}

class _RentalOutFormSheet extends ConsumerStatefulWidget {
  const _RentalOutFormSheet({required this.toolId, this.existing});

  final String toolId;
  final ToolRentalOut? existing;

  @override
  ConsumerState<_RentalOutFormSheet> createState() =>
      _RentalOutFormSheetState();
}

class _RentalOutFormSheetState extends ConsumerState<_RentalOutFormSheet> {
  late final _renterNameController =
      TextEditingController(text: widget.existing?.renterName);
  late final _dailyRateController = TextEditingController(
    text: widget.existing?.dailyRate.toString(),
  );
  late final _noteController = TextEditingController(text: widget.existing?.note);

  String? _customerId;
  DateTime? _startDate;

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _customerId = widget.existing?.customerId;
    _startDate = widget.existing?.startDate;
  }

  @override
  void dispose() {
    _renterNameController.dispose();
    _dailyRateController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final customers = ref.watch(allCustomersProvider);

    final rate = double.tryParse(_dailyRateController.text.trim());
    final canSubmit = !_busy &&
        _renterNameController.text.trim().isNotEmpty &&
        rate != null &&
        rate > 0;

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
              _isEditing ? l10n.rentalOutEditTitle : l10n.rentalOutAdd,
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
              controller: _renterNameController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.rentalOutRenterName),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            customers.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) => DropdownButtonFormField<String?>(
                initialValue: _customerId,
                decoration: InputDecoration(labelText: l10n.rentalOutCustomer),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(l10n.rentalOutNoCustomer),
                  ),
                  for (final customer in items)
                    DropdownMenuItem(value: customer.id, child: Text(customer.name)),
                ],
                onChanged: _busy
                    ? null
                    : (value) => setState(() => _customerId = value),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _dailyRateController,
              enabled: !_busy,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: l10n.rentalOutDailyRate),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            _DatePickerField(
              label: l10n.rentalOutStartDate,
              value: _startDate,
              enabled: !_busy,
              onChanged: (value) => setState(() => _startDate = value),
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
    final repository = ref.read(toolRentalOutRepositoryProvider);
    final rate = double.parse(_dailyRateController.text.trim());

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          customerId: _customerId,
          renterName: _renterNameController.text.trim(),
          dailyRate: rate,
          startDate: _startDate ?? widget.existing!.startDate,
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        );
      } else {
        await repository.record(
          toolId: widget.toolId,
          customerId: _customerId,
          renterName: _renterNameController.text.trim(),
          dailyRate: rate,
          startDate: _startDate,
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        );
      }

      ref.invalidate(_toolRentalsOutProvider(widget.toolId));
      ref.invalidate(toolDetailProvider(widget.toolId));
      await ref.read(toolsControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.rentalOutSaved)));
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
