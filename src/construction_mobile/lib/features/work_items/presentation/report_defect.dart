import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/work_item_repository.dart';
import 'my_work_controller.dart';

/// Reports a defect against a site, from the site.
///
/// The one kind of work a Worker may raise, and the reason: the person
/// standing in front of a crack is best placed to record it, while handing
/// out tasks is a supervisor's job.
class ReportDefectButton extends ConsumerStatefulWidget {
  const ReportDefectButton({super.key, required this.projectId});

  final String projectId;

  @override
  ConsumerState<ReportDefectButton> createState() => _ReportDefectButtonState();
}

class _ReportDefectButtonState extends ConsumerState<ReportDefectButton> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    return TextButton.icon(
      onPressed: _busy ? null : _open,
      icon: const Icon(Icons.report_problem_outlined),
      label: Text(context.l10n.workItemsReportDefect),
    );
  }

  Future<void> _open() async {
    final result = await showModalBottomSheet<({String title, String? description})>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _DefectSheet(),
    );

    if (result == null || !mounted) {
      return;
    }

    await _report(result.title, result.description);
  }

  Future<void> _report(String title, String? description) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    setState(() => _busy = true);

    try {
      final position = await _currentPosition();

      await ref.read(workItemRepositoryProvider).reportDefect(
            projectId: widget.projectId,
            title: title,
            description: description,
            latitude: position?.latitude,
            longitude: position?.longitude,
          );

      // The reporter may also be the assignee later; refreshing keeps their
      // own list honest without a second trip to the screen.
      ref.invalidate(myWorkControllerProvider);

      messenger.showSnackBar(SnackBar(content: Text(l10n.workItemsDefectSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.message)));
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  /// A position for the report, or null if one cannot be had quickly.
  ///
  /// A site is hundreds of metres across and "crack in the wall" does not
  /// locate itself, so the fix is worth waiting a few seconds for — but never
  /// worth refusing the report over.
  Future<Position?> _currentPosition() async {
    try {
      final permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return null;
      }

      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } catch (_) {
      return null;
    }
  }
}

class _DefectSheet extends StatefulWidget {
  const _DefectSheet();

  @override
  State<_DefectSheet> createState() => _DefectSheetState();
}

class _DefectSheetState extends State<_DefectSheet> {
  final _title = TextEditingController();
  final _description = TextEditingController();
  bool _showTitleError = false;

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return Padding(
      padding: EdgeInsets.fromLTRB(
        24,
        24,
        24,
        24 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.workItemsReportDefect,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 16),
          TextField(
            controller: _title,
            autofocus: true,
            textCapitalization: TextCapitalization.sentences,
            decoration: InputDecoration(
              labelText: l10n.workItemsDefectTitle,
              border: const OutlineInputBorder(),
              errorText: _showTitleError ? l10n.workItemsDefectNeedsTitle : null,
            ),
            onChanged: (_) {
              if (_showTitleError) {
                setState(() => _showTitleError = false);
              }
            },
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _description,
            minLines: 2,
            maxLines: 4,
            textCapitalization: TextCapitalization.sentences,
            decoration: InputDecoration(
              labelText: l10n.workItemsDefectDescription,
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
              const Spacer(),
              FilledButton(
                onPressed: _submit,
                child: Text(l10n.workItemsDefectSend),
              ),
            ],
          ),
        ],
      ),
    );
  }

  void _submit() {
    final title = _title.text.trim();

    // The API refuses an empty title; saying so here saves a round trip and
    // keeps what was typed.
    if (title.isEmpty) {
      setState(() => _showTitleError = true);
      return;
    }

    final description = _description.text.trim();

    Navigator.of(context).pop((
      title: title,
      description: description.isEmpty ? null : description,
    ));
  }
}
