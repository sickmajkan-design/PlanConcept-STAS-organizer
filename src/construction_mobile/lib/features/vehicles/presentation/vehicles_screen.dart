import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/paged_list_view.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/vehicle.dart';
import 'vehicles_controller.dart';

class VehiclesScreen extends ConsumerWidget {
  const VehiclesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final controller = ref.read(vehiclesControllerProvider.notifier);
    final state = ref.watch(vehiclesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.navVehicles)),
      body: SafeArea(
        child: PagedListView<Vehicle>(
          state: state,
          onRefresh: controller.refresh,
          onLoadMore: controller.loadMore,
          emptyMessage: context.l10n.vehiclesEmpty,
          emptyIcon: Icons.local_shipping_outlined,
          header: ListSearchHeader(
            hintText: context.l10n.vehiclesSearchHint,
            onSearchChanged: controller.search,
            filters: vehicleStatusFilters,
            selectedFilter: controller.filter,
            onFilterSelected: controller.applyFilter,
          ),
          itemBuilder: (context, vehicle) => _VehicleCard(vehicle: vehicle),
        ),
      ),
    );
  }
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.vehicle});

  final Vehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(AppRoutes.vehicleDetail(vehicle.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Icon(
                  Icons.local_shipping_outlined,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      vehicle.displayName,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      vehicle.assignedEmployeeName ??
                          vehicle.registrationNumber,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              StatusChip(status: vehicle.status, kind: EnumKind.vehicleStatus, dense: true),
            ],
          ),
        ),
      ),
    );
  }
}
