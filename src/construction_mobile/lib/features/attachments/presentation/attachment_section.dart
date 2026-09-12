import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/confirm_dialog.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/attachment_repository.dart';
import '../data/models/attachment.dart';
import 'attachment_form_sheet.dart';
import 'attachment_preview_screen.dart';

/// The documents on one record, shown inside a detail screen.
///
/// Upload is Foreman and above, delete Admin and above — mirrors the API's
/// `AttachmentRules.CanUpload`/`CanDelete` exactly, which is wider than this
/// screen used to assume.
class AttachmentSection extends ConsumerWidget {
  const AttachmentSection({
    super.key,
    required this.ownerType,
    required this.ownerId,
  });

  final String ownerType;
  final String ownerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final user = ref.watch(currentUserProvider);
    final canUpload = user?.canViewDirectory ?? false;
    final canDelete = user?.isAdminAndAbove ?? false;

    final attachments = ref.watch(
      attachmentsProvider((ownerType: ownerType, ownerId: ownerId)),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (canUpload)
          Align(
            alignment: Alignment.centerRight,
            child: TextButton.icon(
              onPressed: () => showAttachmentFormSheet(
                context,
                ref,
                ownerType: ownerType,
                ownerId: ownerId,
              ),
              icon: const Icon(Icons.upload_file_outlined, size: 18),
              label: Text(l10n.attachmentsAddDocument),
            ),
          ),
        attachments.when(
          // A record with no documents is the common case, and a spinner or
          // an error banner for it would be noise on an otherwise complete
          // screen.
          loading: () => const SizedBox.shrink(),
          error: (_, _) => const SizedBox.shrink(),
          data: (items) {
            if (items.isEmpty) {
              return Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  l10n.attachmentsEmpty,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              );
            }

            return Column(
              children: [
                for (final attachment in items)
                  _AttachmentTile(
                    attachment: attachment,
                    canDelete: canDelete,
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}

class _AttachmentTile extends ConsumerWidget {
  const _AttachmentTile({required this.attachment, required this.canDelete});

  final Attachment attachment;
  final bool canDelete;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final expired = attachment.isExpiredOn(DateTime.now());
    final retained = attachment.isRetainedOn(DateTime.now());

    return ListTile(
      leading: Icon(
        attachment.isImage ? Icons.image_outlined : Icons.description_outlined,
      ),
      title: Text(attachment.fileName),
      subtitle: Text(
        [
          enumLabel(l10n, EnumKind.attachmentCategory, attachment.category),
          if (attachment.expiryDate != null)
            expired
                ? l10n.attachmentsExpired
                : l10n.attachmentsExpiresOn(formatDate(attachment.expiryDate)),
        ].join(' · '),
        style: theme.textTheme.bodySmall?.copyWith(
          color: expired
              ? theme.colorScheme.error
              : theme.colorScheme.onSurfaceVariant,
        ),
      ),
      trailing: canDelete
          ? Tooltip(
              message: retained
                  ? l10n.attachmentsRetainedCannotDelete(
                      formatDate(attachment.retainUntilDate))
                  : l10n.commonDelete,
              child: IconButton(
                icon: const Icon(Icons.delete_outline),
                onPressed:
                    retained ? null : () => _delete(context, ref, attachment),
              ),
            )
          : const Icon(Icons.chevron_right),
      onTap: () => Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => AttachmentPreviewScreen(attachment: attachment),
        ),
      ),
    );
  }

  Future<void> _delete(
    BuildContext context,
    WidgetRef ref,
    Attachment attachment,
  ) async {
    final l10n = context.l10n;

    final confirmed = await showConfirmDialog(
      context,
      title: l10n.attachmentsDeleteTitle,
      body: l10n.attachmentsDeleteBody(attachment.fileName),
      destructive: true,
    );

    if (!confirmed || !context.mounted) return;

    final messenger = ScaffoldMessenger.of(context);

    try {
      await ref.read(attachmentRepositoryProvider).remove(attachment.id);
      ref.invalidate(attachmentsProvider(
        (ownerType: attachment.ownerType, ownerId: attachment.ownerId),
      ));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}
