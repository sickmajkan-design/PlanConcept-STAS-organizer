import 'package:flutter/material.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/widgets/offline_data_banner.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../notifications/presentation/acknowledgment_banner.dart';
import '../../notifications/presentation/notifications_controller.dart';

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

    final branches = <int>[
      _homeBranch,
      if (isEmployee) _timeEntriesBranch,
      if (canViewDirectory) _employeesBranch,
      if (canViewDirectory) _projectsBranch,
      _notificationsBranch,
    ];

    final selected = branches.indexOf(navigationShell.currentIndex);

    return Scaffold(
      // Above the branches rather than inside each screen: the notice is about
      // the phone, not about the page, and a screen that forgot to include it
      // would be the one showing yesterday's numbers without saying so.
      body: Column(
        children: [
          const OfflineDataBanner(),
          const AcknowledgmentBanner(),
          Expanded(child: navigationShell),
        ],
      ),
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
    );
  }
}
