import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import 'pending_acknowledgments_controller.dart';

/// Call at the top of a mutating action's handler. Returns true — and shows
/// why — when there is an unconfirmed required notification, so the caller
/// can bail out before doing anything. The screen underneath stays visible
/// and usable; only the action itself is refused.
bool blockedByPendingAcknowledgment(BuildContext context, WidgetRef ref) {
  if (!ref.read(hasPendingAcknowledgmentProvider)) {
    return false;
  }

  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text(context.l10n.ackGatedMessage)),
  );

  return true;
}
