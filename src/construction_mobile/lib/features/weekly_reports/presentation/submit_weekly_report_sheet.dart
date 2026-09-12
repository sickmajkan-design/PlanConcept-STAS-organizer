import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/iso_week.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../data/weekly_report_repository.dart';
import 'weekly_reports_controller.dart';

/// The kinds a foreman may report. Mirrors the API's `WeeklyReportType`.
const _reportTypes = <String>['SignedHours', 'Aufmass', 'Other'];

/// Mirrors the API's `AttachmentRules.AllowedTypesByExtension` for the kinds
/// of proof this feature accepts.
const _allowedExtensions = <String>['pdf', 'jpg', 'jpeg', 'png', 'webp', 'heic'];

Future<void> showSubmitWeeklyReportSheet(BuildContext context, WidgetRef ref) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _SubmitWeeklyReportSheet(),
  );
}

class _SubmitWeeklyReportSheet extends ConsumerStatefulWidget {
  const _SubmitWeeklyReportSheet();

  @override
  ConsumerState<_SubmitWeeklyReportSheet> createState() =>
      _SubmitWeeklyReportSheetState();
}

class _SubmitWeeklyReportSheetState
    extends ConsumerState<_SubmitWeeklyReportSheet> {
  final _quantityController = TextEditingController();
  final _noteController = TextEditingController();

  String? _projectId;
  late final IsoWeek _week = IsoWeek.lastCompleted();
  late int _isoYear = _week.isoYear;
  late int _isoWeek = _week.isoWeek;
  String _type = 'SignedHours';
  PlatformFile? _file;
  bool _busy = false;

  @override
  void dispose() {
    _quantityController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final projectsAsync = ref.watch(reportableProjectsProvider);

    final canSubmit = !_busy &&
        _projectId != null &&
        _file != null &&
        (_type != 'SignedHours' || _quantityController.text.trim().isNotEmpty);

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
              l10n.weeklyReportsSubmit,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            projectsAsync.when(
              data: (projects) => DropdownButtonFormField<String>(
                initialValue: _projectId,
                decoration: InputDecoration(labelText: l10n.weeklyReportsProject),
                items: [
                  for (final project in projects)
                    DropdownMenuItem(value: project.id, child: Text(project.name)),
                ],
                onChanged: _busy
                    ? null
                    : (value) => setState(() => _projectId = value),
              ),
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 12),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Text(
                l10n.weeklyReportsProjectsFailed,
                style: TextStyle(color: theme.colorScheme.error),
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    initialValue: _isoYear.toString(),
                    decoration:
                        InputDecoration(labelText: l10n.weeklyReportsIsoYear),
                    keyboardType: TextInputType.number,
                    enabled: !_busy,
                    onChanged: (value) {
                      final parsed = int.tryParse(value);
                      if (parsed != null) {
                        _isoYear = parsed;
                      }
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    initialValue: _isoWeek.toString(),
                    decoration:
                        InputDecoration(labelText: l10n.weeklyReportsIsoWeek),
                    keyboardType: TextInputType.number,
                    enabled: !_busy,
                    onChanged: (value) {
                      final parsed = int.tryParse(value);
                      if (parsed != null && parsed >= 1 && parsed <= 53) {
                        _isoWeek = parsed;
                      }
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _type,
              decoration: InputDecoration(labelText: l10n.weeklyReportsType),
              items: [
                for (final type in _reportTypes)
                  DropdownMenuItem(
                    value: type,
                    child: Text(enumLabel(l10n, EnumKind.weeklyReportType, type)),
                  ),
              ],
              onChanged: _busy
                  ? null
                  : (value) {
                      if (value != null) {
                        setState(() => _type = value);
                      }
                    },
            ),
            if (_type == 'SignedHours') ...[
              const SizedBox(height: 12),
              TextField(
                controller: _quantityController,
                enabled: !_busy,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(labelText: l10n.weeklyReportsHours),
                onChanged: (_) => setState(() {}),
              ),
            ],
            const SizedBox(height: 12),
            TextField(
              controller: _noteController,
              enabled: !_busy,
              maxLines: 3,
              maxLength: 1000,
              decoration: InputDecoration(labelText: l10n.weeklyReportsNote),
            ),
            const SizedBox(height: 4),
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickFile,
              icon: Icon(_file == null
                  ? Icons.attach_file_outlined
                  : Icons.check_circle_outline),
              label: Text(_file?.name ?? l10n.weeklyReportsAttachFile),
            ),
            const SizedBox(height: 8),
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
                    : Text(l10n.weeklyReportsSend),
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

    if (picked == null || picked.path == null || !mounted) {
      return;
    }

    final size = picked.lengthSync();

    if (size != null && size > WeeklyReportRepository.maxSizeBytes) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.l10n.weeklyReportsFileTooLarge)),
      );
      return;
    }

    setState(() => _file = picked);
  }

  Future<void> _submit() async {
    final projectId = _projectId;
    final file = _file;

    if (projectId == null || file == null || file.path == null) {
      return;
    }

    if (blockedByPendingAcknowledgment(context, ref)) return;

    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final note = _noteController.text.trim();
    final quantity = _type == 'SignedHours'
        ? double.tryParse(_quantityController.text.trim())
        : null;

    setState(() => _busy = true);

    try {
      await ref.read(weeklyReportRepositoryProvider).submit(
            projectId: projectId,
            isoYear: _isoYear,
            isoWeek: _isoWeek,
            type: _type,
            quantity: quantity,
            note: note.isEmpty ? null : note,
            filePath: file.path!,
            fileName: file.name,
          );

      // So the new submission shows up in the history list right away,
      // rather than only after the next pull-to-refresh.
      ref.invalidate(weeklyReportsControllerProvider);

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.weeklyReportsSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
