import 'dart:async';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/sheet_motion.dart';
import '../../../core/utils/iso_week.dart';
import '../../../l10n/app_localizations.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../data/models/weekly_site_report.dart';
import '../data/weekly_report_repository.dart';
import 'weekly_reports_controller.dart';

/// The kinds a foreman may report. Mirrors the API's `WeeklyReportType`.
const _reportTypes = <String>['SignedHours', 'Aufmass', 'Other'];

/// Mirrors the API's `AttachmentRules.AllowedTypesByExtension` for the kinds
/// of proof this feature accepts.
const _allowedExtensions = <String>['pdf', 'jpg', 'jpeg', 'png', 'webp', 'heic'];

/// More hours than a week holds are a typing mistake; the API refuses them too.
const _maxWeekHours = 168.0;

/// More pictures than this in one report are refused by the API.
const _maxPictures = 20;

/// Opens the report form. With [resend] the site and the week of a report the office sent back
/// are filled in, so sending it again is a few taps.
Future<void> showSubmitWeeklyReportSheet(
  BuildContext context,
  WidgetRef ref, {
  WeeklySiteReport? resend,
}) {
  return showModalBottomSheet<void>(
    context: context,
    sheetAnimationStyle: sheetMotion(context),
    isScrollControlled: true,
    builder: (_) => _SubmitWeeklyReportSheet(resend: resend),
  );
}

/// One worker in the report, with the hours typed for the week (and, for a week that runs over a
/// month's end, the hours that fall in the later month).
class _Line {
  _Line({required this.employeeId, required this.name});

  final String employeeId;
  final String name;
  final first = TextEditingController();
  final second = TextEditingController();

  void dispose() {
    first.dispose();
    second.dispose();
  }
}

double _number(String text) =>
    double.tryParse(text.trim().replaceAll(',', '.')) ?? 0;

String _hours(double value) {
  final rounded = (value * 100).round() / 100;
  return rounded == rounded.roundToDouble()
      ? rounded.toStringAsFixed(0)
      : rounded.toString();
}

String _monthLabel(DateTime month) =>
    '${month.month.toString().padLeft(2, '0')}/${month.year}';

class _SubmitWeeklyReportSheet extends ConsumerStatefulWidget {
  const _SubmitWeeklyReportSheet({this.resend});

  final WeeklySiteReport? resend;

  @override
  ConsumerState<_SubmitWeeklyReportSheet> createState() =>
      _SubmitWeeklyReportSheetState();
}

class _SubmitWeeklyReportSheetState
    extends ConsumerState<_SubmitWeeklyReportSheet> {
  final _quantityController = TextEditingController();
  final _noteController = TextEditingController();
  final _lines = <_Line>[];
  final _pictures = <ReportPicture>[];

  String? _projectId;
  late int _isoYear;
  late int _isoWeek;
  String _type = 'SignedHours';
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    final resend = widget.resend;
    final week = IsoWeek.lastCompleted();

    _projectId = resend?.projectId;
    _isoYear = resend?.isoYear ?? week.isoYear;
    _isoWeek = resend?.isoWeek ?? week.isoWeek;
    _type = resend?.type ?? 'SignedHours';
  }

  @override
  void dispose() {
    _quantityController.dispose();
    _noteController.dispose();
    for (final line in _lines) {
      line.dispose();
    }
    super.dispose();
  }

  IsoWeek get _week => IsoWeek(isoYear: _isoYear, isoWeek: _isoWeek);

  bool get _split => _week.months.second != null;

  bool get _linesValid => _lines.every((line) {
        final total = _number(line.first.text) + _number(line.second.text);
        return _number(line.first.text) >= 0 &&
            _number(line.second.text) >= 0 &&
            total <= _maxWeekHours;
      });

  double get _total => _lines.fold(
        0.0,
        (sum, line) => sum + _number(line.first.text) + _number(line.second.text),
      );

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final projectsAsync = ref.watch(reportableProjectsProvider);
    final signed = _type == 'SignedHours';
    final months = _week.months;

    final canSubmit = !_busy &&
        _projectId != null &&
        _pictures.isNotEmpty &&
        _linesValid &&
        (!signed ||
            _lines.isNotEmpty ||
            _quantityController.text.trim().isNotEmpty);

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
                initialValue: projects.any((p) => p.id == _projectId)
                    ? _projectId
                    : null,
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
                      if (parsed != null && parsed >= 2020 && parsed <= 2100) {
                        setState(() => _isoYear = parsed);
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
                        setState(() => _isoWeek = parsed);
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
            if (signed) ...[
              const SizedBox(height: 16),
              _workers(l10n, theme, months),
            ],
            const SizedBox(height: 16),
            _pictureSection(l10n, theme),
            const SizedBox(height: 12),
            TextField(
              controller: _noteController,
              enabled: !_busy,
              maxLines: 3,
              maxLength: 1000,
              decoration: InputDecoration(labelText: l10n.weeklyReportsNote),
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

  /// The workers of the week with their hours; for a week that runs over a month's end each one
  /// has hours for both months.
  Widget _workers(
    AppLocalizations l10n,
    ThemeData theme,
    ({DateTime first, DateTime? second}) months,
  ) {
    final secondMonth = months.second;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        border: Border.all(color: theme.colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.weeklyReportsWorkers,
            style: theme.textTheme.titleSmall
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          if (secondMonth != null) ...[
            const SizedBox(height: 4),
            Text(
              l10n.weeklyReportsSplitHint(
                _monthLabel(months.first),
                _monthLabel(secondMonth),
              ),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
          const SizedBox(height: 8),
          for (final line in _lines)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                children: [
                  Expanded(
                    child: Text(line.name, overflow: TextOverflow.ellipsis),
                  ),
                  const SizedBox(width: 8),
                  SizedBox(
                    width: 84,
                    child: TextField(
                      controller: line.first,
                      enabled: !_busy,
                      textAlign: TextAlign.end,
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      decoration: InputDecoration(
                        labelText: secondMonth != null
                            ? _monthLabel(months.first)
                            : l10n.weeklyReportsHours,
                        isDense: true,
                      ),
                      onChanged: (_) => setState(() {}),
                    ),
                  ),
                  if (secondMonth != null) ...[
                    const SizedBox(width: 8),
                    SizedBox(
                      width: 84,
                      child: TextField(
                        controller: line.second,
                        enabled: !_busy,
                        textAlign: TextAlign.end,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        decoration: InputDecoration(
                          labelText: _monthLabel(secondMonth),
                          isDense: true,
                        ),
                        onChanged: (_) => setState(() {}),
                      ),
                    ),
                  ],
                  IconButton(
                    tooltip: l10n.weeklyReportsRemoveRow,
                    icon: const Icon(Icons.close),
                    onPressed: _busy ? null : () => _removeLine(line),
                  ),
                ],
              ),
            ),
          const SizedBox(height: 4),
          OutlinedButton.icon(
            onPressed: _busy ? null : _addWorker,
            icon: const Icon(Icons.person_add_alt_outlined),
            label: Text(l10n.weeklyReportsAddWorker),
          ),
          if (_lines.isNotEmpty) ...[
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerRight,
              child: Text(
                l10n.weeklyReportsTotalHours(_hours(_total)),
                style: theme.textTheme.titleSmall,
              ),
            ),
          ] else ...[
            const SizedBox(height: 8),
            TextField(
              controller: _quantityController,
              enabled: !_busy,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration:
                  InputDecoration(labelText: l10n.weeklyReportsTotalOnly),
              onChanged: (_) => setState(() {}),
            ),
          ],
        ],
      ),
    );
  }

  /// The pictures of the signed sheet, as many as the sheet has pages.
  Widget _pictureSection(AppLocalizations l10n, ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        border: Border.all(color: theme.colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.weeklyReportsPictures,
            style: theme.textTheme.titleSmall
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 2),
          Text(
            l10n.weeklyReportsPicturesHint,
            style: theme.textTheme.bodySmall
                ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: 8),
          for (var index = 0; index < _pictures.length; index++)
            Row(
              children: [
                Icon(Icons.image_outlined,
                    size: 18, color: theme.colorScheme.onSurfaceVariant),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${index + 1}. ${_pictures[index].name}',
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                IconButton(
                  tooltip: l10n.weeklyReportsRemoveRow,
                  icon: const Icon(Icons.close),
                  onPressed: _busy
                      ? null
                      : () => setState(() => _pictures.removeAt(index)),
                ),
              ],
            ),
          const SizedBox(height: 4),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              OutlinedButton.icon(
                onPressed: _busy || _pictures.length >= _maxPictures
                    ? null
                    : _takePhoto,
                icon: const Icon(Icons.photo_camera_outlined),
                label: Text(l10n.weeklyReportsTakePhoto),
              ),
              OutlinedButton.icon(
                onPressed: _busy || _pictures.length >= _maxPictures
                    ? null
                    : _pickFiles,
                icon: const Icon(Icons.attach_file_outlined),
                label: Text(l10n.weeklyReportsChooseFiles),
              ),
            ],
          ),
        ],
      ),
    );
  }

  void _removeLine(_Line line) {
    setState(() => _lines.remove(line));
    line.dispose();
  }

  Future<void> _addWorker() async {
    final picked = await showModalBottomSheet<ReportWorker>(
      context: context,
      sheetAnimationStyle: sheetMotion(context),
      isScrollControlled: true,
      builder: (_) => _WorkerPickerSheet(
        alreadyListed: {for (final line in _lines) line.employeeId},
      ),
    );

    if (picked == null || !mounted) {
      return;
    }

    setState(
      () => _lines.add(_Line(employeeId: picked.id, name: picked.fullName)),
    );
  }

  Future<void> _takePhoto() async {
    // Re-encoded on the way out: a phone camera frame is several megabytes, far over the upload
    // limit and far more than a legible sheet needs.
    final picked = await ImagePicker().pickImage(
      source: ImageSource.camera,
      maxWidth: 2400,
      maxHeight: 2400,
      imageQuality: 85,
    );

    if (picked == null || !mounted) {
      return;
    }

    setState(
      () => _pictures.add(ReportPicture(path: picked.path, name: picked.name)),
    );
  }

  Future<void> _pickFiles() async {
    final picked = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: _allowedExtensions,
    );

    if (picked.isEmpty || !mounted) {
      return;
    }

    var tooLarge = false;
    final added = <ReportPicture>[];

    for (final file in picked) {
      final path = file.path;

      if (path == null) {
        continue;
      }

      final size = file.lengthSync();

      if (size != null && size > WeeklyReportRepository.maxSizeBytes) {
        tooLarge = true;
        continue;
      }

      added.add(ReportPicture(path: path, name: file.name));
    }

    if (tooLarge) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.l10n.weeklyReportsFileTooLarge)),
      );
    }

    setState(() {
      _pictures.addAll(added.take(_maxPictures - _pictures.length));
    });
  }

  Future<void> _submit() async {
    final projectId = _projectId;

    if (projectId == null || _pictures.isEmpty) {
      return;
    }

    if (blockedByPendingAcknowledgment(context, ref)) return;

    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final note = _noteController.text.trim();
    final signed = _type == 'SignedHours';
    final split = _split;
    final quantity = signed && _lines.isEmpty
        ? double.tryParse(_quantityController.text.trim().replaceAll(',', '.'))
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
            pictures: List.of(_pictures),
            lines: signed
                ? [
                    for (final line in _lines)
                      ReportLine(
                        employeeId: line.employeeId,
                        hours:
                            _number(line.first.text) + _number(line.second.text),
                        hoursSecondMonth: split ? _number(line.second.text) : 0,
                      ),
                  ]
                : const [],
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

/// Picks a worker by name: anyone, not only those posted to the site, because a foreman can have
/// somebody with him who was never entered in the planner.
class _WorkerPickerSheet extends ConsumerStatefulWidget {
  const _WorkerPickerSheet({required this.alreadyListed});

  final Set<String> alreadyListed;

  @override
  ConsumerState<_WorkerPickerSheet> createState() => _WorkerPickerSheetState();
}

class _WorkerPickerSheetState extends ConsumerState<_WorkerPickerSheet> {
  Timer? _debounce;
  List<ReportWorker> _found = const [];
  bool _loading = true;
  String _query = '';

  @override
  void initState() {
    super.initState();
    unawaited(_search());
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  void _typed(String value) {
    _query = value.trim();
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), _search);
  }

  Future<void> _search() async {
    final query = _query;
    setState(() => _loading = true);

    try {
      // A short search is answered with the crew of the foreman's own sites; from two letters on the
      // search reaches everybody who works for the firm.
      final people = await ref
          .read(weeklyReportRepositoryProvider)
          .fetchReportWorkers(search: query.isEmpty ? null : query);

      if (!mounted || query != _query) {
        return;
      }

      setState(() {
        _found = people
            .where((e) => !widget.alreadyListed.contains(e.id))
            .toList();
        _loading = false;
      });
    } on ApiException {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: SizedBox(
        height: MediaQuery.sizeOf(context).height * 0.6,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.weeklyReportsAddWorker,
              style: Theme.of(context)
                  .textTheme
                  .titleLarge
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 12),
            TextField(
              autofocus: true,
              onChanged: _typed,
              decoration: InputDecoration(
                labelText: l10n.weeklyReportsSearchWorker,
                prefixIcon: const Icon(Icons.search),
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : _found.isEmpty
                      ? Center(child: Text(l10n.weeklyReportsNoWorkerFound))
                      : ListView.builder(
                          itemCount: _found.length,
                          itemBuilder: (context, index) {
                            final employee = _found[index];

                            return ListTile(
                              title: Text(employee.fullName),
                              subtitle: employee.position == null
                                  ? null
                                  : Text(employee.position!),
                              onTap: () => Navigator.of(context).pop(employee),
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }
}
