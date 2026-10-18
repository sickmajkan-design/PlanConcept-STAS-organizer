import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/auto_refresh.dart';
import '../../../core/widgets/failure_view.dart';
import '../../attachments/data/models/attachment.dart';
import '../../attachments/presentation/attachment_preview_screen.dart';
import '../../my_documents/data/my_documents_repository.dart';
import '../data/document_request_repository.dart';
import '../../../core/time/company_time.dart';

/// The label and colour of a request's status, shared by the list and the detail.
({String label, Color? color, IconData icon}) requestStatusStyle(
  BuildContext context,
  DocumentRequest request,
) {
  final l10n = context.l10n;
  final scheme = Theme.of(context).colorScheme;

  if (request.isOverdueOn(CompanyTime.now())) {
    return (
      label: l10n.requestStatusOverdue,
      color: scheme.error,
      icon: Icons.error_outline,
    );
  }

  return switch (request.status) {
    'Submitted' => (
      label: l10n.requestStatusSubmitted,
      color: Colors.amber.shade800,
      icon: Icons.hourglass_top_outlined,
    ),
    'Accepted' => (
      label: l10n.requestStatusAccepted,
      color: Colors.green.shade700,
      icon: Icons.check_circle_outline,
    ),
    'Returned' => (
      label: l10n.requestStatusReturned,
      color: scheme.error,
      icon: Icons.replay,
    ),
    'Cancelled' => (
      label: l10n.requestStatusCancelled,
      color: scheme.onSurfaceVariant,
      icon: Icons.block,
    ),
    _ => (
      label: l10n.requestStatusOpen,
      color: scheme.primary,
      icon: Icons.upload_file_outlined,
    ),
  };
}

/// One request: what is asked, where it stands, the history, and — while it waits for the person —
/// the way to answer it from the camera, the gallery or a file.
class DocumentRequestScreen extends ConsumerStatefulWidget {
  const DocumentRequestScreen({super.key, required this.requestId});

  final String requestId;

  @override
  ConsumerState<DocumentRequestScreen> createState() =>
      _DocumentRequestScreenState();
}

class _DocumentRequestScreenState extends ConsumerState<DocumentRequestScreen> {
  final List<AnswerFile> _picked = [];
  bool _busy = false;
  ApiException? _error;
  String? _localError;

  Future<void> _refresh() async {
    ref.invalidate(documentRequestProvider(widget.requestId));
    ref.invalidate(myDocumentRequestsProvider);
    await ref.read(documentRequestProvider(widget.requestId).future);
  }

  void _add(AnswerFile file) {
    final l10n = context.l10n;

    if (_picked.length >= DocumentRequestRepository.maxFiles) {
      setState(
        () => _localError = l10n.requestTooManyFiles(
          DocumentRequestRepository.maxFiles,
        ),
      );
      return;
    }

    if (file.sizeBytes > DocumentRequestRepository.maxFileBytes) {
      setState(
        () => _localError = l10n.attachmentsTooLarge(
          DocumentRequestRepository.maxFileBytes ~/ (1024 * 1024),
        ),
      );
      return;
    }

    setState(() {
      _localError = null;
      _picked.add(file);
    });
  }

  Future<void> _fromCamera({bool gallery = false}) async {
    // Re-encoded on the way out: a phone frame is 8–12 MB and a document needs far less.
    final picked = await ImagePicker().pickImage(
      source: gallery ? ImageSource.gallery : ImageSource.camera,
      maxWidth: 2400,
      maxHeight: 2400,
      imageQuality: 88,
    );

    if (picked == null || !mounted) {
      return;
    }

    _add(
      AnswerFile(
        path: picked.path,
        name: picked.name,
        sizeBytes: await picked.length(),
      ),
    );
  }

  Future<void> _fromFiles() async {
    final file = await FilePicker.pickFile(
      type: FileType.custom,
      allowedExtensions: DocumentRequestRepository.extensions,
    );

    final path = file?.path;

    if (file == null || path == null || !mounted) {
      return;
    }

    final size = file.lengthSync() ?? await file.length();

    if (!mounted) {
      return;
    }

    _add(AnswerFile(path: path, name: file.name, sizeBytes: size));
  }

  Future<void> _send(DocumentRequest request) async {
    setState(() {
      _busy = true;
      _error = null;
      _localError = null;
    });

    try {
      await ref
          .read(documentRequestRepositoryProvider)
          .submit(request.id, _picked);

      if (!mounted) return;

      setState(_picked.clear);
      ref.invalidate(myDocumentRequestsProvider);
      ref.invalidate(documentRequestProvider(widget.requestId));
      ref.invalidate(myDocumentsProvider);

      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(context.l10n.requestSent)));
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _error = exception);
      // The request may have changed under the person; show what it is now.
      ref.invalidate(documentRequestProvider(widget.requestId));
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final state = ref.watch(documentRequestProvider(widget.requestId));

    return Scaffold(
      appBar: AppBar(title: Text(l10n.requestTitle)),
      body: SafeArea(
        child: AutoRefresh(
          // Not while something is being sent, so the screen never swaps under the person's hands.
          onRefresh: () {
            if (!_busy) {
              ref.invalidate(documentRequestProvider(widget.requestId));
              ref.invalidate(myDocumentRequestsProvider);
            }
          },
          child: RefreshIndicator(
            onRefresh: _refresh,
            child: state.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ListView(
                children: [
                  SizedBox(
                    height: MediaQuery.of(context).size.height * 0.7,
                    child: FailureView(error: error, onRetry: _refresh),
                  ),
                ],
              ),
              data: (request) => _body(context, request),
            ),
          ),
        ),
      ),
    );
  }

  Widget _body(BuildContext context, DocumentRequest request) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final style = requestStatusStyle(context, request);

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(request.title, style: theme.textTheme.titleLarge),
        const SizedBox(height: 4),
        Text(
          enumLabel(l10n, EnumKind.attachmentCategory, request.category),
          style: theme.textTheme.bodySmall,
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Icon(style.icon, color: style.color),
            const SizedBox(width: 8),
            Text(
              style.label,
              style: theme.textTheme.titleSmall?.copyWith(color: style.color),
            ),
            const Spacer(),
            if (request.dueDate != null)
              Text(
                l10n.requestDue(formatDate(request.dueDate)),
                style: theme.textTheme.bodySmall,
              ),
          ],
        ),
        if (request.note != null && request.note!.isNotEmpty) ...[
          const SizedBox(height: 12),
          Text(request.note!),
        ],
        if (request.status == 'Returned' && request.returnReason != null) ...[
          const SizedBox(height: 16),
          Card(
            color: theme.colorScheme.errorContainer,
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.requestReturnedBecause,
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: theme.colorScheme.onErrorContainer,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    request.returnReason!,
                    style: TextStyle(color: theme.colorScheme.onErrorContainer),
                  ),
                ],
              ),
            ),
          ),
        ],
        if (request.files.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.requestSentFiles, style: theme.textTheme.titleSmall),
          for (final file in request.files)
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: Icon(
                file.contentType.startsWith('image/')
                    ? Icons.image_outlined
                    : Icons.description_outlined,
              ),
              title: Text(file.fileName),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => AttachmentPreviewScreen(
                    attachment: Attachment(
                      id: file.id,
                      fileName: file.fileName,
                      contentType: file.contentType,
                      sizeBytes: file.sizeBytes,
                      category: request.category,
                      title: request.title,
                      ownerType: 'Employee',
                      ownerId: '',
                      createdAt: DateTime.now(),
                    ),
                  ),
                ),
              ),
            ),
        ],
        if (request.canBeAnswered) ..._answer(context, request),
        if (request.events.isNotEmpty) ...[
          const SizedBox(height: 24),
          Text(l10n.requestHistory, style: theme.textTheme.titleSmall),
          const SizedBox(height: 8),
          for (final event in request.events) _EventRow(event: event),
        ],
      ],
    );
  }

  List<Widget> _answer(BuildContext context, DocumentRequest request) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return [
      const SizedBox(height: 20),
      Text(
        request.status == 'Returned'
            ? l10n.requestSendNew
            : l10n.requestSendHint,
        style: theme.textTheme.titleSmall,
      ),
      const SizedBox(height: 12),
      if (_error != null) ...[
        Text(
          _error!.describe(l10n),
          style: TextStyle(color: theme.colorScheme.error),
        ),
        const SizedBox(height: 8),
      ],
      if (_localError != null) ...[
        Text(_localError!, style: TextStyle(color: theme.colorScheme.error)),
        const SizedBox(height: 8),
      ],
      Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          OutlinedButton.icon(
            onPressed: _busy ? null : () => _fromCamera(),
            icon: const Icon(Icons.photo_camera_outlined),
            label: Text(l10n.requestTakePhoto),
          ),
          OutlinedButton.icon(
            onPressed: _busy ? null : () => _fromCamera(gallery: true),
            icon: const Icon(Icons.photo_library_outlined),
            label: Text(l10n.requestFromGallery),
          ),
          OutlinedButton.icon(
            onPressed: _busy ? null : _fromFiles,
            icon: const Icon(Icons.attach_file),
            label: Text(l10n.requestChooseFile),
          ),
        ],
      ),
      const SizedBox(height: 8),
      for (var i = 0; i < _picked.length; i++)
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.insert_drive_file_outlined),
          title: Text(
            _picked[i].name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          subtitle: Text(formatFileSize(_picked[i].sizeBytes)),
          trailing: IconButton(
            tooltip: l10n.requestRemoveFile,
            icon: const Icon(Icons.close),
            onPressed: _busy ? null : () => setState(() => _picked.removeAt(i)),
          ),
        ),
      const SizedBox(height: 12),
      FilledButton.icon(
        onPressed: _busy || _picked.isEmpty ? null : () => _send(request),
        icon: _busy
            ? const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : const Icon(Icons.send_outlined),
        label: Text(l10n.requestSend),
      ),
    ];
  }
}

class _EventRow extends StatelessWidget {
  const _EventRow({required this.event});

  final RequestEvent event;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final who = event.byEmployee ? l10n.requestByYou : l10n.requestByOffice;
    final what = switch (event.type) {
      'Created' => l10n.requestEventCreated,
      'Submitted' => l10n.requestEventSubmitted,
      'Accepted' => l10n.requestEventAccepted,
      'Returned' => l10n.requestEventReturned,
      'Cancelled' => l10n.requestEventCancelled,
      _ => event.type,
    };

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(top: 6, right: 10),
            child: Icon(Icons.circle, size: 8),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$what · $who'),
                Text(
                  formatDateTime(CompanyTime.wallClock(event.at)),
                  style: theme.textTheme.bodySmall,
                ),
                if (event.reason != null && event.reason!.isNotEmpty)
                  Text('„${event.reason}"', style: theme.textTheme.bodySmall),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
