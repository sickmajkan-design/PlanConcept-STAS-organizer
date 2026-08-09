import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/widgets/status_chip.dart';
import '../data/models/tool.dart';
import '../data/tool_repository.dart';

/// Looks a tool up by its QR label. Available to every signed-in employee —
/// including a Worker without directory access — because the API's
/// `by-qr` endpoint is intentionally open to `AllEmployees`, so a crew
/// member can identify a tool on site without needing directory permissions.
///
/// The code is typed or pasted rather than scanned by camera: this build has
/// no camera dependency wired in, so it works with any code obtained from a
/// printed label, a barcode app, or by asking a foreman.
class ToolLookupScreen extends ConsumerStatefulWidget {
  const ToolLookupScreen({super.key});

  @override
  ConsumerState<ToolLookupScreen> createState() => _ToolLookupScreenState();
}

class _ToolLookupScreenState extends ConsumerState<ToolLookupScreen> {
  final _controller = TextEditingController();
  bool _isLoading = false;
  /// The failure itself, not a sentence about it, so it re-reads in the
  /// operator's language if they switch it while the message is on screen.
  ApiException? _failure;
  Tool? _result;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
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
    });

    try {
      final tool =
          await ref.read(toolRepositoryProvider).fetchToolByQrCode(code);
      if (!mounted) return;
      setState(() => _result = tool);
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _failure = exception);
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.toolLookUp)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              context.l10n.toolLookUpHint,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _controller,
              textInputAction: TextInputAction.search,
              autofocus: true,
              decoration: InputDecoration(
                labelText: context.l10n.toolQrCode,
                prefixIcon: const Icon(Icons.qr_code_outlined),
              ),
              onSubmitted: (_) => _lookup(),
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _isLoading ? null : _lookup,
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
            if (_result != null) _ToolResultCard(tool: _result!),
          ],
        ),
      ),
    );
  }
}

class _ToolResultCard extends StatelessWidget {
  const _ToolResultCard({required this.tool});

  final Tool tool;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    tool.name,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                StatusChip(status: tool.status, kind: EnumKind.toolStatus),
              ],
            ),
            const SizedBox(height: 8),
            if (tool.category != null) Text('Category: ${tool.category}'),
            if (tool.serialNumber != null)
              Text('Serial number: ${tool.serialNumber}'),
            const SizedBox(height: 8),
            Text(
              tool.assignedEmployeeName != null
                  ? 'Held by ${tool.assignedEmployeeName}'
                  : tool.assignedProjectName != null
                      ? 'On site at ${tool.assignedProjectName}'
                      : 'Not currently assigned',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
