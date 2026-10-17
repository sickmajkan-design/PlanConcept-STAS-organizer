import 'dart:async';
import 'dart:ui' show ImageFilter;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../core/l10n/app_locales.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/acknowledgment_banner.dart';
import '../../notifications/presentation/notifications_controller.dart';
import '../../reminders/presentation/shift_reminder_controller.dart';

/// Bottom-navigation frame around the signed-in sections.
///
/// The directory tabs are only offered to roles the API actually serves them
/// to, so a Worker is never shown a tab that would answer 403. Time Entries
/// is offered to every employee-linked account, Worker included — it is the
/// one screen almost everyone opens at least twice a day, which is exactly
/// why it sits in the bar itself rather than one tap into a card on Home.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  /// Branch indices in the order declared by the router.
  static const int _homeBranch = 0;
  static const int _timeEntriesBranch = 1;
  static const int _employeesBranch = 2;
  static const int _projectsBranch = 3;
  static const int _notificationsBranch = 4;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider);
    final canViewDirectory = user?.canViewDirectory ?? false;
    final isEmployee = user?.isEmployee ?? false;
    final unread = ref.watch(unreadNotificationCountProvider).value ?? 0;

    // Keeps the clock-in / clock-out reminders on this phone in step with the shift.
    ref.watch(shiftReminderSyncProvider);

    final branches = <int>[
      _homeBranch,
      if (isEmployee) _timeEntriesBranch,
      if (canViewDirectory) _employeesBranch,
      if (canViewDirectory) _projectsBranch,
      _notificationsBranch,
    ];

    final selected = branches.indexOf(navigationShell.currentIndex);

    void select(int index) {
      unawaited(HapticFeedback.selectionClick());
      final branch = branches[index];

      navigationShell.goBranch(
        branch,
        // Tapping the active tab returns to the top of that section.
        initialLocation: branch == navigationShell.currentIndex,
      );
    }

    return Scaffold(
      // The bar floats over the content, which scrolls under it; the screens leave room for it themselves
      // (their lists read the bottom padding this makes), so the last row can always be reached.
      extendBody: true,
      // Above the branches rather than inside each screen: the notice is about
      // the phone, not about the page, and a screen that forgot to include it
      // would be the one showing yesterday's numbers without saying so.
      body: Column(
        children: [
          const AcknowledgmentBanner(),
          Expanded(child: navigationShell),
        ],
      ),
      bottomNavigationBar: _FloatingBar(
        child: Row(
          children: [
            _BarItem(
              icon: Icons.home_outlined,
              selectedIcon: Icons.home,
              label: context.l10n.navHome,
              selected: selected == 0,
              onTap: () => select(0),
            ),
            if (isEmployee)
              _BarItem(
                icon: Icons.schedule_outlined,
                selectedIcon: Icons.schedule,
                label: context.l10n.navTimeEntries,
                selected: selected == branches.indexOf(_timeEntriesBranch),
                onTap: () => select(branches.indexOf(_timeEntriesBranch)),
              ),
            if (canViewDirectory) ...[
              _BarItem(
                icon: Icons.people_outline,
                selectedIcon: Icons.people,
                label: context.l10n.navEmployees,
                selected: selected == branches.indexOf(_employeesBranch),
                onTap: () => select(branches.indexOf(_employeesBranch)),
              ),
              _BarItem(
                icon: Icons.apartment_outlined,
                selectedIcon: Icons.apartment,
                label: context.l10n.navProjects,
                selected: selected == branches.indexOf(_projectsBranch),
                onTap: () => select(branches.indexOf(_projectsBranch)),
              ),
            ],
            _BarItem(
              icon: Icons.notifications_none,
              selectedIcon: Icons.notifications,
              label: context.l10n.commonAlerts,
              badge: unread,
              selected: selected == branches.indexOf(_notificationsBranch),
              onTap: () => select(branches.indexOf(_notificationsBranch)),
            ),
          ],
        ),
      ),
    );
  }
}

/// One tab of the bar: an icon in a pill when it is the current one, and a label on a single line. The label is
/// set smaller when its width asks for it, never wrapped and never cut, so five tabs fit the narrowest phone.
class _BarItem extends StatelessWidget {
  const _BarItem({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    required this.selected,
    required this.onTap,
    this.badge = 0,
  });

  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final bool selected;
  final VoidCallback onTap;
  final int badge;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final reduceMotion = MediaQuery.disableAnimationsOf(context);

    Widget glyph = Icon(
      selected ? selectedIcon : icon,
      size: 24,
      color: selected ? scheme.onSecondaryContainer : scheme.onSurfaceVariant,
    );

    if (badge > 0) {
      glyph = Badge.count(count: badge, child: glyph);
    }

    return Expanded(
      child: Semantics(
        button: true,
        selected: selected,
        label: label,
        child: ExcludeSemantics(
          child: InkWell(
            borderRadius: BorderRadius.circular(28),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 2, vertical: 6),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  AnimatedContainer(
                    duration: reduceMotion
                        ? Duration.zero
                        : const Duration(milliseconds: 220),
                    curve: Curves.easeOutCubic,
                    width: 56,
                    height: 30,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: selected
                          ? scheme.secondaryContainer
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(15),
                    ),
                    child: glyph,
                  ),
                  const SizedBox(height: 2),
                  SizedBox(
                    width: double.infinity,
                    child: FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(
                        label,
                        maxLines: 1,
                        softWrap: false,
                        style: Theme.of(context).textTheme.labelMedium
                            ?.copyWith(
                              fontSize: 12,
                              color: selected
                                  ? scheme.onSurface
                                  : scheme.onSurfaceVariant,
                              fontWeight: selected
                                  ? FontWeight.w700
                                  : FontWeight.w500,
                            ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// The bar as a rounded, slightly see-through pill that floats above the bottom edge. With high contrast or
/// reduced transparency asked for by the phone it is a plain solid bar, so nothing depends on the blur.
class _FloatingBar extends StatelessWidget {
  const _FloatingBar({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final solid = MediaQuery.highContrastOf(context);
    final radius = BorderRadius.circular(30);

    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(14, 0, 14, 10),
        child: DecoratedBox(
          decoration: BoxDecoration(
            borderRadius: radius,
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.18),
                blurRadius: 24,
                offset: const Offset(0, 8),
              ),
            ],
          ),
          child: ClipRRect(
            borderRadius: radius,
            child: BackdropFilter(
              filter: ImageFilter.blur(
                sigmaX: solid ? 0 : 18,
                sigmaY: solid ? 0 : 18,
              ),
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: scheme.surface.withValues(alpha: solid ? 1 : 0.74),
                  borderRadius: radius,
                  border: Border.all(
                    color: scheme.onSurface.withValues(alpha: 0.12),
                  ),
                ),
                child: SizedBox(height: 64, child: child),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
