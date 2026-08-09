import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/attachment_repository.dart';
import '../data/models/attachment.dart';

/// Shows an attachment's contents.
///
/// Images render; anything else reports that the phone cannot open it here.
/// Opening a PDF would mean either an external viewer — which the token-bound
/// URL cannot be handed to — or a rendering dependency, and neither is worth
/// carrying for a screen a foreman uses from the office web app instead.
class AttachmentPreviewScreen extends ConsumerWidget {
  const AttachmentPreviewScreen({super.key, required this.attachment});

  final Attachment attachment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;

    return Scaffold(
      appBar: AppBar(title: Text(attachment.fileName)),
      body: attachment.isImage
          ? _ImageBody(attachment: attachment)
          : Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  l10n.attachmentsOpenFailed,
                  textAlign: TextAlign.center,
                ),
              ),
            ),
    );
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
