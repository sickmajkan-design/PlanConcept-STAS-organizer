import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/attachment_repository.dart';
import '../data/models/attachment.dart';

/// Shows an attachment's contents.
///
/// Images render in place; anything else is downloaded through the
/// authenticated client and handed to whatever app the phone already has for
/// that file type (a PDF viewer, Office, …) — the bearer-token URL cannot be
/// given to an external app directly, so the bytes are fetched here first and
/// written to a private cache file that one is allowed to open.
class AttachmentPreviewScreen extends ConsumerWidget {
  const AttachmentPreviewScreen({super.key, required this.attachment});

  final Attachment attachment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: Text(attachment.fileName)),
      body: attachment.isImage
          ? _ImageBody(attachment: attachment)
          : _OpenExternallyBody(attachment: attachment),
    );
  }
}

class _OpenExternallyBody extends ConsumerStatefulWidget {
  const _OpenExternallyBody({required this.attachment});

  final Attachment attachment;

  @override
  ConsumerState<_OpenExternallyBody> createState() =>
      _OpenExternallyBodyState();
}

class _OpenExternallyBodyState extends ConsumerState<_OpenExternallyBody> {
  bool _busy = false;
  ApiException? _error;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.description_outlined,
              size: 48,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(height: 16),
            Text(widget.attachment.fileName, textAlign: TextAlign.center),
            const SizedBox(height: 20),
            if (_error != null) ...[
              Text(
                _error!.describe(l10n),
                textAlign: TextAlign.center,
                style: TextStyle(color: theme.colorScheme.error),
              ),
              const SizedBox(height: 12),
            ],
            FilledButton.icon(
              onPressed: _busy ? null : _open,
              icon: _busy
                  ? const SizedBox(
                      height: 16,
                      width: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.open_in_new),
              label: Text(
                _busy ? l10n.attachmentsOpeningExternally : l10n.attachmentsOpen,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _open() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final bytes = await ref
          .read(attachmentRepositoryProvider)
          .fetchContent(widget.attachment.id);

      final cacheDir = await getTemporaryDirectory();
      final file = File(p.join(cacheDir.path, widget.attachment.fileName));
      await file.writeAsBytes(bytes, flush: true);

      final result = await OpenFilex.open(file.path);

      if (result.type != ResultType.done && mounted) {
        messenger.showSnackBar(
          SnackBar(content: Text(l10n.attachmentsOpenExternalFailed)),
        );
      }
    } on ApiException catch (exception) {
      setState(() => _error = exception);
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}

class _ImageBody extends ConsumerStatefulWidget {
  const _ImageBody({required this.attachment});

  final Attachment attachment;

  @override
  ConsumerState<_ImageBody> createState() => _ImageBodyState();
}

class _ImageBodyState extends ConsumerState<_ImageBody> {
  late Future<Uint8List> _bytes;

  @override
  void initState() {
    super.initState();

    // Fetched through the authenticated client and held as bytes, because the
    // endpoint requires a bearer token and Image.network would request it
    // without one.
    _bytes = ref
        .read(attachmentRepositoryProvider)
        .fetchContent(widget.attachment.id);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;

    return FutureBuilder<Uint8List>(
      future: _bytes,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }

        if (snapshot.hasError) {
          final error = snapshot.error;

          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Text(
                error is ApiException
                    ? error.describe(l10n)
                    : l10n.attachmentsOpenFailed,
                textAlign: TextAlign.center,
              ),
            ),
          );
        }

        return InteractiveViewer(
          child: Center(child: Image.memory(snapshot.data!)),
        );
      },
    );
  }
}
