import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../data/models/tool.dart';
import '../data/tool_repository.dart';
import 'tools_controller.dart';

/// One sheet for both create and edit — see `VehicleFormSheet` for the
/// pattern this mirrors exactly. Tool has no ownership-type field exposed on
/// mobile (the API model doesn't surface it here either), so create silently
/// defaults it server-side rather than adding a dropdown nothing else shows.
Future<void> showToolFormSheet(
  BuildContext context,
  WidgetRef ref, {
  Tool? existing,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _ToolFormSheet(existing: existing),
  );
}

class _ToolFormSheet extends ConsumerStatefulWidget {
  const _ToolFormSheet({this.existing});

  final Tool? existing;

  @override
  ConsumerState<_ToolFormSheet> createState() => _ToolFormSheetState();
}

class _ToolFormSheetState extends ConsumerState<_ToolFormSheet> {
  late final _nameController = TextEditingController(text: widget.existing?.name);
  late final _categoryController =
      TextEditingController(text: widget.existing?.category);
  late final _serialController =
      TextEditingController(text: widget.existing?.serialNumber);

  late String _status = widget.existing?.status ?? 'Available';
  late String _ownershipType = widget.existing?.ownershipType ?? 'Owned';

  bool _busy = false;
  ApiException? _error;

  bool get _isEditing => widget.existing != null;

  @override
  void dispose() {
    _nameController.dispose();
    _categoryController.dispose();
    _serialController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final canSubmit = !_busy && _nameController.text.trim().isNotEmpty;

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
              _isEditing ? l10n.toolFormEditTitle : l10n.toolFormAddTitle,
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
              decoration: InputDecoration(labelText: l10n.toolFormName),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _categoryController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.toolFormCategory),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _serialController,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.toolSerialNumber),
            ),
            if (_isEditing) ...[
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: InputDecoration(labelText: l10n.commonStatus),
                items: [
                  for (final value in toolStatusFilters)
                    DropdownMenuItem(
                      value: value,
                      child: Text(enumLabel(l10n, EnumKind.toolStatus, value)),
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
                for (final value in const ['Owned', 'Rented'])
                  DropdownMenuItem(
                    value: value,
                    child:
                        Text(enumLabel(l10n, EnumKind.vehicleOwnershipType, value)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) setState(() => _ownershipType = value);
                    },
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
    final repository = ref.read(toolRepositoryProvider);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEditing) {
        await repository.update(
          widget.existing!.id,
          name: _nameController.text.trim(),
          category: _categoryController.text.trim().isEmpty
              ? null
              : _categoryController.text.trim(),
          serialNumber: _serialController.text.trim().isEmpty
              ? null
              : _serialController.text.trim(),
          status: _status,
          ownershipType: 'Owned',
        );
        ref.invalidate(toolDetailProvider(widget.existing!.id));
      } else {
        await repository.create(
          name: _nameController.text.trim(),
          category: _categoryController.text.trim().isEmpty
              ? null
              : _categoryController.text.trim(),
          serialNumber: _serialController.text.trim().isEmpty
              ? null
              : _serialController.text.trim(),
        );
      }

      await ref.read(toolsControllerProvider.notifier).refresh();

      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(_isEditing ? l10n.toolFormSaved : l10n.toolFormAdded),
      ));
    } on ApiException catch (exception) {
      setState(() {
        _error = exception;
        _busy = false;
      });
    }
  }
}
