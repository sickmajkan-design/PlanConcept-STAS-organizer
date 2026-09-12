import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../data/attachment_repository.dart';

/// Which categories are offered for each owner type — mirrors the desktop
/// admin panel's `CATEGORIES_BY_OWNER_TYPE` so the same document types are
/// available from either side.
const _categoriesByOwnerType = <String, List<String>>{
  'Employee': ['Contract', 'Certificate', 'MedicalCheck', 'Licence', 'Other'],
  'Project': ['SiteDocument', 'Photo', 'Licence', 'Insurance', 'Other'],
  'Vehicle': ['Insurance', 'Licence', 'Certificate', 'Photo', 'Other'],
  'Tool': ['Certificate', 'Licence', 'Photo', 'Other'],
};

/// Extensions the API's `AttachmentRules.AllowedTypesByExtension` accepts.
const _allowedExtensions = [
  'pdf', 'jpg', 'jpeg', 'png', 'webp', 'heic',
  'doc', 'docx', 'xls', 'xlsx', 'txt',
];

/// Attaches any of the accepted document types to a record — the general
/// upload the office needs (a contract, an insurance certificate, an
/// inspection report), distinct from [AddSitePhotoButton]'s camera-only flow.
Future<void> showAttachmentFormSheet(
  BuildContext context,
  WidgetRef ref, {
  required String ownerType,
  required String ownerId,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _AttachmentFormSheet(ownerType: ownerType, ownerId: ownerId),
  );
}

class _AttachmentFormSheet extends ConsumerStatefulWidget {
  const _AttachmentFormSheet({required this.ownerType, required this.ownerId});

  final String ownerType;
  final String ownerId;

  @override
  ConsumerState<_AttachmentFormSheet> createState() =>
      _AttachmentFormSheetState();
}

class _AttachmentFormSheetState extends ConsumerState<_AttachmentFormSheet> {
  late final _descriptionController = TextEditingController();

  PlatformFile? _file;
  int? _fileSize;
  late String _category =
      (_categoriesByOwnerType[widget.ownerType] ?? const ['Other']).first;
  DateTime? _expiresAt;
  DateTime? _retainUntil;

  bool _busy = false;
  ApiException? _error;
  String? _localError;

  @override
  void dispose() {
    _descriptionController.dispose();
    super.dispose();
  }

  List<String> get _categories =>
      _categoriesByOwnerType[widget.ownerType] ?? const ['Other'];

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final canSubmit = !_busy &&
        _file != null &&
        _fileSize != null &&
        _fileSize! <= AttachmentRepository.maxSizeBytes;

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
              l10n.attachmentsAddDocument,
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
            if (_localError != null) ...[
              Text(
                _localError!,
                style: TextStyle(color: theme.colorScheme.error),
              ),
              const SizedBox(height: 12),
            ],
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickFile,
              icon: const Icon(Icons.attach_file),
              label: Text(
                _file == null
                    ? l10n.attachmentsPickFile
                    : l10n.attachmentsChangeFile,
              ),
            ),
            if (_file != null) ...[
              const SizedBox(height: 8),
              Text(
                _fileSize == null
                    ? _file!.name
                    : '${_file!.name} · ${formatFileSize(_fileSize!)}',
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ],
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _category,
              decoration: InputDecoration(labelText: l10n.attachmentsCategory),
              items: [
                for (final value in _categories)
                  DropdownMenuItem(
                    value: value,
                    child: Text(enumLabel(l10n, EnumKind.attachmentCategory, value)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) {
                        setState(() {
                          _category = value;
                          if (value == 'Photo') _expiresAt = null;
                        });
                      }
                    },
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _descriptionController,
              enabled: !_busy,
              maxLines: 2,
              decoration: InputDecoration(labelText: l10n.attachmentsDescription),
            ),
            // A photograph does not lapse — matches the API's own rule that
            // ExpiresAt must be null for that category.
            if (_category != 'Photo') ...[
              const SizedBox(height: 12),
              _DatePickerField(
                label: l10n.attachmentsExpiryDate,
                value: _expiresAt,
                enabled: !_busy,
                onChanged: (value) => setState(() => _expiresAt = value),
              ),
            ],
            const SizedBox(height: 12),
            _DatePickerField(
              label: l10n.attachmentsRetainUntil,
              value: _retainUntil,
              enabled: !_busy,
              onChanged: (value) => setState(() => _retainUntil = value),
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
                    : Text(l10n.commonAdd),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickFile() async {
    final picked = await FilePicker.pickFile(
      type: FileType.custom,
      allowedExtensions: _allowedExtensions,
    );

    if (picked == null || !mounted) {
      return;
    }

    final size = picked.lengthSync() ?? await picked.length();

    if (!mounted) {
      return;
    }

    setState(() {
      _file = picked;
      _fileSize = size;
      _localError = size > AttachmentRepository.maxSizeBytes
          ? context.l10n.attachmentsTooLarge(
              AttachmentRepository.maxSizeBytes ~/ (1024 * 1024))
          : null;
    });
  }

  Future<void> _submit() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final file = _file!;

    if (file.path == null) {
      setState(() => _localError = l10n.attachmentsOpenFailed);
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      await ref.read(attachmentRepositoryProvider).upload(
            ownerType: widget.ownerType,
            ownerId: widget.ownerId,
            category: _category,
            filePath: file.path!,
            fileName: file.name,
            description: _descriptionController.text.trim().isEmpty
                ? null
                : _descriptionController.text.trim(),
            expiresAt: _expiresAt,
            retainUntil: _retainUntil,
          );

      ref.invalidate(attachmentsProvider(
        (ownerType: widget.ownerType, ownerId: widget.ownerId),
      ));

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.attachmentsSaved)));
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
