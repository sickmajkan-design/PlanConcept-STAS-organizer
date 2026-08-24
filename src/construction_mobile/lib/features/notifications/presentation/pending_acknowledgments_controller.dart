import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../data/models/app_notification.dart';
import '../data/notification_repository.dart';

/// Notifications the signed-in worker must confirm before the app lets them
/// do anything mutating — a hazard notice, a recall. Empty while signed out.
final pendingAcknowledgmentsProvider =
    FutureProvider<List<AppNotification>>((ref) async {
  final user = ref.watch(currentUserProvider);

  if (user == null) {
    return const [];
  }

  return ref.read(notificationRepositoryProvider).fetchPendingAcknowledgments();
});

/// True while any notification is still waiting on a confirmation. Screens
/// gate their mutating actions on this rather than the raw list, since most
/// of them only care whether it is empty.
final hasPendingAcknowledgmentProvider = Provider<bool>((ref) {
  return ref.watch(pendingAcknowledgmentsProvider).maybeWhen(
        data: (items) => items.isNotEmpty,
        orElse: () => false,
      );
});

class AcknowledgeController extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<void> acknowledge(String notificationId) async {
    await ref.read(notificationRepositoryProvider).acknowledge(notificationId);
    ref.invalidate(pendingAcknowledgmentsProvider);
  }
}

final acknowledgeControllerProvider =
    AsyncNotifierProvider<AcknowledgeController, void>(
  AcknowledgeController.new,
);
