import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/idempotency.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../attachments/data/attachment_repository.dart';
import '../data/refund.dart';
import '../data/refund_repository.dart';

/// Requests to be paid back the caller may see: their own, and for the office everyone's.
///
/// Watches the signed-in user, so the next person to sign in on the phone never sees the
/// previous one's money.
class RefundsController extends AsyncNotifier<List<Refund>> {
  @override
  Future<List<Refund>> build() async {
    ref.watch(currentUserProvider);

    return (await ref.read(refundRepositoryProvider).fetch()).items;
  }

  Future<void> refresh() async {
    state = await AsyncValue.guard(
      () async => (await ref.read(refundRepositoryProvider).fetch()).items,
    );
  }

  /// Sends the request, then the receipt if there is one. The request stands even if the photo
  /// does not go through: the person can add it again from the admin panel or the next attempt.
  Future<void> create({
    required double amount,
    required String currency,
    required DateTime expenseDate,
    required String description,
    String? receiptPath,
    String? receiptName,
  }) async {
    final refund = await ref.read(refundRepositoryProvider).create(
          amount: amount,
          currency: currency,
          expenseDate: expenseDate,
          description: description,
          idempotencyKey: newIdempotencyKey(),
        );

    if (receiptPath != null && receiptName != null) {
      try {
        await ref.read(attachmentRepositoryProvider).uploadPhoto(
              ownerType: 'Refund',
              ownerId: refund.id,
              filePath: receiptPath,
              fileName: receiptName,
            );
      } finally {
        await refresh();
      }
    } else {
      await refresh();
    }
  }

  Future<void> review(Refund refund, String status, {String? note}) async {
    await ref.read(refundRepositoryProvider).review(refund.id, status, note: note);
    await refresh();
  }
}

final refundsControllerProvider =
    AsyncNotifierProvider<RefundsController, List<Refund>>(RefundsController.new);
