import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../vehicles/data/models/vehicle.dart';
import '../../vehicles/data/vehicle_repository.dart';
import 'vehicle_expenses_controller.dart';

/// The kinds worth offering on a phone.
///
/// Insurance and registration are annual, arrive as paperwork, and are typed
/// in at a desk. What happens away from one is fuel, a repair, and the
/// occasional service — so those are what the sheet offers.
const _recordableKinds = <String>['Fuel', 'Repair', 'Service', 'Other'];

Future<void> showRecordExpenseSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _RecordExpenseSheet(),
  );
}

/// The vehicles a foreman can pick from. Cached for the session: the fleet
/// does not change between two fill-ups.
final _vehicleOptionsProvider = FutureProvider<List<Vehicle>>((ref) async {
  final page = await ref
      .read(vehicleRepositoryProvider)
      .fetchVehicles(pageSize: 100, sortBy: 'registrationNumber');

  return page.items;
});

class _RecordExpenseSheet extends ConsumerStatefulWidget {
  const _RecordExpenseSheet();

  @override
  ConsumerState<_RecordExpenseSheet> createState() => _RecordExpenseSheetState();
}

class _RecordExpenseSheetState extends ConsumerState<_RecordExpenseSheet> {
  final _amountController = TextEditingController();
  final _litresController = TextEditingController();
  final _odometerController = TextEditingController();
  final _supplierController = TextEditingController();
  final _noteController = TextEditingController();

  String _kind = 'Fuel';
  String? _vehicleId;
  bool _busy = false;

  @override
  void dispose() {
    _amountController.dispose();
    _litresController.dispose();
    _odometerController.dispose();
    _supplierController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  bool get _isFuel => _kind == 'Fuel';

  double? get _amount => double.tryParse(_amountController.text.replaceAll(',', '.'));

  double? get _litres => double.tryParse(_litresController.text.replaceAll(',', '.'));

  bool get _canSubmit {
    if (_vehicleId == null || _amount == null || _amount! < 0) {
      return false;
    }

    // The database refuses a fill-up with no litres, so the button does too
    // rather than letting the request go and come back as an error.
    return !_isFuel || (_litres != null && _litres! > 0);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final vehicles = ref.watch(_vehicleOptionsProvider);

    return Padding(
      // Lifts the sheet clear of the keyboard, which covers most of a phone
      // once a number field has focus.
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
              l10n.vehicleExpensesRecord,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            vehicles.when(
              loading: () => const LinearProgressIndicator(),
              error: (error, _) => Text(
                error is ApiException
                    ? error.describe(l10n)
                    : l10n.errorUnknown,
                style: TextStyle(color: theme.colorScheme.error),
              ),
              data: (options) => DropdownButtonFormField<String>(
                initialValue: _vehicleId,
                decoration: InputDecoration(
                  labelText: l10n.vehicleExpensesVehicle,
                  helperText: _vehicleId == null
                      ? l10n.vehicleExpensesNeedsVehicle
                      : null,
                ),
                items: [
                  for (final vehicle in options)
                    DropdownMenuItem(
                      value: vehicle.id,
                      child: Text(
                        '${vehicle.brand} ${vehicle.model} · ${vehicle.registrationNumber}',
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                ],
                onChanged: (value) => setState(() => _vehicleId = value),
              ),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _kind,
              decoration: InputDecoration(labelText: l10n.vehicleExpensesKind),
              items: [
                for (final kind in _recordableKinds)
                  DropdownMenuItem(
                    value: kind,
                    child: Text(
                      enumLabel(l10n, EnumKind.vehicleExpenseKind, kind),
                    ),
                  ),
              ],
              onChanged: (value) {
                if (value != null) {
                  setState(() {
                    _kind = value;
                    // Litres belong to a fill-up alone. Leaving a stale value
                    // behind would send it on a repair, which the database
                    // refuses.
                    if (value != 'Fuel') {
                      _litresController.clear();
                    }
                  });
                }
              },
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _amountController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: l10n.vehicleExpensesAmount,
                helperText: _amount == null && _amountController.text.isNotEmpty
                    ? l10n.vehicleExpensesNeedsAmount
                    : null,
              ),
              onChanged: (_) => setState(() {}),
            ),
            if (_isFuel) ...[
              const SizedBox(height: 12),
              TextField(
                controller: _litresController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(
                  labelText: l10n.vehicleExpensesLitres,
                  helperText: l10n.vehicleExpensesFuelNeedsLitres,
                ),
                onChanged: (_) => setState(() {}),
              ),
            ],
            const SizedBox(height: 12),
            TextField(
              controller: _odometerController,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: l10n.vehicleExpensesOdometer,
                helperText: l10n.vehicleExpensesOdometerHint,
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _supplierController,
              decoration: InputDecoration(labelText: l10n.vehicleExpensesSupplier),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _noteController,
              maxLines: 2,
              decoration: InputDecoration(labelText: l10n.vehicleExpensesNote),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || !_canSubmit ? null : _send,
                child: Text(l10n.vehicleExpensesSend),
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
      await ref.read(vehicleExpensesControllerProvider.notifier).record(
            vehicleId: _vehicleId!,
            kind: _kind,
            amount: _amount!,
            litres: _isFuel ? _litres : null,
            odometerKm: int.tryParse(_odometerController.text.trim()),
            supplier: supplier.isEmpty ? null : supplier,
            note: note.isEmpty ? null : note,
          );

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.vehicleExpensesSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
