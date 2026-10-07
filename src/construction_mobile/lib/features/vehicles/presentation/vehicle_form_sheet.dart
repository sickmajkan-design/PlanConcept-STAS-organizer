import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../data/models/vehicle.dart';
import '../data/vehicle_repository.dart';
import 'vehicles_controller.dart';

const _fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid', 'Lpg'];
const _ownershipTypes = ['Owned', 'Rented'];

/// One sheet for both create and edit: [existing] null means a blank form
/// that calls `create()`; passing a vehicle pre-fills every field and
/// switches submit to `update()`. Mirrors the compose-sheet shape already
/// established by `submit_weekly_report_sheet.dart`/`notify_employee_sheet.dart`.
Future<void> showVehicleFormSheet(
  BuildContext context,
  WidgetRef ref, {
  Vehicle? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _VehicleFormSheet(existing: existing),
  );
}

class _VehicleFormSheet extends ConsumerStatefulWidget {
  const _VehicleFormSheet({this.existing});

  final Vehicle? existing;

  @override
  ConsumerState<_VehicleFormSheet> createState() => _VehicleFormSheetState();
}

class _VehicleFormSheetState extends ConsumerState<_VehicleFormSheet> {
  late final _brandController =
      TextEditingController(text: widget.existing?.brand);
  late final _modelController =
      TextEditingController(text: widget.existing?.model);
  late final _registrationController =
      TextEditingController(text: widget.existing?.registrationNumber);
  late final _vinController = TextEditingController(text: widget.existing?.vin);
  late final _tdController = TextEditingController(text: widget.existing?.tdNumber);

  // `YYYY-MM-DD`, or null when not set.
  late String? _registrationValidUntil = widget.existing?.registrationValidUntil;
  late String? _technicalInspectionValidUntil = widget.existing?.technicalInspectionValidUntil;
  late String? _insuranceValidUntil = widget.existing?.insuranceValidUntil;
  late String? _nextServiceDue = widget.existing?.nextServiceDue;
  late String? _rentedUntil = widget.existing?.rentedUntil;

  late String _fuelType = widget.existing?.fuelType ?? _fuelTypes.first;
  late String _status = widget.existing?.status ?? 'Available';
  late String _ownershipType = widget.existing?.ownershipType ?? 'Owned';

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void dispose() {
    _brandController.dispose();
    _modelController.dispose();
    _registrationController.dispose();
    _vinController.dispose();
    _tdController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final canSubmit = !_busy &&
        _brandController.text.trim().isNotEmpty &&
        _modelController.text.trim().isNotEmpty &&
        _registrationController.text.trim().isNotEmpty;

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
              _isEditing ? l10n.vehicleFormEditTitle : l10n.vehicleFormAddTitle,
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
              controller: _brandController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.vehicleFormBrand),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _modelController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.vehicleFormModel),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _registrationController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.vehicleRegistration),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _tdController,
              enabled: !_busy,
              decoration: InputDecoration(
                labelText: l10n.vehicleTdNumber,
                helperText: l10n.vehicleTdNumberHint,
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _vinController,
              enabled: !_busy,
              decoration: const InputDecoration(labelText: 'VIN'),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _fuelType,
              decoration: InputDecoration(labelText: l10n.vehicleFuelType),
              items: [
                for (final value in _fuelTypes)
                  DropdownMenuItem(
                    value: value,
                    child: Text(enumLabel(l10n, EnumKind.fuelType, value)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) setState(() => _fuelType = value);
                    },
            ),
            if (_isEditing) ...[
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: InputDecoration(labelText: l10n.commonStatus),
                items: [
                  for (final value in vehicleStatusFilters)
                    DropdownMenuItem(
                      value: value,
                      child: Text(enumLabel(l10n, EnumKind.vehicleStatus, value)),
                    ),
                ],
                onChanged: _busy
                    ? null
                    : (value) {
                        if (value != null) setState(() => _status = value);
                      },
              ),
            ],
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _ownershipType,
              decoration: InputDecoration(labelText: l10n.vehicleOwnershipType),
              items: [
                for (final value in _ownershipTypes)
                  DropdownMenuItem(
                    value: value,
                    child: Text(enumLabel(l10n, EnumKind.vehicleOwnershipType, value)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) setState(() => _ownershipType = value);
                    },
            ),
            const SizedBox(height: 12),
            _DateField(
              label: l10n.vehicleRegistrationValidUntil,
              value: _registrationValidUntil,
              enabled: !_busy,
              onChanged: (value) => setState(() => _registrationValidUntil = value),
            ),
            const SizedBox(height: 12),
            _DateField(
              label: l10n.vehicleInspectionValidUntil,
              value: _technicalInspectionValidUntil,
              enabled: !_busy,
              onChanged: (value) => setState(() => _technicalInspectionValidUntil = value),
            ),
            const SizedBox(height: 12),
            _DateField(
              label: l10n.vehicleInsuranceValidUntil,
              value: _insuranceValidUntil,
              enabled: !_busy,
              onChanged: (value) => setState(() => _insuranceValidUntil = value),
            ),
            const SizedBox(height: 12),
            _DateField(
              label: l10n.vehicleNextServiceDue,
              value: _nextServiceDue,
              enabled: !_busy,
              onChanged: (value) => setState(() => _nextServiceDue = value),
            ),
            if (_ownershipType != 'Owned') ...[
              const SizedBox(height: 12),
              _DateField(
                label: l10n.vehicleRentedUntil,
                value: _rentedUntil,
                enabled: !_busy,
                onChanged: (value) => setState(() => _rentedUntil = value),
              ),
            ],
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
                    : Text(_isEditing ? l10n.commonSave : l10n.commonAdd),
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
    final repository = ref.read(vehicleRepositoryProvider);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          brand: _brandController.text.trim(),
          model: _modelController.text.trim(),
          registrationNumber: _registrationController.text.trim(),
          tdNumber: _tdController.text.trim().isEmpty ? null : _tdController.text.trim(),
          vin: _vinController.text.trim().isEmpty
              ? null
              : _vinController.text.trim(),
          fuelType: _fuelType,
          status: _status,
          ownershipType: _ownershipType,
          registrationValidUntil: _registrationValidUntil,
          technicalInspectionValidUntil: _technicalInspectionValidUntil,
          insuranceValidUntil: _insuranceValidUntil,
          nextServiceDue: _nextServiceDue,
          rentedUntil: _ownershipType == 'Owned' ? null : _rentedUntil,
          qrCode: widget.existing!.qrCode,
          gpsProvider: widget.existing!.gpsProvider,
          gpsTrackingUrl: widget.existing!.gpsTrackingUrl,
          branchId: widget.existing!.branchId,
        );
        ref.invalidate(vehicleDetailProvider(widget.existing!.id));
      } else {
        await repository.create(
          brand: _brandController.text.trim(),
          model: _modelController.text.trim(),
          registrationNumber: _registrationController.text.trim(),
          tdNumber: _tdController.text.trim().isEmpty ? null : _tdController.text.trim(),
          vin: _vinController.text.trim().isEmpty
              ? null
              : _vinController.text.trim(),
          fuelType: _fuelType,
          ownershipType: _ownershipType,
          registrationValidUntil: _registrationValidUntil,
          technicalInspectionValidUntil: _technicalInspectionValidUntil,
          insuranceValidUntil: _insuranceValidUntil,
          nextServiceDue: _nextServiceDue,
          rentedUntil: _ownershipType == 'Owned' ? null : _rentedUntil,
        );
      }

      await ref.read(vehiclesControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(_isEditing ? l10n.vehicleFormSaved : l10n.vehicleFormAdded),
      ));
    } on ApiException catch (exception) {
      setState(() {
        _error = exception;
        _busy = false;
      });
    }
  }
}

/// A date that can be picked and cleared, shown as DD.MM.YYYY. and carried as `YYYY-MM-DD`.
class _DateField extends StatelessWidget {
  const _DateField({
    required this.label,
    required this.value,
    required this.enabled,
    required this.onChanged,
  });

  final String label;
  final String? value;
  final bool enabled;
  final ValueChanged<String?> onChanged;

  static String _iso(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  Future<void> _pick(BuildContext context) async {
    final current = value == null ? null : DateTime.tryParse(value!);

    final picked = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime.now(),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );

    if (picked != null) {
      onChanged(_iso(picked));
    }
  }

  @override
  Widget build(BuildContext context) {
    final current = value == null ? null : DateTime.tryParse(value!);

    return InkWell(
      onTap: enabled ? () => _pick(context) : null,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          suffixIcon: current == null
              ? const Icon(Icons.calendar_today_outlined)
              : IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: enabled ? () => onChanged(null) : null,
                ),
        ),
        child: Text(current == null ? '—' : formatDate(current)),
      ),
    );
  }
}
