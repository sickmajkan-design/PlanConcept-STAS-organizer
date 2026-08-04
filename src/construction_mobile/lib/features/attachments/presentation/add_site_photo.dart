import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/attachment_repository.dart';

/// Adds a photograph to a project from the camera or the gallery.
///
/// The one upload the phone offers. Site photographs are the thing the person
/// standing on the site is best placed to capture, and the API allows a Worker
/// to add exactly this and nothing else.
class AddSitePhotoButton extends ConsumerStatefulWidget {
  const AddSitePhotoButton({super.key, required this.projectId});

  final String projectId;

  @override
  ConsumerState<AddSitePhotoButton> createState() => _AddSitePhotoButtonState();
}

class _AddSitePhotoButtonState extends ConsumerState<AddSitePhotoButton> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return TextButton.icon(
      onPressed: _busy ? null : _pick,
      icon: const Icon(Icons.add_a_photo_outlined),
      label: Text(_busy ? l10n.attachmentsUploading : l10n.attachmentsAddPhoto),
    );
  }

  Future<void> _pick() async {
    final l10n = context.l10n;

    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: Text(l10n.attachmentsTakePhoto),
              onTap: () => Navigator.of(sheetContext).pop(ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: Text(l10n.attachmentsFromGallery),
              onTap: () => Navigator.of(sheetContext).pop(ImageSource.gallery),
            ),
          ],
        ),
      ),
    );

    if (source == null || !mounted) {
      return;
    }

    // Re-encoded on the way out: a modern phone camera produces 8–12 MB per
    // frame, which is both over the API's limit and far more detail than a
    // record of "this is what the wall looked like" needs.
    final picked = await ImagePicker().pickImage(
      source: source,
      maxWidth: 2048,
      maxHeight: 2048,
      imageQuality: 85,
    );

    if (picked == null || !mounted) {
      return;
    }

    await _upload(picked);
  }

  Future<void> _upload(XFile picked) async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    final length = await File(picked.path).length();

    if (length > AttachmentRepository.maxSizeBytes) {
      messenger.showSnackBar(SnackBar(
        content: Text(l10n.attachmentsTooLarge(
          AttachmentRepository.maxSizeBytes ~/ (1024 * 1024),
        )),
      ));
      return;
    }

    setState(() => _busy = true);

    try {
      await ref.read(attachmentRepositoryProvider).uploadPhoto(
            ownerType: 'Project',
            ownerId: widget.projectId,
            filePath: picked.path,
            fileName: picked.name,
          );

      ref.invalidate(attachmentsProvider(
        (ownerType: 'Project', ownerId: widget.projectId),
      ));

      messenger.showSnackBar(
        SnackBar(content: Text(l10n.attachmentsUploaded)),
      );
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.message)));
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
