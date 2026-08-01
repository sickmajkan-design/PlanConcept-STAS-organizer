import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/vehicle.dart';
import 'vehicles_controller.dart';

class VehicleDetailScreen extends ConsumerWidget {
  const VehicleDetailScreen({super.key, required this.vehicleId});

  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(vehicleDetailProvider(vehicleId));

    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.displayName ?? 'Vehicle')),
      body: SafeArea(
        child: detail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(vehicleDetailProvider(vehicleId)),
          ),
          data: (vehicle) => RefreshIndicator(
            onRefresh: () async =>
                ref.invalidate(vehicleDetailProvider(vehicleId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _Header(vehicle: vehicle),
                const SizedBox(height: 20),
                _Section(
                  title: 'Vehicle',
                  children: [
                    InfoTile(
                      icon: Icons.pin_outlined,
                      label: 'Registration number',
                      value: vehicle.registrationNumber,
                    ),
                    InfoTile(
                      icon: Icons.confirmation_number_outlined,
                      label: 'VIN',
                      value: vehicle.vin,
                    ),
                    InfoTile(
                      icon: Icons.local_gas_station_outlined,
                      label: 'Fuel type',
                      value: humanizeEnum(vehicle.fuelType),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: 'Assignment',
                  children: [
                    if (vehicle.isAssigned)
                      ListTile(
                        leading: const Icon(Icons.person_outline),
                        title: Text(vehicle.assignedEmployeeName!),
                        subtitle: Text(vehicle.assignedEmployeeNumber ?? ''),
                        onTap: () => context.push(
                          AppRoutes.employeeDetail(vehicle.assignedEmployeeId!),
                        ),
                      )
                    else
                      const ListTile(
                        leading: Icon(Icons.person_off_outlined),
                        title: Text('Not assigned to any employee'),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.vehicle});

  final Vehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            CircleAvatar(
              radius: 32,
              backgroundColor: theme.colorScheme.primaryContainer,
              child: Icon(
                Icons.local_shipping_outlined,
                size: 30,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    vehicle.displayName,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    vehicle.registrationNumber,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 10),
                  StatusChip(status: vehicle.status),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: Text(
            title,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ),
        Card(child: Column(children: children)),
      ],
    );
  }
}
