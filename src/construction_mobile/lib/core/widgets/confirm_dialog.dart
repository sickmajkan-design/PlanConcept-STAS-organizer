import 'package:flutter/material.dart';

import '../l10n/app_locales.dart';

/// Asks before something irreversible happens. The only confirm-dialog
/// mechanism this app has ever needed — extracted from what
/// `HomeScreen._confirmSignOut` already hand-wrote, so sign-out and every new
/// delete action share one implementation instead of five near-identical ones.
Future<bool> showConfirmDialog(
  BuildContext context, {
  required String title,
  required String body,
  String? confirmLabel,
  bool destructive = false,
}) async {
  final theme = Theme.of(context);

  final confirmed = await showDialog<bool>(
    context: context,
    builder: (dialogContext) => AlertDialog(
      title: Text(title),
      content: Text(body),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(dialogContext).pop(false),
          child: Text(context.l10n.commonCancel),
        ),
        FilledButton(
          style: destructive
              ? FilledButton.styleFrom(
                  backgroundColor: theme.colorScheme.error,
                  foregroundColor: theme.colorScheme.onError,
                )
              : null,
          onPressed: () => Navigator.of(dialogContext).pop(true),
          child: Text(confirmLabel ?? context.l10n.commonDelete),
        ),
      ],
    ),
  );

  return confirmed ?? false;
}
