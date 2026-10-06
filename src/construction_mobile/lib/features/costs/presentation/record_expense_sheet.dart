import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/idempotency.dart';
import '../../attachments/data/attachment_repository.dart';
import '../../vehicles/data/models/vehicle.dart';
import '../../vehicles/data/vehicle_repository.dart';
import 'vehicle_expenses_controller.dart';
import '../../auth/presentation/auth_controller.dart';

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
  ref.watch(currentUserProvider.select((user) => user?.id));

  // A Worker is not served the fleet; what they may record fuel for is what is signed out to them.
  if (ref.watch(currentUserProvider.select((user) => user?.role)) == 'Worker') {
    return ref.read(vehicleRepositoryProvider).fetchMyVehicles();
  }

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

  /// The photograph of the receipt. A fill-up is not accepted without one.
  XFile? _receipt;

  /// Set once the cost itself has reached the server, so that a failed photo
  /// upload is retried on its own instead of recording the fuel a second time.
  String? _recordedId;

  /// Names this attempt at recording the expense.
  ///
  /// Created once, with the sheet, and deliberately not regenerated on a
  /// retry: a failed send may well have reached the server and lost its answer
  /// on the way back, and pressing the button again with a fresh key would
  /// book the fuel twice. The sheet closes on success, so the key never
  /// outlives the expense it named.
  final String _idempotencyKey = newIdempotencyKey();

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

  /// A Worker records fuel and nothing else; the API refuses the rest.
  List<String> get _kinds => ref.read(currentUserProvider)?.role == 'Worker' ? const ['Fuel'] : _recordableKinds;

  double? get _amount => double.tryParse(_amountController.text.replaceAll(',', '.'));

  double? get _litres => double.tryParse(_litresController.text.replaceAll(',', '.'));

  int? get _odometer => int.tryParse(_odometerController.text.trim());

  bool get _canSubmit {
    if (_vehicleId == null || _amount == null || _amount! < 0) {
      return false;
    }

    if (!_isFuel) {
      return true;
    }

    // A fill-up is checked against the fuel-card statement later, so it has to carry
    // everything that check needs: litres (the database refuses it without them),
    // the odometer reading, and the receipt itself.
    return _litres != null && _litres! > 0 && _odometer != null && _odometer! >= 0 && _receipt != null;
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
              data: (options) => options.isEmpty
                  ? Text(
                      l10n.vehicleExpensesNoVehicle,
                      style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.error),
                    )
                  : DropdownButtonFormField<String>(
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
                for (final kind in _kinds)
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
                helperText: _isFuel
                    ? l10n.vehicleExpensesOdometerRequired
                    : l10n.vehicleExpensesOdometerHint,
              ),
              onChanged: (_) => setState(() {}),
            ),
            if (_isFuel) ...[
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: _busy ? null : _pickReceipt,
                icon: Icon(_receipt == null ? Icons.photo_camera_outlined : Icons.check_circle_outline),
                label: Text(_receipt == null ? l10n.vehicleExpensesReceiptAttach : _receipt!.name),
              ),
              if (_receipt == null)
                Padding(
                  padding: const EdgeInsets.only(top: 4, left: 12),
                  child: Text(
                    l10n.vehicleExpensesReceiptNeeded,
                    style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                  ),
                ),
            ],
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

  /// The camera or the gallery, the way the other photo flows in the app offer it.
  Future<void> _pickReceipt() async {
    final l10n = context.l10n;

    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: Text(l10n.attachmentsTakePhoto),
              onTap: () => Navigator.of(sheetContext).pop(ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: Text(l10n.attachmentsFromGallery),
              onTap: () => Navigator.of(sheetContext).pop(ImageSource.gallery),
            ),
          ],
        ),
      ),
    );

    if (source == null || !mounted) {
      return;
    }

    // Re-encoded on the way out: a phone camera frame is several megabytes, far over the
    // upload limit and far more than a legible receipt needs.
    final picked = await ImagePicker().pickImage(
      source: source,
      maxWidth: 2048,
      maxHeight: 2048,
      imageQuality: 85,
    );

    if (picked != null && mounted) {
      setState(() => _receipt = picked);
    }
  }

  Future<void> _send() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final supplier = _supplierController.text.trim();
    final note = _noteController.text.trim();

    setState(() => _busy = true);

    try {
      if (_recordedId == null) {
        final expense = await ref.read(vehicleExpensesControllerProvider.notifier).record(
              vehicleId: _vehicleId!,
              kind: _kind,
              amount: _amount!,
              litres: _isFuel ? _litres : null,
              odometerKm: _odometer,
              supplier: supplier.isEmpty ? null : supplier,
              note: note.isEmpty ? null : note,
              idempotencyKey: _idempotencyKey,
            );

        _recordedId = expense.id;
      }

      final receipt = _receipt;

      if (_isFuel && receipt != null) {
        try {
          await ref.read(attachmentRepositoryProvider).upload(
                ownerType: 'VehicleExpense',
                ownerId: _recordedId!,
                category: 'Photo',
                filePath: receipt.path,
                fileName: receipt.name,
              );
        } on ApiException {
          // The cost is saved; only the picture is missing. Say so, and keep the sheet open so the
          // next press sends the picture alone instead of booking the fuel a second time.
          messenger.showSnackBar(SnackBar(content: Text(l10n.vehicleExpensesReceiptUploadFailed)));

          if (mounted) {
            setState(() => _busy = false);
          }

          return;
        }
      }

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
