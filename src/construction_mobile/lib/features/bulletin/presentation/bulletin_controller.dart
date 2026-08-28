import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/bulletin_repository.dart';
import '../data/models/bulletin_post.dart';

/// The board's current notices — and the passive "seen" tracking.
///
/// Opening the board is the whole acknowledgment: every notice not yet
/// marked viewed is marked the moment this loads, with no extra tap and
/// nothing blocking while it happens.
class BulletinController extends AsyncNotifier<List<BulletinPost>> {
  @override
  Future<List<BulletinPost>> build() async {
    final repository = ref.read(bulletinRepositoryProvider);
    final posts = await repository.fetchAll();

    for (final post in posts) {
      if (!post.viewed) {
        // Not awaited: a failed mark-viewed call is not worth stalling the
        // board over, and it will simply be retried the next time this loads.
        unawaited(repository.markViewed(post.id));
      }
    }

    return posts;
  }

  Future<void> refresh() async {
    ref.invalidateSelf();
    await future;
  }
}

final bulletinControllerProvider =
    AsyncNotifierProvider<BulletinController, List<BulletinPost>>(
  BulletinController.new,
);
