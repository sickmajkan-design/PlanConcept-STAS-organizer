import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../attachments/data/attachment_repository.dart';
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
    final result =
        await showModalBottomSheet<({String title, String? description, XFile? photo})>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _DefectSheet(),
    );

    if (result == null || !mounted) {
      return;
    }

    await _report(result.title, result.description, result.photo);
  }

  Future<void> _report(String title, String? description, XFile? photo) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    setState(() => _busy = true);

    try {
      final position = await _currentPosition();

      final defect = await ref.read(workItemRepositoryProvider).reportDefect(
            projectId: widget.projectId,
            title: title,
            description: description,
            latitude: position?.latitude,
            longitude: position?.longitude,
          );

      // The photo goes on after the defect exists, because it needs the id.
      // A failure here is reported separately and does not undo the report:
      // a defect on record without its picture is worth far more than no
      // defect at all, and the picture can be added afterwards.
      var photoFailed = false;

      if (photo != null) {
        try {
          await ref.read(attachmentRepositoryProvider).uploadPhoto(
                ownerType: 'WorkItem',
                ownerId: defect.id,
                filePath: photo.path,
                fileName: photo.name,
              );
        } on ApiException {
          photoFailed = true;
        }
      }

      // The reporter may also be the assignee later; refreshing keeps their
      // own list honest without a second trip to the screen.
      ref.invalidate(myWorkControllerProvider);

      messenger.showSnackBar(SnackBar(
        content: Text(photoFailed
            ? l10n.workItemsDefectPhotoFailed
            : l10n.workItemsDefectSent),
      ));
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
