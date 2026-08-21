import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../auth/data/models/user.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../location/presentation/location_status_card.dart';
import '../../notifications/presentation/push_controller.dart';

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

    return Scaffold(
      appBar: AppBar(
        title: const Text('Construction Organizer'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: context.l10n.commonSignOut,
            onPressed: () => _confirmSignOut(context, ref),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => _refreshProfile(context, ref),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _ProfileCard(user: user),
            const SizedBox(height: 16),
            const LocationStatusCard(),
            const _PushStatusNotice(),
            // Above the resource list because it is the thing a worker opens
            // the app for twice a day, and it is open to every role.
            if (user.isEmployee) ...[
              const SizedBox(height: 16),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(Icons.schedule_outlined),
                      title: Text(context.l10n.navTimeEntries),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => context.push(AppRoutes.timeEntries),
                    ),
                    const Divider(height: 1, indent: 20, endIndent: 20),
                    ListTile(
                      leading: const Icon(Icons.checklist_outlined),
                      title: Text(context.l10n.navWorkItems),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => context.push(AppRoutes.workItems),
                    ),
                    const Divider(height: 1, indent: 20, endIndent: 20),
                    ListTile(
                      leading: const Icon(Icons.calendar_month_outlined),
                      title: Text(context.l10n.navSchedule),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => context.push(AppRoutes.schedule),
                    ),
                    const Divider(height: 1, indent: 20, endIndent: 20),
                    ListTile(
                      leading: const Icon(Icons.event_busy_outlined),
                      title: Text(context.l10n.navAbsences),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => context.push(AppRoutes.absences),
                    ),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 24),
            _ResourcesSection(canViewDirectory: user.canViewDirectory),
            const SizedBox(height: 24),
            Padding(
              padding: const EdgeInsets.only(left: 4, bottom: 8),
              child: Text(
                context.l10n.commonAccount,
                style: Theme.of(context).textTheme.titleSmall?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
              ),
            ),
            Card(
              child: Column(
                children: [
                  ListTile(
                    leading: const Icon(Icons.language),
                    title: Text(context.l10n.settingsLanguage),
                    trailing: Text(
                      localeName(Localizations.localeOf(context)),
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                    ),
                    onTap: () => _pickLanguage(context, ref),
                  ),
                  const Divider(height: 1, indent: 20, endIndent: 20),
                  ListTile(
                    leading: const Icon(Icons.password),
                    title: Text(context.l10n.authChangePassword),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push(AppRoutes.changePassword),
                  ),
                  const Divider(height: 1, indent: 20, endIndent: 20),
                  ListTile(
                    leading: const Icon(Icons.logout),
                    title: Text(context.l10n.commonSignOut),
                    onTap: () => _confirmSignOut(context, ref),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _refreshProfile(BuildContext context, WidgetRef ref) async {
    final messenger = ScaffoldMessenger.of(context);
    // Read before the await, with the messenger and for the same reason: the
    // context may be gone by the time the request fails.
    final l10n = context.l10n;

    try {
      await ref.read(authControllerProvider.notifier).refreshProfile();
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
    }
  }

  /// Lets the user override the device language. On a shared site phone the
  /// device is often set to whatever the last person left it on, so following
  /// it blindly is not enough.
  Future<void> _pickLanguage(BuildContext context, WidgetRef ref) async {
    final current = Localizations.localeOf(context);

    final chosen = await showModalBottomSheet<Locale>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final locale in supportedLocales)
              ListTile(
                title: Text(localeName(locale)),
                trailing: locale.languageCode == current.languageCode
                    ? const Icon(Icons.check)
                    : null,
                onTap: () => Navigator.of(sheetContext).pop(locale),
              ),
          ],
        ),
      ),
    );

    if (chosen != null) {
      await ref.read(localeControllerProvider.notifier).select(chosen);
    }
  }

  Future<void> _confirmSignOut(BuildContext context, WidgetRef ref) async {
    final shouldSignOut = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(context.l10n.commonSignOutQuestion),
        content: Text(context.l10n.commonSignOutBody),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(context.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(context.l10n.commonSignOut),
          ),
        ],
      ),
    );

    if (shouldSignOut ?? false) {
      await ref.read(authControllerProvider.notifier).signOut();
    }
  }
}

/// Site resources: vehicles, tools and materials. The directory-gated ones
/// mirror the API's `ForemanAndAbove` policy; tool lookup mirrors the API's
/// `AllEmployees` policy on `by-qr`, so it stays visible for a Worker too.
class _ResourcesSection extends StatelessWidget {
  const _ResourcesSection({required this.canViewDirectory});

  final bool canViewDirectory;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Text(
            context.l10n.commonResources,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ),
        Card(
          child: Column(
            children: [
              if (canViewDirectory) ...[
                ListTile(
                  leading: const Icon(Icons.local_shipping_outlined),
                  title: Text(context.l10n.navVehicles),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push(AppRoutes.vehicles),
                ),
                const Divider(height: 1, indent: 20, endIndent: 20),
                ListTile(
                  leading: const Icon(Icons.handyman_outlined),
                  title: Text(context.l10n.navTools),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push(AppRoutes.tools),
                ),
                const Divider(height: 1, indent: 20, endIndent: 20),
                ListTile(
                  leading: const Icon(Icons.inventory_2_outlined),
                  title: Text(context.l10n.navMaterials),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push(AppRoutes.materials),
                ),
                const Divider(height: 1, indent: 20, endIndent: 20),
                // Foreman and above, the same set the API lets record
                // spending. The person filling the tank is standing next to
                // the receipt and the odometer.
                ListTile(
                  leading: const Icon(Icons.local_gas_station_outlined),
                  title: Text(context.l10n.navVehicleExpenses),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push(AppRoutes.vehicleExpenses),
                ),
                const Divider(height: 1, indent: 20, endIndent: 20),
              ],
              ListTile(
                leading: const Icon(Icons.qr_code_scanner_outlined),
                title: Text(context.l10n.scanTitle),
                subtitle: Text(context.l10n.toolByQrCode),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.push(AppRoutes.scan),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Explains why push notifications are silent, when they are. The in-app
/// inbox keeps working either way, so this is informational only.
class _PushStatusNotice extends ConsumerWidget {
  const _PushStatusNotice();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final push = ref.watch(pushControllerProvider);

    final l10n = context.l10n;

    final message = switch (push.status) {
      PushStatus.permissionDenied => l10n.notificationsBlockedBody,
      PushStatus.unconfigured => l10n.notificationsNotConfiguredBody,
      // Either a message of ours or, failing that, whatever the platform said.
      PushStatus.error =>
        push.message?.resolve(l10n) ?? push.failure?.describe(l10n) ?? push.detail,
      _ => null,
    };

    if (message == null) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Card(
        child: ListTile(
          leading: const Icon(Icons.notifications_off_outlined),
          title: Text(
            message,
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ),
      ),
    );
  }
}

class _ProfileCard extends StatelessWidget {
  const _ProfileCard({required this.user});

  final User user;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            CircleAvatar(
              radius: 30,
              backgroundColor: theme.colorScheme.primaryContainer,
              child: Text(
                initialsOf(user.firstName, user.lastName, fallback: user.email),
                style: theme.textTheme.titleLarge?.copyWith(
                  color: theme.colorScheme.onPrimaryContainer,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user.displayName,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    user.email,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 10),
                  Chip(
                    label: Text(humanizeEnum(user.role)),
                    visualDensity: VisualDensity.compact,
                    padding: EdgeInsets.zero,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
