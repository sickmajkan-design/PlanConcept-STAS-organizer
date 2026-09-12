import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../data/models/bulletin_post.dart';
import 'bulletin_controller.dart';

class BulletinScreen extends ConsumerWidget {
  const BulletinScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final state = ref.watch(bulletinControllerProvider);
    final controller = ref.read(bulletinControllerProvider.notifier);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.bulletinTitle)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: controller.refresh,
          child: state.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            // Wrapped in a scrollable so RefreshIndicator's pull-to-retry
            // gesture still works even though there is nothing to scroll —
            // matches every other failure state in the app.
            error: (error, _) => ListView(
              children: [
                SizedBox(
                  height: MediaQuery.of(context).size.height * 0.7,
                  child: FailureView(error: error, onRetry: controller.refresh),
                ),
              ],
            ),
            data: (posts) => posts.isEmpty
                ? ListView(
                    children: [
                      SizedBox(
                        height: MediaQuery.of(context).size.height * 0.7,
                        child: EmptyView(
                          message: l10n.bulletinEmpty,
                          icon: Icons.campaign_outlined,
                        ),
                      ),
                    ],
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: posts.length,
                    itemBuilder: (context, index) =>
                        _BulletinCard(post: posts[index]),
                  ),
          ),
        ),
      ),
    );
  }
}

class _BulletinCard extends StatelessWidget {
  const _BulletinCard({required this.post});

  final BulletinPost post;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = context.l10n;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: theme.colorScheme.primary,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 12),
            Text(
              post.title,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(post.body, style: theme.textTheme.bodyMedium),
            const SizedBox(height: 12),
            Row(
              children: [
                CircleAvatar(
                  radius: 14,
                  child: Text(
                    initialsOf(null, null, fallback: post.createdByName),
                    style: const TextStyle(fontSize: 12),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${l10n.bulletinPostedBy(post.createdByName)} · '
                    '${formatDateTime(post.createdAt)}',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
