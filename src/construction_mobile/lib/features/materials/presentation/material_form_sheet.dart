import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../projects/data/project_repository.dart';
import '../data/material_repository.dart';
import '../data/models/material.dart';
import 'materials_controller.dart';

final _allProjectsForPickerProvider = FutureProvider.autoDispose((ref) {
  return ref.watch(projectRepositoryProvider).fetchAll();
});

/// One sheet for both create and edit — see `VehicleFormSheet` for the
/// pattern this mirrors.
Future<void> showMaterialFormSheet(
  BuildContext context,
  WidgetRef ref, {
  MaterialItem? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _MaterialFormSheet(existing: existing),
  );
}

class _MaterialFormSheet extends ConsumerStatefulWidget {
  const _MaterialFormSheet({this.existing});

  final MaterialItem? existing;

  @override
  ConsumerState<_MaterialFormSheet> createState() =>
      _MaterialFormSheetState();
}

class _MaterialFormSheetState extends ConsumerState<_MaterialFormSheet> {
  late final _nameController = TextEditingController(text: widget.existing?.name);
  late final _unitController = TextEditingController(text: widget.existing?.unit);
  late final _quantityController = TextEditingController(
    text: widget.existing?.quantity.toString(),
  );
  late final _warehouseController =
      TextEditingController(text: widget.existing?.warehouse);
  late final _unitPriceController = TextEditingController(
    text: widget.existing?.unitPrice?.toString(),
  );

  String? _projectId;

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _projectId = widget.existing?.projectId;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _unitController.dispose();
    _quantityController.dispose();
    _warehouseController.dispose();
    _unitPriceController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final projects = ref.watch(_allProjectsForPickerProvider);

    final quantity = double.tryParse(_quantityController.text.trim());
    final canSubmit = !_busy &&
        _nameController.text.trim().isNotEmpty &&
        _unitController.text.trim().isNotEmpty &&
        quantity != null &&
        quantity >= 0;

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
                  ? l10n.materialFormEditTitle
                  : l10n.materialFormAddTitle,
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
              decoration: InputDecoration(labelText: l10n.materialFormName),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _unitController,
                    enabled: !_busy,
                    decoration: InputDecoration(labelText: l10n.materialFormUnit),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: _quantityController,
                    enabled: !_busy,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    decoration: InputDecoration(labelText: l10n.materialStock),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _warehouseController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.materialWarehouse),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _unitPriceController,
              enabled: !_busy,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: l10n.materialFormUnitPrice),
            ),
            const SizedBox(height: 12),
            projects.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) => DropdownButtonFormField<String?>(
                initialValue: _projectId,
                decoration: InputDecoration(labelText: l10n.commonProject),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(l10n.materialWarehouseOnly),
                  ),
                  for (final project in items)
                    DropdownMenuItem(value: project.id, child: Text(project.name)),
                ],
                onChanged: _busy
                    ? null
                    : (value) => setState(() => _projectId = value),
              ),
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
    final repository = ref.read(materialRepositoryProvider);
    final quantity = double.parse(_quantityController.text.trim());
    final unitPrice = double.tryParse(_unitPriceController.text.trim());

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          name: _nameController.text.trim(),
          unit: _unitController.text.trim(),
          quantity: quantity,
          warehouse: _warehouseController.text.trim().isEmpty
              ? null
              : _warehouseController.text.trim(),
          unitPrice: unitPrice,
          projectId: _projectId,
        );
        ref.invalidate(materialDetailProvider(widget.existing!.id));
      } else {
        await repository.create(
          name: _nameController.text.trim(),
          unit: _unitController.text.trim(),
          quantity: quantity,
          warehouse: _warehouseController.text.trim().isEmpty
              ? null
              : _warehouseController.text.trim(),
          unitPrice: unitPrice,
          projectId: _projectId,
        );
      }

      await ref.read(materialsControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(
          _isEditing ? l10n.materialFormSaved : l10n.materialFormAdded,
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
