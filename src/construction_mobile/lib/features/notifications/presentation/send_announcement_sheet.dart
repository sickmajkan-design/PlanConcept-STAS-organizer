import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../projects/data/project_repository.dart';
import '../data/notification_group_repository.dart';
import '../data/notification_repository.dart';

const _roles = ['SuperAdmin', 'Admin', 'ProjectManager', 'Foreman', 'Worker'];

final _allProjectsForAnnouncementProvider = FutureProvider.autoDispose((ref) {
  return ref.watch(projectRepositoryProvider).fetchAll();
});

final _allGroupsProvider = FutureProvider.autoDispose((ref) {
  return ref.watch(notificationGroupRepositoryProvider).fetchAll();
});

/// Writes one message to everyone, or to a role, a project's crew, and/or a
/// named group — Admin and above, mirroring desktop's `AnnounceDialog`.
Future<void> showSendAnnouncementSheet(BuildContext context, WidgetRef ref) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _SendAnnouncementSheet(),
  );
}

class _SendAnnouncementSheet extends ConsumerStatefulWidget {
  const _SendAnnouncementSheet();

  @override
  ConsumerState<_SendAnnouncementSheet> createState() =>
      _SendAnnouncementSheetState();
}

class _SendAnnouncementSheetState extends ConsumerState<_SendAnnouncementSheet> {
  final _titleController = TextEditingController();
  final _bodyController = TextEditingController();

  String? _role;
  String? _projectId;
  String? _groupId;
  bool _requiresAcknowledgment = false;
  bool _busy = false;
  ApiException? _error;

  @override
  void dispose() {
    _titleController.dispose();
    _bodyController.dispose();
    super.dispose();
  }

  bool get _canSubmit =>
      !_busy &&
      _titleController.text.trim().isNotEmpty &&
      _bodyController.text.trim().isNotEmpty;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final projects = ref.watch(_allProjectsForAnnouncementProvider);
    final groups = ref.watch(_allGroupsProvider);

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
              l10n.announceTitle,
              style:
                  theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
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
              decoration: InputDecoration(labelText: l10n.announceSubject),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _bodyController,
              enabled: !_busy,
              maxLines: 4,
              decoration: InputDecoration(labelText: l10n.announceMessage),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String?>(
              initialValue: _role,
              decoration: InputDecoration(labelText: l10n.announceAudienceRole),
              items: [
                DropdownMenuItem(value: null, child: Text(l10n.announceEveryRole)),
                for (final value in _roles)
                  DropdownMenuItem(
                    value: value,
                    child: Text(enumLabel(l10n, EnumKind.role, value)),
                  ),
              ],
              onChanged: _busy ? null : (value) => setState(() => _role = value),
            ),
            const SizedBox(height: 12),
            projects.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) => DropdownButtonFormField<String?>(
                initialValue: _projectId,
                decoration:
                    InputDecoration(labelText: l10n.announceAudienceProject),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(l10n.announceEveryProject),
                  ),
                  for (final project in items)
                    DropdownMenuItem(value: project.id, child: Text(project.name)),
                ],
                onChanged: _busy
                    ? null
                    : (value) => setState(() => _projectId = value),
              ),
            ),
            const SizedBox(height: 12),
            groups.when(
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const SizedBox.shrink(),
              data: (items) => DropdownButtonFormField<String?>(
                initialValue: _groupId,
                decoration: InputDecoration(labelText: l10n.announceAudienceGroup),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(l10n.announceEveryGroup),
                  ),
                  for (final group in items)
                    DropdownMenuItem(value: group.id, child: Text(group.name)),
                ],
                onChanged:
                    _busy ? null : (value) => setState(() => _groupId = value),
              ),
            ),
            const SizedBox(height: 4),
            CheckboxListTile(
              value: _requiresAcknowledgment,
              onChanged: _busy
                  ? null
                  : (value) =>
                      setState(() => _requiresAcknowledgment = value ?? false),
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              title: Text(l10n.announceRequiresAcknowledgment),
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: theme.colorScheme.secondaryContainer,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline,
                      size: 18, color: theme.colorScheme.onSecondaryContainer),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      l10n.announceHint,
                      style: theme.textTheme.bodySmall
                          ?.copyWith(color: theme.colorScheme.onSecondaryContainer),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _canSubmit ? _send : null,
                child: _busy
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(l10n.announceSend),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _send() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final recipients = await ref.read(notificationRepositoryProvider).sendAnnouncement(
            title: _titleController.text.trim(),
            body: _bodyController.text.trim(),
            role: _role,
            projectId: _projectId,
            groupId: _groupId,
            requiresAcknowledgment: _requiresAcknowledgment,
          );

      navigator.pop();
      messenger.showSnackBar(
        SnackBar(content: Text(l10n.announceSent(recipients))),
      );
    } on ApiException catch (exception) {
      setState(() {
        _error = exception;
        _busy = false;
      });
    }
  }
}
