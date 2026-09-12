import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../customers/data/customer_repository.dart';
import '../data/models/project.dart';
import '../data/project_repository.dart';
import 'projects_controller.dart';

final _allProjectsForParentPickerProvider =
    FutureProvider.autoDispose((ref) {
  return ref.watch(projectRepositoryProvider).fetchAll();
});

/// One sheet for both create and edit — see `VehicleFormSheet` for the
/// pattern this mirrors. The one wrinkle here: picking a Parent Project
/// locks the Customer field to the parent's customer, mirroring the
/// backend's own rule that a sub-project always inherits its parent's
/// customer regardless of what was sent.
Future<void> showProjectFormSheet(
  BuildContext context,
  WidgetRef ref, {
  ProjectDetail? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _ProjectFormSheet(existing: existing),
  );
}

class _ProjectFormSheet extends ConsumerStatefulWidget {
  const _ProjectFormSheet({this.existing});

  final ProjectDetail? existing;

  @override
  ConsumerState<_ProjectFormSheet> createState() => _ProjectFormSheetState();
}

class _ProjectFormSheetState extends ConsumerState<_ProjectFormSheet> {
  late final _nameController = TextEditingController(text: widget.existing?.name);
  late final _descriptionController =
      TextEditingController(text: widget.existing?.description);
  late final _addressController =
      TextEditingController(text: widget.existing?.address);
  late final _latitudeController = TextEditingController(
    text: widget.existing?.latitude?.toString(),
  );
  late final _longitudeController = TextEditingController(
    text: widget.existing?.longitude?.toString(),
  );
  late final _countryCodeController =
      TextEditingController(text: widget.existing?.countryCode);
  late final _contractValueController = TextEditingController(
    text: widget.existing?.contractValue?.toString(),
  );

  String? _customerId;
  String? _parentProjectId;
  TimeOfDay? _shiftStartTime;
  DateTime? _startDate;
  DateTime? _endDate;
  late String _status = widget.existing?.status ?? 'Planned';

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _customerId = widget.existing?.customerId;
    _parentProjectId = widget.existing?.parentProjectId;
    _startDate = widget.existing?.startDate;
    _endDate = widget.existing?.endDate;

    final rawShift = widget.existing?.shiftStartTime;
    if (rawShift != null) {
      final parts = rawShift.split(':');
      if (parts.length >= 2) {
        _shiftStartTime = TimeOfDay(
          hour: int.tryParse(parts[0]) ?? 0,
          minute: int.tryParse(parts[1]) ?? 0,
        );
      }
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _addressController.dispose();
    _latitudeController.dispose();
    _longitudeController.dispose();
    _countryCodeController.dispose();
    _contractValueController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final customers = ref.watch(allCustomersProvider);
    final allProjects = ref.watch(_allProjectsForParentPickerProvider);

    final hasLatitude = _latitudeController.text.trim().isNotEmpty;
    final hasLongitude = _longitudeController.text.trim().isNotEmpty;
    final canSubmit = !_busy &&
        _nameController.text.trim().isNotEmpty &&
        hasLatitude == hasLongitude;

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
              _isEditing ? l10n.projectFormEditTitle : l10n.projectFormAddTitle,
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
              controller: _nameController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.projectFormName),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _descriptionController,
              enabled: !_busy,
              maxLines: 3,
              decoration:
                  InputDecoration(labelText: l10n.projectFormDescription),
            ),
            const SizedBox(height: 12),
            allProjects.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) {
                final parentOptions = items
                    .where((project) =>
                        project.kind == 'Main' && project.id != widget.existing?.id)
                    .toList();

                return DropdownButtonFormField<String?>(
                  initialValue: _parentProjectId,
                  decoration:
                      InputDecoration(labelText: l10n.projectFormParentProject),
                  items: [
                    DropdownMenuItem(
                      value: null,
                      child: Text(l10n.projectFormNoParent),
                    ),
                    for (final project in parentOptions)
                      DropdownMenuItem(value: project.id, child: Text(project.name)),
                  ],
                  onChanged: _busy
                      ? null
                      : (value) => setState(() {
                            _parentProjectId = value;
                            if (value != null) {
                              final parent = parentOptions
                                  .firstWhere((project) => project.id == value);
                              _customerId = parent.customerId;
                            }
                          }),
                );
              },
            ),
            const SizedBox(height: 12),
            customers.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) => DropdownButtonFormField<String?>(
                initialValue: _customerId,
                decoration: InputDecoration(labelText: l10n.projectClient),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(l10n.projectFormNoCustomer),
                  ),
                  for (final customer in items)
                    DropdownMenuItem(value: customer.id, child: Text(customer.name)),
                ],
                onChanged: (_busy || _parentProjectId != null)
                    ? null
                    : (value) => setState(() => _customerId = value),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _addressController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.projectAddress),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _latitudeController,
                    enabled: !_busy,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true, signed: true),
                    decoration:
                        InputDecoration(labelText: l10n.projectFormLatitude),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: _longitudeController,
                    enabled: !_busy,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true, signed: true),
                    decoration:
                        InputDecoration(labelText: l10n.projectFormLongitude),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _countryCodeController,
              enabled: !_busy,
              maxLength: 2,
              textCapitalization: TextCapitalization.characters,
              decoration: InputDecoration(labelText: l10n.projectFormCountryCode),
            ),
            const SizedBox(height: 12),
            _TimePickerField(
              label: l10n.projectFormShiftStartTime,
              value: _shiftStartTime,
              enabled: !_busy,
              onChanged: (value) => setState(() => _shiftStartTime = value),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _DatePickerField(
                    label: l10n.projectStartDate,
                    value: _startDate,
                    enabled: !_busy,
                    onChanged: (value) => setState(() => _startDate = value),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _DatePickerField(
                    label: l10n.projectEndDate,
                    value: _endDate,
                    enabled: !_busy,
                    onChanged: (value) => setState(() => _endDate = value),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _contractValueController,
              enabled: !_busy,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration:
                  InputDecoration(labelText: l10n.projectFormContractValue),
            ),
            if (_isEditing) ...[
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: InputDecoration(labelText: l10n.commonStatus),
                items: [
                  for (final value in projectStatusFilters)
                    DropdownMenuItem(
                      value: value,
                      child:
                          Text(enumLabel(l10n, EnumKind.projectStatus, value)),
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
    final repository = ref.read(projectRepositoryProvider);

    final latitude = double.tryParse(_latitudeController.text.trim());
    final longitude = double.tryParse(_longitudeController.text.trim());
    final contractValue = double.tryParse(_contractValueController.text.trim());
    final shiftStartTime = _shiftStartTime == null
        ? null
        : '${_shiftStartTime!.hour.toString().padLeft(2, '0')}:'
            '${_shiftStartTime!.minute.toString().padLeft(2, '0')}:00';

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          name: _nameController.text.trim(),
          description: _descriptionController.text.trim().isEmpty
              ? null
              : _descriptionController.text.trim(),
          customerId: _customerId,
          parentProjectId: _parentProjectId,
          address: _addressController.text.trim().isEmpty
              ? null
              : _addressController.text.trim(),
          latitude: latitude,
          longitude: longitude,
          countryCode: _countryCodeController.text.trim().isEmpty
              ? null
              : _countryCodeController.text.trim(),
          shiftStartTime: shiftStartTime,
          startDate: _startDate?.toIso8601String(),
          endDate: _endDate?.toIso8601String(),
          status: _status,
          contractValue: contractValue,
        );
        ref.invalidate(projectDetailProvider(widget.existing!.id));
      } else {
        await repository.create(
          name: _nameController.text.trim(),
          description: _descriptionController.text.trim().isEmpty
              ? null
              : _descriptionController.text.trim(),
          customerId: _customerId,
          parentProjectId: _parentProjectId,
          address: _addressController.text.trim().isEmpty
              ? null
              : _addressController.text.trim(),
          latitude: latitude,
          longitude: longitude,
          countryCode: _countryCodeController.text.trim().isEmpty
              ? null
              : _countryCodeController.text.trim(),
          shiftStartTime: shiftStartTime,
          startDate: _startDate?.toIso8601String(),
          endDate: _endDate?.toIso8601String(),
          contractValue: contractValue,
        );
      }

      await ref.read(projectsControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(
          _isEditing ? l10n.projectFormSaved : l10n.projectFormAdded,
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

class _TimePickerField extends StatelessWidget {
  const _TimePickerField({
    required this.label,
    required this.value,
    required this.enabled,
    required this.onChanged,
  });

  final String label;
  final TimeOfDay? value;
  final bool enabled;
  final ValueChanged<TimeOfDay?> onChanged;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: enabled
          ? () async {
              final picked = await showTimePicker(
                context: context,
                initialTime: value ?? const TimeOfDay(hour: 7, minute: 0),
              );
              if (picked != null) onChanged(picked);
            }
          : null,
      child: InputDecorator(
        decoration: InputDecoration(labelText: label),
        child: Text(value == null ? '' : value!.format(context)),
      ),
    );
  }
}
