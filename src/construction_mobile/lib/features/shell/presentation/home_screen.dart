import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_theme.dart';
import '../../../core/utils/formatting.dart';
import '../../auth/data/models/user.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../location/presentation/location_status_card.dart';
import '../../time_entries/presentation/shift_controller.dart';
import '../../time_entries/presentation/shift_screen.dart';
import '../../work_items/data/models/work_item.dart';
import '../../work_items/presentation/my_work_controller.dart';
import '../../work_items/presentation/report_defect.dart';
import 'today_data.dart';

/// "Today": what this person is doing right now, and the one thing they may
/// need to do about it. Everything else lives behind the avatar
/// (see `MoreScreen`).
///
/// A worker sees their shift and their tasks. A foreman or anyone above sees
/// how the crew stands and what wants a look. Both get the same way to report
/// a defect, because it is the one thing every site visit may turn up.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider);

    if (user == null) {
      // The router redirects to sign-in; this only shows for the one frame
      // between signing out and the redirect taking effect.
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    final oversees = user.canViewDirectory;

    return Scaffold(
      body: Stack(
        children: [
          RefreshIndicator(
            edgeOffset: MediaQuery.paddingOf(context).top,
            onRefresh: () => _refresh(context, ref),
            child: ListView(
              padding: EdgeInsets.zero,
              children: [
                Stack(
                  // The picture runs a little past the card; clipping it there
                  // cut the end of its fade into a visible line.
                  clipBehavior: Clip.none,
                  children: [
                    const Positioned(
                      top: 0,
                      left: 0,
                      right: 0,
                      child: _SiteArtwork(),
                    ),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(
                            height: MediaQuery.paddingOf(context).top + 12,
                          ),
                          _GreetingRow(user: user),
                          if (oversees && user.isEmployee) const _ShiftBadge(),
                          SizedBox(
                            height: user.isEmployee && !oversees ? 96 : 84,
                          ),
                          if (user.isEmployee && !oversees)
                            const ShiftCard(hero: true)
                          else if (oversees)
                            const _CrewCard(),
                        ],
                      ),
                    ),
                  ],
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 24, 16, 32),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (user.isEmployee && !oversees) const _TasksSection(),
                      if (oversees) const _AttentionSection(),
                      if (user.isEmployee) ...[
                        const SizedBox(height: 20),
                        const ReportDefectButton(prominent: true),
                        const SizedBox(height: 12),
                        const LocationStatusCard(),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
          // Keeps the clock and battery readable when the page has scrolled
          // up underneath them.
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            height: MediaQuery.paddingOf(context).top + 12,
            child: IgnorePointer(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [
                      Theme.of(
                        context,
                      ).scaffoldBackgroundColor.withValues(alpha: 1),
                      Theme.of(
                        context,
                      ).scaffoldBackgroundColor.withValues(alpha: 0),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _refresh(BuildContext context, WidgetRef ref) async {
    final messenger = ScaffoldMessenger.of(context);
    // Read before the await, with the messenger and for the same reason: the
    // context may be gone by the time the request fails.
    final l10n = context.l10n;

    ref.invalidate(shiftControllerProvider);
    ref.invalidate(myWorkControllerProvider);
    ref.invalidate(crewTodayProvider);

    try {
      await ref.read(authControllerProvider.notifier).refreshProfile();
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }
}

/// The dawn-over-the-site picture behind the greeting, dissolving into the
/// page. One picture for everyone: it is atmosphere, not information.
class _SiteArtwork extends StatelessWidget {
  const _SiteArtwork();

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final top = MediaQuery.paddingOf(context).top;

    final page = Theme.of(context).scaffoldBackgroundColor;

    return ExcludeSemantics(
      child: SizedBox(
        height: top + 330,
        width: double.infinity,
        child: Stack(
          fit: StackFit.expand,
          children: [
            Opacity(
              opacity: dark ? 0.4 : 1,
              child: Image.asset(
                'assets/images/site_dawn.jpg',
                fit: BoxFit.cover,
                alignment: Alignment.topCenter,
              ),
            ),
            // The page colour laid over the picture, thickening to solid at
            // the bottom edge, so it ends exactly on the page in either theme.
            DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [page.withValues(alpha: 0), page],
                  stops: const [0.45, 1],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _GreetingRow extends StatelessWidget {
  const _GreetingRow({required this.user});

  final User user;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final name = (user.firstName?.trim().isNotEmpty ?? false)
        ? user.firstName!.trim()
        // An account with no name behind it: the part of the e-mail before the @ greets better than all of it.
        : user.displayName.split('@').first;
    final hour = DateTime.now().hour;

    final greeting = hour < 11
        ? l10n.homeGreetingMorning(name)
        : hour < 18
        ? l10n.homeGreetingDay(name)
        : l10n.homeGreetingEvening(name);

    return Row(
      children: [
        Icon(Icons.engineering_outlined, color: theme.colorScheme.onSurface),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            greeting,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w800,
              height: 1.15,
            ),
          ),
        ),
        const SizedBox(width: 12),
        Tooltip(
          message: l10n.moreTitle,
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: () => context.push(AppRoutes.more),
            child: CircleAvatar(
              radius: 24,
              backgroundColor: AppTheme.heroCard,
              child: Text(
                initialsOf(user.firstName, user.lastName, fallback: user.email),
                style: theme.textTheme.titleMedium?.copyWith(
                  color: AppTheme.heroCardText,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

/// "On shift since 06:45", for a foreman whose main card is the crew's.
class _ShiftBadge extends ConsumerWidget {
  const _ShiftBadge();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(shiftControllerProvider).value;
    final since = state?.startedAt;

    if (state == null || !state.isRunning || since == null) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.only(top: 6, left: 36),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: const BoxDecoration(
              color: Color(0xFF3F9B4F),
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 8),
          Text(
            '${context.l10n.homeOnShift} · ${formatTime(since)}',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ],
      ),
    );
  }
}

class _TasksSection extends ConsumerWidget {
  const _TasksSection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final items =
        ref.watch(myWorkControllerProvider).value?.items ?? const <WorkItem>[];
    final shown = items.take(3).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                l10n.homeTasksToday,
                style: theme.textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            TextButton(
              onPressed: () => context.push(AppRoutes.workItems),
              child: Text(l10n.homeAllTasks),
            ),
          ],
        ),
        if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Text(
              l10n.homeNoTasks,
              style: theme.textTheme.bodyLarge?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          )
        else
          for (final item in shown) _TaskRow(item: item),
      ],
    );
  }
}

class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.item});

  final WorkItem item;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final high = item.priority == 'High' || item.priority == 'Urgent';

    final detail = [
      if (high) enumLabel(l10n, EnumKind.workItemPriority, item.priority),
      if (item.due != null) l10n.workItemsDue(formatDate(item.due)),
    ];

    return InkWell(
      onTap: () => context.push(AppRoutes.workItems),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.6),
            ),
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(
                item.kind == 'Defect'
                    ? Icons.report_problem_outlined
                    : Icons.construction_outlined,
                color: AppTheme.heroCard,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  if (detail.isNotEmpty)
                    Text(
                      detail.join(' · '),
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: high
                            ? AppTheme.accent
                            : theme.colorScheme.onSurfaceVariant,
                        fontWeight: high ? FontWeight.w600 : null,
                      ),
                    ),
                ],
              ),
            ),
            Icon(
              Icons.chevron_right,
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ],
        ),
      ),
    );
  }
}

/// How the crew stands, in one dark card.
class _CrewCard extends ConsumerWidget {
  const _CrewCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final crew = ref.watch(crewTodayProvider);
    const ink = AppTheme.heroCardText;
    final muted = ink.withValues(alpha: 0.72);

    final data = crew.value;

    final title = data == null || data.siteNames.isEmpty
        ? l10n.teamTodayAction
        : data.siteNames.length == 1
        ? data.siteNames.single
        : l10n.homeSitesCount(data.siteNames.length);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppTheme.heroCard,
        borderRadius: BorderRadius.circular(24),
        boxShadow: const [
          BoxShadow(
            color: Color(0x33000000),
            blurRadius: 18,
            offset: Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.engineering_outlined, color: ink),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: ink,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (data == null)
            Text(
              crew.error is ApiException
                  ? (crew.error as ApiException).describe(l10n)
                  : '…',
              style: theme.textTheme.bodyLarge?.copyWith(color: muted),
            )
          else ...[
            Text(
              l10n.homeCrewCount(data.onSiteCount, data.crewCount),
              style: theme.textTheme.displaySmall?.copyWith(
                color: ink,
                fontWeight: FontWeight.w800,
                height: 1.05,
              ),
            ),
            Text(
              l10n.homeCrewOnSite,
              style: theme.textTheme.titleMedium?.copyWith(color: muted),
            ),
            const SizedBox(height: 14),
            _Segments(filled: data.onSiteCount, total: data.crewCount),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: Text(
                    l10n.homeNotClockedIn(data.notClockedIn.length),
                    style: theme.textTheme.bodyLarge?.copyWith(
                      color: data.notClockedIn.isEmpty
                          ? ink
                          : const Color(0xFFFF9B62),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                Text(
                  l10n.homeOutsideSite(data.outsideSiteCount),
                  style: theme.textTheme.bodyLarge?.copyWith(color: ink),
                ),
              ],
            ),
          ],
          const SizedBox(height: 18),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              style: FilledButton.styleFrom(
                backgroundColor: AppTheme.heroCardText,
                foregroundColor: AppTheme.heroCard,
              ),
              onPressed: () => context.push(AppRoutes.teamToday),
              child: Text(l10n.teamTodayAction),
            ),
          ),
        ],
      ),
    );
  }
}

/// One rounded segment per person up to twelve; past that, one bar.
class _Segments extends StatelessWidget {
  const _Segments({required this.filled, required this.total});

  final int filled;
  final int total;

  @override
  Widget build(BuildContext context) {
    const ink = AppTheme.heroCardText;

    if (total <= 0) {
      return const SizedBox(height: 10);
    }

    if (total > 12) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(6),
        child: LinearProgressIndicator(
          minHeight: 10,
          value: (filled / total).clamp(0, 1).toDouble(),
          color: ink,
          backgroundColor: ink.withValues(alpha: 0.2),
        ),
      );
    }

    return Row(
      children: [
        for (var i = 0; i < total; i++) ...[
          if (i > 0) const SizedBox(width: 6),
          Expanded(
            child: Container(
              height: 10,
              decoration: BoxDecoration(
                color: i < filled ? ink : Colors.transparent,
                border: Border.all(color: ink.withValues(alpha: 0.8)),
                borderRadius: BorderRadius.circular(6),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

class _AttentionSection extends ConsumerWidget {
  const _AttentionSection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final items =
        ref.watch(crewTodayProvider).value?.attention ??
        const <AttentionItem>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          l10n.homeNeedsAttention,
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 6),
        if (items.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Text(
              l10n.homeAllClear,
              style: theme.textTheme.bodyLarge?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          )
        else
          for (final item in items) _AttentionRow(item: item),
      ],
    );
  }
}

class _AttentionRow extends StatelessWidget {
  const _AttentionRow({required this.item});

  final AttentionItem item;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final missing = item.kind == AttentionKind.notClockedIn;

    return InkWell(
      onTap: () {
        if (missing) {
          context.push(AppRoutes.teamToday);
        } else if (item.projectId != null) {
          context.push('${AppRoutes.projects}/${item.projectId}');
        }
      },
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.6),
            ),
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: missing ? AppTheme.accent : AppTheme.heroCard,
                shape: BoxShape.circle,
              ),
              child: Icon(
                missing ? Icons.person_outline : Icons.report_problem_outlined,
                color: AppTheme.heroCardText,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  Text(
                    missing
                        ? l10n.homeNoShiftYet
                        : l10n.homeDefectReported(
                            formatRelative(item.reportedAt, l10n),
                          ),
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            Icon(
              Icons.chevron_right,
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ],
        ),
      ),
    );
  }
}
