import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/notification_repository.dart';
import 'acknowledgment_gate.dart';

/// Compose-and-send sheet reachable from a Foreman's own-site crew list, or
/// from an employee's detail screen for ProjectManager and above — see
/// `AttachmentRules`-style layered authorization on the backend command for
/// exactly who may reach whom; this sheet does not attempt to replicate that
/// scoping, it just lets the API's own 403 surface if the caller cannot.
Future<void> showNotifyEmployeeSheet(
  BuildContext context,
  WidgetRef ref, {
  required String employeeId,
  required String employeeName,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _NotifyEmployeeSheet(
      employeeId: employeeId,
      employeeName: employeeName,
    ),
  );
}

class _NotifyEmployeeSheet extends ConsumerStatefulWidget {
  const _NotifyEmployeeSheet({
    required this.employeeId,
    required this.employeeName,
  });

  final String employeeId;
  final String employeeName;

  @override
  ConsumerState<_NotifyEmployeeSheet> createState() =>
      _NotifyEmployeeSheetState();
}

class _NotifyEmployeeSheetState extends ConsumerState<_NotifyEmployeeSheet> {
  final _titleController = TextEditingController();
  final _bodyController = TextEditingController();
  bool _requiresAcknowledgment = false;
  bool _busy = false;
  ApiException? _error;

  @override
  void dispose() {
    _titleController.dispose();
    _bodyController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final canSend = !_busy &&
        _titleController.text.trim().isNotEmpty &&
        _bodyController.text.trim().isNotEmpty;

    return Padding(
      padding: EdgeInsets.only(
        left: 20,
        right: 20,
        top: 20,
        bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.notifyEmployeeTitle,
              style:
                  theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 4),
            Text(
              widget.employeeName,
              style: theme.textTheme.bodyMedium
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 16),
            if (_error != null) ...[
              Text(
                _error!.describe(l10n),
                style: TextStyle(color: theme.colorScheme.error),
              ),
              const SizedBox(height: 12),
            ],
            TextField(
              controller: _titleController,
              enabled: !_busy,
              maxLength: 200,
              decoration: InputDecoration(labelText: l10n.notifyEmployeeSubject),
              onChanged: (_) => setState(() {}),
            ),
            TextField(
              controller: _bodyController,
              enabled: !_busy,
              maxLines: 4,
              maxLength: 2000,
              decoration: InputDecoration(labelText: l10n.notifyEmployeeMessage),
              onChanged: (_) => setState(() {}),
            ),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              value: _requiresAcknowledgment,
              onChanged: _busy
                  ? null
                  : (value) => setState(() => _requiresAcknowledgment = value),
              title: Text(l10n.notifyEmployeeRequireAck),
              subtitle: Text(l10n.notifyEmployeeRequireAckHint),
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: canSend ? _send : null,
                child: _busy
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(l10n.notifyEmployeeSend),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _send() async {
    if (blockedByPendingAcknowledgment(context, ref)) return;

    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      await ref.read(notificationRepositoryProvider).sendDirect(
            employeeId: widget.employeeId,
            title: _titleController.text.trim(),
            body: _bodyController.text.trim(),
            requiresAcknowledgment: _requiresAcknowledgment,
          );

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.notifyEmployeeSent)));
    } on ApiException catch (exception) {
      setState(() {
        _error = exception;
        _busy = false;
      });
    }
  }
}
