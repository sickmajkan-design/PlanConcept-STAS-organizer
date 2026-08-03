import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../data/attachment_repository.dart';
import '../data/models/attachment.dart';
import 'attachment_preview_screen.dart';

/// The documents on one record, shown inside a detail screen.
///
/// Read-only apart from site photos, which have their own entry point on the
/// project screen: the API refuses everything else from a phone anyway, and
/// offering an upload button that always fails is worse than not offering one.
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

    final attachments = ref.watch(
      attachmentsProvider((ownerType: ownerType, ownerId: ownerId)),
    );

    return attachments.when(
      // A record with no documents is the common case, and a spinner or an
      // error banner for it would be noise on an otherwise complete screen.
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
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            for (final attachment in items)
              _AttachmentTile(attachment: attachment),
          ],
        );
      },
    );
  }
}

class _AttachmentTile extends StatelessWidget {
  const _AttachmentTile({required this.attachment});

  final Attachment attachment;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final expired = attachment.isExpiredOn(DateTime.now());

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
      trailing: const Icon(Icons.chevron_right),
      onTap: () => Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => AttachmentPreviewScreen(attachment: attachment),
        ),
      ),
    );
  }
}
