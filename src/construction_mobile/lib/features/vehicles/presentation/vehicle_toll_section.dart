import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/vehicle_toll.dart';
import '../data/vehicle_toll_repository.dart';

final _vehicleTollsProvider = FutureProvider.autoDispose
    .family<List<VehicleToll>, String>((ref, vehicleId) {
  return ref.watch(vehicleTollRepositoryProvider).fetch(vehicleId);
});

/// Which vignettes, tunnels and passages are paid for a vehicle, and until
/// when — so a driver can check before setting off. Read-only and shown to
/// every role; the API already lists expired / expiring-soon entries first.
class VehicleTollSection extends ConsumerWidget {
  const VehicleTollSection({super.key, required this.vehicleId});

  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final tolls = ref.watch(_vehicleTollsProvider(vehicleId));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Text(
            l10n.vehicleTollsTitle,
            style: theme.textTheme.titleSmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ),
        Card(
          child: tolls.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (_, _) => const SizedBox.shrink(),
            data: (items) {
              if (items.isEmpty) {
                return ListTile(
                  leading: const Icon(Icons.toll_outlined),
                  title: Text(l10n.vehicleTollsEmpty),
                );
              }

              return Column(
                children: [
                  for (final toll in items)
                    ListTile(
                      leading: const Icon(Icons.toll_outlined),
                      title: Text(
                        toll.routeSegment?.isNotEmpty == true
                            ? '${enumLabel(l10n, EnumKind.vehicleTollType, toll.type)}'
                                ' · ${toll.country} · ${toll.routeSegment}'
                            : '${enumLabel(l10n, EnumKind.vehicleTollType, toll.type)}'
                                ' · ${toll.country}',
                      ),
                      subtitle: toll.validUntil == null
                          ? null
                          : Text(
                              l10n.vehicleTollValidUntil(
                                formatDate(toll.validUntil),
                              ),
                            ),
                      trailing: StatusChip(
                        status: toll.computedState,
                        kind: EnumKind.vehicleTollState,
                        dense: true,
                      ),
                    ),
                ],
              );
            },
          ),
        ),
      ],
    );
  }
}
