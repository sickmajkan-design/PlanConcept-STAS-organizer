import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/server_address.dart';
import '../../../core/l10n/app_locales.dart';

/// Where the phone is told which server it belongs to.
///
/// Reached from the sign-in screen rather than a settings page, because that
/// is the only moment it matters: an app that cannot reach its server cannot
/// get past sign-in, so a settings screen behind the sign-in screen would be
/// behind the very thing that is broken.
///
/// Shown as a sheet rather than a route so it can be opened, used and
/// dismissed without leaving the screen the operator is stuck on.
class ServerAddressSheet extends ConsumerStatefulWidget {
  const ServerAddressSheet({super.key});

  static Future<void> show(BuildContext context) => showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        builder: (_) => const ServerAddressSheet(),
      );

  @override
  ConsumerState<ServerAddressSheet> createState() => _ServerAddressSheetState();
}

class _ServerAddressSheetState extends ConsumerState<ServerAddressSheet> {
  late final TextEditingController _controller =
      TextEditingController(text: ref.read(serverAddressProvider));

  String? _error;
  bool _saving = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
    });

    final accepted =
        await ref.read(serverAddressProvider.notifier).set(_controller.text);

    if (!mounted) {
      return;
    }

    if (!accepted) {
      setState(() {
        _saving = false;
        _error = context.l10n.serverAddressInvalid;
      });
      return;
    }

    Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      // Above the keyboard, which covers this field on almost every phone.
      padding: EdgeInsets.only(
        left: 24,
        right: 24,
        top: 24,
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            context.l10n.serverAddressTitle,
            style: theme.textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Text(
            context.l10n.serverAddressHint,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 16),
          TextField(
            controller: _controller,
            enabled: !_saving,
            keyboardType: TextInputType.url,
            autocorrect: false,
            decoration: InputDecoration(
              labelText: context.l10n.serverAddressLabel,
              hintText: 'https://organizer.example.com',
              prefixIcon: const Icon(Icons.dns_outlined),
              errorText: _error,
            ),
            onSubmitted: (_) => _save(),
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _saving ? null : _save,
            child: Text(context.l10n.commonSave),
          ),
        ],
      ),
    );
  }
}
