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
        child: NavigationBar(
          selectedIndex: selected < 0 ? 0 : selected,
          onDestinationSelected: (index) {
            unawaited(HapticFeedback.selectionClick());
            final branch = branches[index];

            navigationShell.goBranch(
              branch,
              // Tapping the active tab returns to the top of that section.
              initialLocation: branch == navigationShell.currentIndex,
            );
          },
          destinations: [
            NavigationDestination(
              icon: const Icon(Icons.home_outlined),
              selectedIcon: const Icon(Icons.home),
              label: context.l10n.navHome,
            ),
            if (isEmployee)
              NavigationDestination(
                icon: const Icon(Icons.schedule_outlined),
                selectedIcon: const Icon(Icons.schedule),
                label: context.l10n.navTimeEntries,
              ),
            if (canViewDirectory) ...[
              NavigationDestination(
                icon: Icon(Icons.people_outline),
                selectedIcon: Icon(Icons.people),
                label: context.l10n.navEmployees,
              ),
              NavigationDestination(
                icon: Icon(Icons.apartment_outlined),
                selectedIcon: Icon(Icons.apartment),
                label: context.l10n.navProjects,
              ),
            ],
            NavigationDestination(
              icon: Badge.count(
                count: unread,
                isLabelVisible: unread > 0,
                child: const Icon(Icons.notifications_none),
              ),
              selectedIcon: Badge.count(
                count: unread,
                isLabelVisible: unread > 0,
                child: const Icon(Icons.notifications),
              ),
              label: context.l10n.commonAlerts,
            ),
          ],
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
                child: NavigationBarTheme(
                  data: NavigationBarThemeData(
                    height: 64,
                    backgroundColor: Colors.transparent,
                    surfaceTintColor: Colors.transparent,
                    elevation: 0,
                  ),
                  child: child,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
