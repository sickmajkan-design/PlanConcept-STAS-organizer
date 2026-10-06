import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/confirm_dialog.dart';
import '../../../core/widgets/failure_view.dart';
import '../../../core/widgets/info_tile.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../../attachments/presentation/attachment_section.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/models/vehicle.dart';
import '../data/vehicle_repository.dart';
import 'vehicle_form_sheet.dart';
import 'vehicle_rental_out_section.dart';
import 'vehicle_rental_rate_section.dart';
import 'vehicle_toll_section.dart';
import 'vehicles_controller.dart';

class VehicleDetailScreen extends ConsumerWidget {
  const VehicleDetailScreen({super.key, required this.vehicleId});

  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(vehicleDetailProvider(vehicleId));
    final canManage = ref.watch(currentUserProvider)?.isAdminAndAbove ?? false;
    final loadedVehicle = detail.value;

    return Scaffold(
      appBar: AppBar(
        title: Text(loadedVehicle?.displayName ?? context.l10n.commonVehicle),
        actions: [
          if (canManage && loadedVehicle != null) ...[
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              tooltip: context.l10n.commonEdit,
              onPressed: () => showVehicleFormSheet(
                context,
                ref,
                existing: loadedVehicle,
              ),
            ),
            PopupMenuButton<void>(
              itemBuilder: (context) => [
                PopupMenuItem(
                  onTap: () => Future.microtask(() {
                    if (!context.mounted) return;
                    _delete(context, ref, loadedVehicle);
                  }),
                  child: Text(
                    context.l10n.commonDelete,
                    style: TextStyle(color: Theme.of(context).colorScheme.error),
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
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
                  title: context.l10n.commonVehicle,
                  children: [
                    InfoTile(
                      icon: Icons.pin_outlined,
                      label: context.l10n.vehicleRegistration,
                      value: vehicle.registrationNumber,
                    ),
                    InfoTile(
                      icon: Icons.tag_outlined,
                      label: context.l10n.vehicleTdNumber,
                      value: vehicle.tdNumber,
                    ),
                    InfoTile(
                      icon: Icons.confirmation_number_outlined,
                      label: 'VIN',
                      value: vehicle.vin,
                    ),
                    InfoTile(
                      icon: Icons.qr_code_outlined,
                      label: context.l10n.vehicleQrCode,
                      value: vehicle.qrCode,
                    ),
                    InfoTile(
                      icon: Icons.local_gas_station_outlined,
                      label: context.l10n.vehicleFuelType,
                      value: humanizeEnum(vehicle.fuelType),
                    ),
                    InfoTile(
                      icon: Icons.event_available_outlined,
                      label: context.l10n.vehicleRegistrationValidUntil,
                      value: _date(vehicle.registrationValidUntil),
                    ),
                    InfoTile(
                      icon: Icons.fact_check_outlined,
                      label: context.l10n.vehicleInspectionValidUntil,
                      value: _date(vehicle.technicalInspectionValidUntil),
                    ),
                    InfoTile(
                      icon: Icons.verified_user_outlined,
                      label: context.l10n.vehicleInsuranceValidUntil,
                      value: _date(vehicle.insuranceValidUntil),
                    ),
                    InfoTile(
                      icon: Icons.build_circle_outlined,
                      label: context.l10n.vehicleNextServiceDue,
                      value: _date(vehicle.nextServiceDue),
                    ),
                    if (vehicle.isHeldOnRental)
                      InfoTile(
                        icon: Icons.event_busy_outlined,
                        label: context.l10n.vehicleRentedUntil,
                        value: _date(vehicle.rentedUntil),
                      ),
                    if (vehicle.isLoanedOut && vehicle.currentRentalOutExpectedEndDate != null)
                      InfoTile(
                        icon: Icons.assignment_return_outlined,
                        label: context.l10n.vehicleDueBack,
                        value: _date(vehicle.currentRentalOutExpectedEndDate),
                      ),
                    if (vehicle.isRented) ...[
                      InfoTile(
                        icon: Icons.request_quote_outlined,
                        label: context.l10n.vehicleRentalProvider,
                        value: vehicle.currentRentalProvider,
                      ),
                      InfoTile(
                        icon: Icons.payments_outlined,
                        label: context.l10n.vehicleRentalMonthlyAmount,
                        value: vehicle.currentRentalMonthlyAmount == null
                            ? null
                            : formatAmount(vehicle.currentRentalMonthlyAmount),
                      ),
                    ],
                    if (vehicle.isLoanedOut)
                      InfoTile(
                        icon: Icons.handshake_outlined,
                        label: context.l10n.commonAssignment,
                        value: context.l10n.vehicleLoanedOutTo(
                          vehicle.currentRentalOutRenterName!,
                        ),
                      ),
                  ],
                ),
                if (vehicle.isRented) ...[
                  const SizedBox(height: 20),
                  VehicleRentalRateSection(vehicleId: vehicle.id),
                ],
                const SizedBox(height: 20),
                VehicleTollSection(vehicleId: vehicle.id),
                const SizedBox(height: 20),
                VehicleRentalOutSection(vehicle: vehicle),
                const SizedBox(height: 20),
                _Section(
                  title: context.l10n.commonAssignment,
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
                      ListTile(
                        leading: Icon(Icons.person_off_outlined),
                        title: Text(context.l10n.vehicleUnassigned),
                      ),
                  ],
                ),
                const SizedBox(height: 20),
                _Section(
                  title: attachmentsTitleWithCount(context, ref, ownerType: 'Vehicle', ownerId: vehicle.id),
                  children: [
                    AttachmentSection(ownerType: 'Vehicle', ownerId: vehicle.id),
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

/// A `YYYY-MM-DD` date as the app writes dates, or null when there is none.
String? _date(String? iso) {
  final parsed = iso == null ? null : DateTime.tryParse(iso);

  return parsed == null ? null : formatDate(parsed);
}

Future<void> _delete(BuildContext context, WidgetRef ref, Vehicle vehicle) async {
  final l10n = context.l10n;

  final confirmed = await showConfirmDialog(
    context,
    title: l10n.vehicleDeleteTitle,
    body: l10n.vehicleDeleteBody(vehicle.displayName),
    destructive: true,
  );

  if (!confirmed || !context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);
  final router = GoRouter.of(context);

  try {
    await ref.read(vehicleRepositoryProvider).remove(vehicle.id);
    await ref.read(vehiclesControllerProvider.notifier).refresh();

    if (router.canPop()) {
      router.pop();
    }
  } on ApiException catch (exception) {
    messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));
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
                  Wrap(
                    spacing: 8,
                    runSpacing: 4,
                    children: [
                      StatusChip(status: vehicle.status, kind: EnumKind.vehicleStatus),
                      StatusChip(
                        status: vehicle.ownershipType,
                        kind: EnumKind.vehicleOwnershipType,
                      ),
                    ],
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
