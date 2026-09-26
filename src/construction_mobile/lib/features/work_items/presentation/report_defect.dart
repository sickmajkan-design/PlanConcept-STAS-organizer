import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:image_picker/image_picker.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/outbox/outbox_queue.dart';
import '../../../core/theme/app_theme.dart';
import '../../outbox/outbox_controller.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../../time_entries/data/models/clock_in_site.dart';
import '../../time_entries/data/time_entry_repository.dart';
import '../../time_entries/presentation/shift_screen.dart';
import 'my_work_controller.dart';

/// Reports a defect against a site, from the site.
///
/// The one kind of work a Worker may raise, and the reason: the person
/// standing in front of a crack is best placed to record it, while handing
/// out tasks is a supervisor's job.
class ReportDefectButton extends ConsumerStatefulWidget {
  const ReportDefectButton({
    super.key,
    this.projectId,
    this.asTile = false,
    this.prominent = false,
  }) : assert(
          projectId != null || asTile || prominent,
          'Name the site, or let the person choose',
        );

  /// The site the defect is on. Null means "one of the sites I am posted to
  /// today", asked for when the button is pressed: a worker has no project
  /// screen to press it on, so the entry point is the home screen instead.
  final String? projectId;

  /// A row for a menu rather than a text button.
  final bool asTile;

  /// A full-width outlined button, for where this is the main field action.
  /// Chooses the site like [asTile] when none is named.
  final bool prominent;

  @override
  ConsumerState<ReportDefectButton> createState() => _ReportDefectButtonState();
}

class _ReportDefectButtonState extends ConsumerState<ReportDefectButton> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    if (widget.asTile) {
      return ListTile(
        leading: const Icon(Icons.report_problem_outlined),
        title: Text(context.l10n.workItemsReportDefect),
        trailing: const Icon(Icons.chevron_right),
        enabled: !_busy,
        onTap: _open,
      );
    }

    if (widget.prominent) {
      return SizedBox(
        width: double.infinity,
        child: OutlinedButton.icon(
          onPressed: _busy ? null : _open,
          icon: const Icon(Icons.photo_camera_outlined),
          label: Text(context.l10n.workItemsReportDefect),
        ),
      );
    }

    return TextButton.icon(
      onPressed: _busy ? null : _open,
      icon: const Icon(Icons.report_problem_outlined),
      label: Text(context.l10n.workItemsReportDefect),
    );
  }

  /// The named site, or the one the person picks from today's postings. Null
  /// when there is nothing to report against or they backed out.
  Future<String?> _site() async {
    final named = widget.projectId;

    if (named != null) {
      return named;
    }

    List<ClockInSite> sites;

    try {
      sites = await ref.read(timeEntryRepositoryProvider).fetchClockInSites();
    } on ApiException catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(exception.describe(context.l10n))),
        );
      }
      return null;
    }

    if (!mounted) {
      return null;
    }

    if (sites.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(context.l10n.workItemsNoSiteToday)),
      );
      return null;
    }

    if (sites.length == 1) {
      return sites.single.id;
    }

    return showModalBottomSheet<String>(
      context: context,
      isDismissible: false,
      enableDrag: false,
      builder: (_) => SitePickerSheet(sites: sites),
    );
  }

  Future<void> _open() async {
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final projectId = await _site();

    if (projectId == null || !mounted) {
      return;
    }

    final result =
        await showModalBottomSheet<({String title, String? description, XFile? photo})>(
      context: context,
      isScrollControlled: true,
      // Not dismissible by a tap outside or a swipe, for the same reason as
      // the clock-out sheet: a dismissal resolves to the same null as pressing
      // Cancel, so the screen cannot tell "I changed my mind" from "my thumb
      // caught the edge". Here the cost is a typed description and a
      // photograph taken in front of the defect, silently discarded — and the
      // reporter is standing on scaffolding, not about to type it twice. The
      // way out is Cancel, which says what it does.
      isDismissible: false,
      enableDrag: false,
      builder: (_) => const _DefectSheet(),
    );

    if (result == null || !mounted) {
      return;
    }

    await _report(projectId, result.title, result.description, result.photo);
  }

  Future<void> _report(
    String projectId,
    String title,
    String? description,
    XFile? photo,
  ) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    setState(() => _busy = true);

    try {
      final position = await _currentPosition();

      // Sent now, or kept on the phone until there is signal: a crack found
      // in a basement is not a reason to lose the report. The photo goes on
      // after the defect exists, because it needs the id; a failure there is
      // reported separately and does not undo the report.
      final result = await ref.read(outboxControllerProvider.notifier).submit(
        OutboxKind.defect,
        <String, dynamic>{
          'projectId': projectId,
          'title': title,
          'description': description,
          'latitude': position?.latitude,
          'longitude': position?.longitude,
          'photoPath': await _keep(photo),
          'photoName': photo?.name,
        },
      );

      // The reporter may also be the assignee later; refreshing keeps their
      // own list honest without a second trip to the screen.
      ref.invalidate(myWorkControllerProvider);

      messenger.showSnackBar(SnackBar(
        content: Text(result.queued
            ? l10n.shiftWaitingToSend
            : result.photoFailed
                ? l10n.workItemsDefectPhotoFailed
                : l10n.workItemsDefectSent),
      ));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  /// Copies the photograph somewhere the system will not clear.
  ///
  /// The camera leaves it in the app's cache, which Android empties when the
  /// phone is short of space — exactly when a report has been waiting a day for
  /// signal. Null when there is no photo or it cannot be copied, in which case
  /// the report goes without it rather than not at all.
  Future<String?> _keep(XFile? photo) async {
    if (photo == null) {
      return null;
    }

    try {
      final directory = Directory(
        p.join((await getApplicationSupportDirectory()).path, 'outbox-photos'),
      );

      await directory.create(recursive: true);

      final kept = p.join(
        directory.path,
        '${DateTime.now().microsecondsSinceEpoch}-${p.basename(photo.path)}',
      );

      await File(photo.path).copy(kept);

      return kept;
    } catch (_) {
      return photo.path;
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
  XFile? _photo;

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
          const SizedBox(height: 12),
          // The camera sits with the text rather than behind a second step:
          // for a defect the picture usually is the report, and anything the
          // reporter has to come back for does not get done.
          OutlinedButton.icon(
            onPressed: _pickPhoto,
            icon: Icon(_photo == null
                ? Icons.photo_camera_outlined
                : Icons.check_circle_outline),
            label: Text(_photo == null
                ? l10n.workItemsAddPhoto
                : l10n.workItemsDefectPhotoAdded),
          ),
          if (_photo == null) ...[
            const SizedBox(height: 4),
            Text(
              l10n.workItemsDefectPhotoHint,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
            ),
          ],
          const SizedBox(height: 20),
          Row(
            children: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
              const Spacer(),
              FilledButton(
                // In a Row: the app theme makes a filled button infinitely
                // wide, which in a Row means it is not drawn at all.
                style: AppTheme.inlineFilledButton,
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
      photo: _photo,
    ));
  }

  /// The camera first, the gallery as a fallback.
  ///
  /// A defect is photographed where it is, so the camera is the intent. Some
  /// devices and emulators have none, and falling back beats a dead button.
  Future<void> _pickPhoto() async {
    final picker = ImagePicker();

    try {
      final picked = await picker.pickImage(
        source: ImageSource.camera,
        // Full-resolution phone photos are several megabytes and the API caps
        // an upload at 20; this stays well inside it and still shows a crack.
        maxWidth: 1920,
        imageQuality: 85,
      );

      if (picked != null && mounted) {
        setState(() => _photo = picked);
      }
    } on Exception {
      final picked = await picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1920,
        imageQuality: 85,
      );

      if (picked != null && mounted) {
        setState(() => _photo = picked);
      }
    }
  }
}
