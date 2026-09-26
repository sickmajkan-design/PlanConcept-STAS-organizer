import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/formatting.dart';
import 'refunds_controller.dart';

Future<void> showRequestRefundSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _RequestRefundSheet(),
  );
}

class _RequestRefundSheet extends ConsumerStatefulWidget {
  const _RequestRefundSheet();

  @override
  ConsumerState<_RequestRefundSheet> createState() => _RequestRefundSheetState();
}

class _RequestRefundSheetState extends ConsumerState<_RequestRefundSheet> {
  final _amount = TextEditingController();
  final _currency = TextEditingController(text: 'EUR');
  final _description = TextEditingController();
  DateTime _date = DateTime.now();
  XFile? _receipt;
  bool _busy = false;

  @override
  void dispose() {
    _amount.dispose();
    _currency.dispose();
    _description.dispose();
    super.dispose();
  }

  double get _value => double.tryParse(_amount.text.trim().replaceAll(',', '.')) ?? 0;

  bool get _valid =>
      _value > 0 &&
      _description.text.trim().isNotEmpty &&
      RegExp(r'^[A-Za-z]{3}$').hasMatch(_currency.text.trim());

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

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
              l10n.refundsNew,
              style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 16),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  flex: 2,
                  child: TextField(
                    controller: _amount,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(labelText: l10n.refundAmount),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    controller: _currency,
                    maxLength: 3,
                    textCapitalization: TextCapitalization.characters,
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(labelText: l10n.refundCurrency),
                  ),
                ),
              ],
            ),
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickDate,
              icon: const Icon(Icons.calendar_today_outlined),
              label: Text('${l10n.refundExpenseDate}: ${formatDate(_date)}'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _description,
              maxLines: 3,
              maxLength: 1000,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                labelText: l10n.refundReason,
                helperText: l10n.refundReasonHint,
              ),
            ),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: _busy ? null : _pickReceipt,
              icon: const Icon(Icons.photo_camera_outlined),
              label: Text(_receipt == null ? l10n.refundAttachReceipt : _receipt!.name),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _busy || !_valid ? null : _send,
                child: Text(l10n.refundSend),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickDate() async {
    final today = DateTime.now();

    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: today.subtract(const Duration(days: 365)),
      lastDate: today,
    );

    if (picked != null && mounted) {
      setState(() => _date = picked);
    }
  }

  /// The camera first, the gallery when there is none.
  Future<void> _pickReceipt() async {
    final picker = ImagePicker();
    XFile? picked;

    try {
      picked = await picker.pickImage(source: ImageSource.camera, maxWidth: 1920, imageQuality: 85);
    } on Exception {
      picked = await picker.pickImage(source: ImageSource.gallery, maxWidth: 1920, imageQuality: 85);
    }

    if (picked != null && mounted) {
      setState(() => _receipt = picked);
    }
  }

  Future<void> _send() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    setState(() => _busy = true);

    try {
      await ref.read(refundsControllerProvider.notifier).create(
            amount: _value,
            currency: _currency.text.trim().toUpperCase(),
            expenseDate: _date,
            description: _description.text.trim(),
            receiptPath: _receipt?.path,
            receiptName: _receipt?.name,
          );

      navigator.pop();
      messenger.showSnackBar(SnackBar(content: Text(l10n.refundSent)));
    } on ApiException catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text(exception.describe(l10n))));

      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
