import 'package:flutter/material.dart';

import '../../../core/l10n/app_locales.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/notifications_controller.dart';

/// Bottom-navigation frame around the signed-in sections.
///
/// The directory tabs are only offered to roles the API actually serves them
/// to, so a Worker is never shown a tab that would answer 403.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  /// Branch indices in the order declared by the router.
  static const int _homeBranch = 0;
  static const int _employeesBranch = 1;
  static const int _projectsBranch = 2;
  static const int _notificationsBranch = 3;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canViewDirectory =
        ref.watch(currentUserProvider)?.canViewDirectory ?? false;
    final unread = ref.watch(unreadNotificationCountProvider).value ?? 0;

    final branches = <int>[
      _homeBranch,
      if (canViewDirectory) _employeesBranch,
      if (canViewDirectory) _projectsBranch,
      _notificationsBranch,
    ];

    final selected = branches.indexOf(navigationShell.currentIndex);

    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: selected < 0 ? 0 : selected,
        onDestinationSelected: (index) {
          final branch = branches[index];

          navigationShell.goBranch(
            branch,
            // Tapping the active tab returns to the top of that section.
            initialLocation: branch == navigationShell.currentIndex,
          );
        },
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home),
            label: 'Home',
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
    );
  }
}
