import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../data/employee_repository.dart';
import '../data/models/employee.dart';
import 'employees_controller.dart';

const _employeeTypes = ['Employee', 'Subcontractor'];

/// One sheet for both create and edit — see `VehicleFormSheet` for the
/// pattern this mirrors.
Future<void> showEmployeeFormSheet(
  BuildContext context,
  WidgetRef ref, {
  EmployeeDetail? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _EmployeeFormSheet(existing: existing),
  );
}

class _EmployeeFormSheet extends ConsumerStatefulWidget {
  const _EmployeeFormSheet({this.existing});

  final EmployeeDetail? existing;

  @override
  ConsumerState<_EmployeeFormSheet> createState() =>
      _EmployeeFormSheetState();
}

class _EmployeeFormSheetState extends ConsumerState<_EmployeeFormSheet> {
  late final _numberController =
      TextEditingController(text: widget.existing?.employeeNumber);
  late final _firstNameController =
      TextEditingController(text: widget.existing?.firstName);
  late final _lastNameController =
      TextEditingController(text: widget.existing?.lastName);
  late final _phoneController = TextEditingController(text: widget.existing?.phone);
  late final _emailController = TextEditingController(text: widget.existing?.email);
  late final _addressController =
      TextEditingController(text: widget.existing?.address);
  late final _positionController =
      TextEditingController(text: widget.existing?.position);

  DateTime? _dateOfBirth;
  DateTime? _employmentDate;
  late String _status = widget.existing?.status ?? 'Active';
  late String _type = widget.existing?.type ?? 'Employee';

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _dateOfBirth = widget.existing?.dateOfBirth;
    _employmentDate = widget.existing?.employmentDate;
  }

  @override
  void dispose() {
    _numberController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _addressController.dispose();
    _positionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final canSubmit = !_busy &&
        _numberController.text.trim().isNotEmpty &&
        _firstNameController.text.trim().isNotEmpty &&
        _lastNameController.text.trim().isNotEmpty &&
        _positionController.text.trim().isNotEmpty &&
        _employmentDate != null;

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
              _isEditing
                  ? l10n.employeeFormEditTitle
                  : l10n.employeeFormAddTitle,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
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
              controller: _numberController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.employeeFormNumber),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _firstNameController,
                    enabled: !_busy,
                    decoration:
                        InputDecoration(labelText: l10n.employeeFormFirstName),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: _lastNameController,
                    enabled: !_busy,
                    decoration:
                        InputDecoration(labelText: l10n.employeeFormLastName),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _positionController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.employeePosition),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _phoneController,
              enabled: !_busy,
              keyboardType: TextInputType.phone,
              decoration: InputDecoration(labelText: l10n.employeePhone),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _emailController,
              enabled: !_busy,
              keyboardType: TextInputType.emailAddress,
              decoration: InputDecoration(labelText: l10n.employeeEmail),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _addressController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.employeeAddress),
            ),
            const SizedBox(height: 12),
            _DatePickerField(
              label: l10n.employeeFormEmploymentDate,
              value: _employmentDate,
              enabled: !_busy,
              onChanged: (value) => setState(() => _employmentDate = value),
            ),
            const SizedBox(height: 12),
            _DatePickerField(
              label: l10n.employeeDateOfBirth,
              value: _dateOfBirth,
              enabled: !_busy,
              onChanged: (value) => setState(() => _dateOfBirth = value),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _type,
              decoration: InputDecoration(labelText: l10n.employeeType),
              items: [
                for (final value in _employeeTypes)
                  DropdownMenuItem(
                    value: value,
                    child: Text(enumLabel(l10n, EnumKind.employeeType, value)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) setState(() => _type = value);
                    },
            ),
            if (_isEditing) ...[
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: InputDecoration(labelText: l10n.commonStatus),
                items: [
                  for (final value in employeeStatusFilters)
                    DropdownMenuItem(
                      value: value,
                      child:
                          Text(enumLabel(l10n, EnumKind.employeeStatus, value)),
                    ),
                ],
                onChanged: _busy
                    ? null
                    : (value) {
                        if (value != null) setState(() => _status = value);
                      },
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
    final repository = ref.read(employeeRepositoryProvider);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          employeeNumber: _numberController.text.trim(),
          firstName: _firstNameController.text.trim(),
          lastName: _lastNameController.text.trim(),
          phone: _phoneController.text.trim().isEmpty
              ? null
              : _phoneController.text.trim(),
          email: _emailController.text.trim().isEmpty
              ? null
              : _emailController.text.trim(),
          address: _addressController.text.trim().isEmpty
              ? null
              : _addressController.text.trim(),
          dateOfBirth: _dateOfBirth,
          employmentDate: _employmentDate!,
          position: _positionController.text.trim(),
          status: _status,
          type: _type,
        );
        ref.invalidate(employeeDetailProvider(widget.existing!.id));
      } else {
        await repository.create(
          employeeNumber: _numberController.text.trim(),
          firstName: _firstNameController.text.trim(),
          lastName: _lastNameController.text.trim(),
          phone: _phoneController.text.trim().isEmpty
              ? null
              : _phoneController.text.trim(),
          email: _emailController.text.trim().isEmpty
              ? null
              : _emailController.text.trim(),
          address: _addressController.text.trim().isEmpty
              ? null
              : _addressController.text.trim(),
          dateOfBirth: _dateOfBirth,
          employmentDate: _employmentDate!,
          position: _positionController.text.trim(),
          type: _type,
        );
      }

      await ref.read(employeesControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(
          _isEditing ? l10n.employeeFormSaved : l10n.employeeFormAdded,
        ),
      ));
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
                firstDate: DateTime(1950),
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
