import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/idempotency.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/widgets/status_chip.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../employees/data/employee_repository.dart';
import '../../employees/data/models/employee.dart';
import '../../notifications/presentation/acknowledgment_gate.dart';
import '../../projects/data/models/project.dart';
import '../../projects/data/project_repository.dart';
import '../../tools/data/models/tool.dart';
import '../../tools/data/tool_repository.dart';
import '../../vehicles/data/models/vehicle.dart';
import '../../vehicles/data/vehicle_repository.dart';
import 'qr_scanner_page.dart';

/// What the last lookup found: a tool, a vehicle, or nothing yet.
sealed class _ScanResult {
  const _ScanResult();
}

class _ToolResult extends _ScanResult {
  const _ToolResult(this.tool);
  final Tool tool;
}

class _VehicleResult extends _ScanResult {
  const _VehicleResult(this.vehicle);
  final Vehicle vehicle;
}

/// Roles the API lets hand a tool to someone else — mirrors
/// `Policies.ForemanAndAbove`, the policy on the tool assign endpoints.
const _toolTransferRoles = <String>{'SuperAdmin', 'Admin', 'ProjectManager', 'Foreman'};

/// Roles the API lets hand a vehicle to someone else — mirrors
/// `Policies.ProjectManagerAndAbove`, the policy on the vehicle assign
/// endpoints. Narrower than tools: a foreman may move a wrench, not a truck.
const _vehicleTransferRoles = <String>{'SuperAdmin', 'Admin', 'ProjectManager'};

/// Looks a tool or vehicle up by its QR label — scanned with the camera or
/// typed in — and lets the operator check it out to themselves, return it,
/// or (Foreman and above for tools, Project Manager and above for vehicles)
/// hand it straight to another employee or site.
///
/// Self-checkout/-return stays self-service only: the API always resolves
/// the target employee from the caller's own session. Transfer is different
/// — it names someone else — so it is gated to the same roles the admin
/// panel's own assign buttons already require.
class ScanScreen extends ConsumerStatefulWidget {
  const ScanScreen({super.key});

  @override
  ConsumerState<ScanScreen> createState() => _ScanScreenState();
}

class _ScanScreenState extends ConsumerState<ScanScreen> {
  final _controller = TextEditingController();
  bool _isLoading = false;
  ApiException? _failure;
  _ScanResult? _result;

  bool _actionBusy = false;
  String? _actionKey;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _scanWithCamera() async {
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const QrScannerPage()),
    );

    if (code == null || !mounted) return;

    _controller.text = code;
    await _lookup();
  }

  Future<void> _lookup() async {
    final code = _controller.text.trim();

    if (code.isEmpty) {
      return;
    }

    setState(() {
      _isLoading = true;
      _failure = null;
      _result = null;
      _actionKey = null;
    });

    try {
      final tool = await ref.read(toolRepositoryProvider).fetchToolByQrCode(code);
      if (!mounted) return;
      setState(() => _result = _ToolResult(tool));
      return;
    } on ApiException catch (exception) {
      if (exception.kind != ApiFailureKind.notFound) {
        if (!mounted) return;
        setState(() => _failure = exception);
        return;
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }

    // Not a tool — the same code might belong to a vehicle.
    setState(() => _isLoading = true);

    try {
      final vehicle =
          await ref.read(vehicleRepositoryProvider).fetchVehicleByQrCode(code);
      if (!mounted) return;
      setState(() => _result = _VehicleResult(vehicle));
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _checkOutToMe() async {
    final result = _result;
    if (result == null) return;
    if (blockedByPendingAcknowledgment(context, ref)) return;

    _actionKey ??= newIdempotencyKey();
    setState(() {
      _actionBusy = true;
      _failure = null;
    });

    try {
      switch (result) {
        case _ToolResult(:final tool):
          final updated = await ref
              .read(toolRepositoryProvider)
              .checkOutToMe(tool.id, idempotencyKey: _actionKey!);
          if (!mounted) return;
          setState(() => _result = _ToolResult(updated));
        case _VehicleResult(:final vehicle):
          final updated = await ref
              .read(vehicleRepositoryProvider)
              .checkOutToMe(vehicle.id, idempotencyKey: _actionKey!);
          if (!mounted) return;
          setState(() => _result = _VehicleResult(updated));
      }

      _actionKey = null;
      if (!mounted) return;
      _showSnackBar(context.l10n.scanCheckOutSuccess);
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _actionBusy = false);
      }
    }
  }

  Future<void> _returnItem() async {
    final result = _result;
    if (result == null) return;
    if (blockedByPendingAcknowledgment(context, ref)) return;

    _actionKey ??= newIdempotencyKey();
    setState(() {
      _actionBusy = true;
      _failure = null;
    });

    try {
      switch (result) {
        case _ToolResult(:final tool):
          final updated = await ref
              .read(toolRepositoryProvider)
              .returnTool(tool.id, idempotencyKey: _actionKey!);
          if (!mounted) return;
          setState(() => _result = _ToolResult(updated));
        case _VehicleResult(:final vehicle):
          final updated = await ref
              .read(vehicleRepositoryProvider)
              .returnVehicle(vehicle.id, idempotencyKey: _actionKey!);
          if (!mounted) return;
          setState(() => _result = _VehicleResult(updated));
      }

      _actionKey = null;
      if (!mounted) return;
      _showSnackBar(context.l10n.scanReturnSuccess);
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _actionBusy = false);
      }
    }
  }

  Future<void> _transferToEmployee() async {
    final result = _result;
    if (result == null) return;
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final employee = await _pickEmployee();
    if (employee == null || !mounted) return;

    final key = newIdempotencyKey();
    setState(() {
      _actionBusy = true;
      _failure = null;
    });

    try {
      switch (result) {
        case _ToolResult(:final tool):
          final updated = await ref
              .read(toolRepositoryProvider)
              .assignToEmployee(tool.id, employee.id, idempotencyKey: key);
          if (!mounted) return;
          setState(() => _result = _ToolResult(updated));
        case _VehicleResult(:final vehicle):
          final updated = await ref
              .read(vehicleRepositoryProvider)
              .assignToEmployee(vehicle.id, employee.id, idempotencyKey: key);
          if (!mounted) return;
          setState(() => _result = _VehicleResult(updated));
      }

      if (!mounted) return;
      _showSnackBar(context.l10n.scanTransferSuccess(employee.fullName));
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _actionBusy = false);
      }
    }
  }

  Future<void> _transferToProjectAction() async {
    final result = _result;
    if (result == null) return;
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final project = await _pickProject();
    if (project == null || !mounted) return;

    final key = newIdempotencyKey();
    setState(() {
      _actionBusy = true;
      _failure = null;
    });

    try {
      switch (result) {
        case _ToolResult(:final tool):
          final updated = await ref
              .read(toolRepositoryProvider)
              .assignToProject(tool.id, project.id, idempotencyKey: key);
          if (!mounted) return;
          setState(() => _result = _ToolResult(updated));
        case _VehicleResult(:final vehicle):
          final updated = await ref
              .read(vehicleRepositoryProvider)
              .assignToProject(vehicle.id, project.id, idempotencyKey: key);
          if (!mounted) return;
          setState(() => _result = _VehicleResult(updated));
      }

      if (!mounted) return;
      _showSnackBar(context.l10n.scanTransferProjectSuccess(project.name));
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _actionBusy = false);
      }
    }
  }

  Future<Employee?> _pickEmployee() {
    return showModalBottomSheet<Employee>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _EmployeePickerSheet(),
    );
  }

  Future<Project?> _pickProject() {
    return showModalBottomSheet<Project>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _ProjectPickerSheet(),
    );
  }

  void _showSnackBar(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final role = ref.watch(currentUserProvider)?.role;

    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.scanTitle)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              context.l10n.scanHint,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () => unawaited(_scanWithCamera()),
              icon: const Icon(Icons.qr_code_scanner_outlined),
              label: Text(context.l10n.scanAction),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _controller,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                labelText: context.l10n.scanCodeLabel,
                prefixIcon: const Icon(Icons.tag_outlined),
              ),
              onSubmitted: (_) => unawaited(_lookup()),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _isLoading ? null : () => unawaited(_lookup()),
              icon: _isLoading
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.search),
              label: Text(context.l10n.toolLookUpAction),
            ),
            const SizedBox(height: 24),
            if (_failure != null)
              Card(
                color: Theme.of(context).colorScheme.errorContainer,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(
                    _failure!.describe(context.l10n),
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.onErrorContainer,
                    ),
                  ),
                ),
              ),
            if (_result case final result?)
              _ScanResultCard(
                result: result,
                busy: _actionBusy,
                canTransfer: switch (result) {
                  _ToolResult() => role != null && _toolTransferRoles.contains(role),
                  _VehicleResult() => role != null && _vehicleTransferRoles.contains(role),
                },
                onCheckOutToMe: _checkOutToMe,
                onReturn: _returnItem,
                onTransferToEmployee: _transferToEmployee,
                onTransferToProject: _transferToProjectAction,
              ),
          ],
        ),
      ),
    );
  }
}

class _ScanResultCard extends ConsumerWidget {
  const _ScanResultCard({
    required this.result,
    required this.busy,
    required this.canTransfer,
    required this.onCheckOutToMe,
    required this.onReturn,
    required this.onTransferToEmployee,
    required this.onTransferToProject,
  });

  final _ScanResult result;
  final bool busy;
  final bool canTransfer;
  final VoidCallback onCheckOutToMe;
  final VoidCallback onReturn;
  final VoidCallback onTransferToEmployee;
  final VoidCallback onTransferToProject;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final l10n = context.l10n;
    final employeeId = ref.watch(currentUserProvider)?.employeeId;

    final (
      title,
      kindLabel,
      statusValue,
      statusKind,
      assignedEmployeeId,
      assignedEmployeeName,
      detailPath,
    ) = switch (result) {
      _ToolResult(:final tool) => (
          tool.name,
          l10n.scanToolFound,
          tool.status,
          EnumKind.toolStatus,
          tool.assignedEmployeeId,
          tool.assignedEmployeeName,
          AppRoutes.toolDetail(tool.id),
        ),
      _VehicleResult(:final vehicle) => (
          vehicle.displayName,
          l10n.scanVehicleFound,
          vehicle.status,
          EnumKind.vehicleStatus,
          vehicle.assignedEmployeeId,
          vehicle.assignedEmployeeName,
          AppRoutes.vehicleDetail(vehicle.id),
        ),
    };

    final isMine = employeeId != null && assignedEmployeeId == employeeId;
    final isFree = assignedEmployeeId == null;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        kindLabel,
                        style: theme.textTheme.labelMedium?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                      Text(
                        title,
                        style: theme.textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
                StatusChip(status: statusValue, kind: statusKind),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              isFree
                  ? l10n.scanNotCheckedOut
                  : isMine
                      ? l10n.scanCheckedOutToYou
                      : l10n.scanCheckedOutToOther(assignedEmployeeName ?? ''),
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 16),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (isMine)
                  FilledButton.icon(
                    onPressed: busy ? null : onReturn,
                    icon: busy
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.assignment_return_outlined),
                    label: Text(l10n.scanReturn),
                  )
                else if (isFree)
                  FilledButton.icon(
                    onPressed: busy ? null : onCheckOutToMe,
                    icon: busy
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.check_circle_outline),
                    label: Text(l10n.scanCheckOutToMe),
                  ),
                if (canTransfer) ...[
                  OutlinedButton.icon(
                    onPressed: busy ? null : onTransferToEmployee,
                    icon: const Icon(Icons.person_outline),
                    label: Text(l10n.scanTransferToEmployee),
                  ),
                  OutlinedButton.icon(
                    onPressed: busy ? null : onTransferToProject,
                    icon: const Icon(Icons.apartment_outlined),
                    label: Text(l10n.scanTransferToProject),
                  ),
                ],
                TextButton(
                  onPressed: () => context.push(detailPath),
                  child: Text(l10n.commonDetails),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

/// The employees a caller with transfer rights can pick from. Cached for the
/// session, like the vehicle picker in the expense sheet.
final _employeeOptionsProvider = FutureProvider<List<Employee>>((ref) async {
  final page = await ref
      .read(employeeRepositoryProvider)
      .fetchEmployees(pageSize: 200, sortBy: 'lastName');

  return page.items;
});

final _projectOptionsProvider = FutureProvider<List<Project>>((ref) async {
  final page = await ref
      .read(projectRepositoryProvider)
      .fetchProjects(pageSize: 200, sortBy: 'name');

  return page.items;
});

class _EmployeePickerSheet extends ConsumerStatefulWidget {
  const _EmployeePickerSheet();

  @override
  ConsumerState<_EmployeePickerSheet> createState() => _EmployeePickerSheetState();
}

class _EmployeePickerSheetState extends ConsumerState<_EmployeePickerSheet> {
  String? _employeeId;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final options = ref.watch(_employeeOptionsProvider);

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.scanTransferToEmployee,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 16),
          options.when(
            loading: () => const LinearProgressIndicator(),
            error: (error, _) => Text(
              error is ApiException ? error.describe(l10n) : l10n.errorUnknown,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
            data: (employees) => DropdownButtonFormField<String>(
              initialValue: _employeeId,
              decoration: InputDecoration(labelText: l10n.scanPickEmployee),
              items: [
                for (final employee in employees)
                  DropdownMenuItem(
                    value: employee.id,
                    child: Text(
                      '${employee.fullName} · ${employee.employeeNumber}',
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
              ],
              onChanged: (value) => setState(() => _employeeId = value),
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
              const Spacer(),
              FilledButton(
                onPressed: _employeeId == null
                    ? null
                    : () {
                        final employee = options.value!
                            .firstWhere((employee) => employee.id == _employeeId);
                        Navigator.of(context).pop(employee);
                      },
                child: Text(l10n.scanTransferConfirm),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ProjectPickerSheet extends ConsumerStatefulWidget {
  const _ProjectPickerSheet();

  @override
  ConsumerState<_ProjectPickerSheet> createState() => _ProjectPickerSheetState();
}

class _ProjectPickerSheetState extends ConsumerState<_ProjectPickerSheet> {
  String? _projectId;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final options = ref.watch(_projectOptionsProvider);

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.scanTransferToProject,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 16),
          options.when(
            loading: () => const LinearProgressIndicator(),
            error: (error, _) => Text(
              error is ApiException ? error.describe(l10n) : l10n.errorUnknown,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
            data: (projects) => DropdownButtonFormField<String>(
              initialValue: _projectId,
              decoration: InputDecoration(labelText: l10n.scanPickProject),
              items: [
                for (final project in projects)
                  DropdownMenuItem(
                    value: project.id,
                    child: Text(project.name, overflow: TextOverflow.ellipsis),
                  ),
              ],
              onChanged: (value) => setState(() => _projectId = value),
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(l10n.commonCancel),
              ),
              const Spacer(),
              FilledButton(
                onPressed: _projectId == null
                    ? null
                    : () {
                        final project = options.value!
                            .firstWhere((project) => project.id == _projectId);
                        Navigator.of(context).pop(project);
                      },
                child: Text(l10n.scanTransferConfirm),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
