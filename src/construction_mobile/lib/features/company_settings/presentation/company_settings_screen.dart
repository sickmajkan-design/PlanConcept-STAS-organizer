import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../data/company_settings_repository.dart';
import '../data/models/company_settings.dart';

/// The platform's own company profile — name, address, tax details, contact
/// info and a logo. SuperAdmin-only, mirroring desktop's `RequireSuperAdmin`
/// route guard: even though the API itself serves a read to any signed-in
/// role, this screen is treated as a SuperAdmin-exclusive one end to end,
/// same as the desktop admin panel.
class CompanySettingsScreen extends ConsumerWidget {
  const CompanySettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final settings = ref.watch(companySettingsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.companySettingsTitle)),
      body: SafeArea(
        child: settings.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Text(
                error is ApiException ? error.describe(l10n) : l10n.errorUnknown,
                textAlign: TextAlign.center,
              ),
            ),
          ),
          data: (settings) => _CompanySettingsForm(existing: settings),
        ),
      ),
    );
  }
}

class _CompanySettingsForm extends ConsumerStatefulWidget {
  const _CompanySettingsForm({required this.existing});

  final CompanySettings existing;

  @override
  ConsumerState<_CompanySettingsForm> createState() =>
      _CompanySettingsFormState();
}

class _CompanySettingsFormState extends ConsumerState<_CompanySettingsForm> {
  late final _nameController = TextEditingController(text: widget.existing.name);
  late final _addressController =
      TextEditingController(text: widget.existing.address);
  late final _taxIdController = TextEditingController(text: widget.existing.taxId);
  late final _registrationController =
      TextEditingController(text: widget.existing.registrationNumber);
  late final _vatController = TextEditingController(text: widget.existing.vatNumber);
  late final _phoneController = TextEditingController(text: widget.existing.phone);
  late final _emailController = TextEditingController(text: widget.existing.email);
  late final _forwardEmailController = TextEditingController(
    text: widget.existing.weeklyReportsForwardEmail,
  );

  bool _busy = false;
  bool _logoBusy = false;
  ApiException? _error;
  ApiException? _logoError;
  Uint8List? _logoBytes;

  @override
  void initState() {
    super.initState();
    if (widget.existing.hasLogo) {
      _loadLogo();
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _addressController.dispose();
    _taxIdController.dispose();
    _registrationController.dispose();
    _vatController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _forwardEmailController.dispose();
    super.dispose();
  }

  Future<void> _loadLogo() async {
    try {
      final bytes = await ref.read(companySettingsRepositoryProvider).fetchLogo();
      if (mounted) setState(() => _logoBytes = bytes);
    } on ApiException {
      // A missing logo is not worth surfacing as an error banner here.
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  l10n.companySettingsLogo,
                  style: theme.textTheme.titleSmall,
                ),
                const SizedBox(height: 12),
                if (_logoError != null) ...[
                  Text(
                    _logoError!.describe(l10n),
                    style: TextStyle(color: theme.colorScheme.error),
                  ),
                  const SizedBox(height: 12),
                ],
                Row(
                  children: [
                    CircleAvatar(
                      radius: 32,
                      backgroundColor: theme.colorScheme.surfaceContainerHighest,
                      backgroundImage:
                          _logoBytes != null ? MemoryImage(_logoBytes!) : null,
                      child: _logoBytes == null
                          ? Text(
                              _nameController.text.trim().isEmpty
                                  ? '?'
                                  : _nameController.text.trim()[0].toUpperCase(),
                            )
                          : null,
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Wrap(
                        spacing: 8,
                        runSpacing: 4,
                        children: [
                          OutlinedButton(
                            onPressed: _logoBusy ? null : _pickLogo,
                            child: Text(l10n.companySettingsUploadLogo),
                          ),
                          if (_logoBytes != null)
                            TextButton(
                              onPressed: _logoBusy ? null : _removeLogo,
                              style: TextButton.styleFrom(
                                foregroundColor: theme.colorScheme.error,
                              ),
                              child: Text(l10n.companySettingsRemoveLogo),
                            ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (_error != null) ...[
                  Text(
                    _error!.describe(l10n),
                    style: TextStyle(color: theme.colorScheme.error),
                  ),
                  const SizedBox(height: 12),
                ],
                TextField(
                  controller: _nameController,
                  enabled: !_busy,
                  decoration: InputDecoration(labelText: l10n.companySettingsName),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _addressController,
                  enabled: !_busy,
                  decoration: InputDecoration(labelText: l10n.companySettingsAddress),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _taxIdController,
                  enabled: !_busy,
                  decoration: InputDecoration(labelText: l10n.companySettingsTaxId),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _registrationController,
                  enabled: !_busy,
                  decoration: InputDecoration(
                    labelText: l10n.companySettingsRegistrationNumber,
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _vatController,
                  enabled: !_busy,
                  decoration:
                      InputDecoration(labelText: l10n.companySettingsVatNumber),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _phoneController,
                  enabled: !_busy,
                  keyboardType: TextInputType.phone,
                  decoration: InputDecoration(labelText: l10n.companySettingsPhone),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _emailController,
                  enabled: !_busy,
                  keyboardType: TextInputType.emailAddress,
                  decoration: InputDecoration(labelText: l10n.companySettingsEmail),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _forwardEmailController,
                  enabled: !_busy,
                  keyboardType: TextInputType.emailAddress,
                  decoration: InputDecoration(
                    labelText: l10n.companySettingsWeeklyReportsForwardEmail,
                    helperText: l10n.companySettingsWeeklyReportsForwardEmailHint,
                  ),
                ),
                const SizedBox(height: 20),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: _busy || _nameController.text.trim().isEmpty
                        ? null
                        : _save,
                    child: _busy
                        ? const SizedBox(
                            height: 18,
                            width: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(l10n.commonSave),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _pickLogo() async {
    final l10n = context.l10n;

    final picked = await ImagePicker().pickImage(
      source: ImageSource.gallery,
      maxWidth: 1024,
      maxHeight: 1024,
      imageQuality: 90,
    );

    if (picked == null || !mounted) return;

    final length = await File(picked.path).length();

    if (length > 20 * 1024 * 1024) {
      setState(() => _logoError = null);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.companySettingsLogoTooLarge(20))),
      );
      return;
    }

    setState(() {
      _logoBusy = true;
      _logoError = null;
    });

    try {
      await ref.read(companySettingsRepositoryProvider).uploadLogo(
            filePath: picked.path,
            fileName: picked.name,
          );
      await _loadLogo();
      ref.invalidate(companySettingsProvider);
    } on ApiException catch (exception) {
      setState(() => _logoError = exception);
    } finally {
      if (mounted) setState(() => _logoBusy = false);
    }
  }

  Future<void> _removeLogo() async {
    setState(() {
      _logoBusy = true;
      _logoError = null;
    });

    try {
      await ref.read(companySettingsRepositoryProvider).removeLogo();
      setState(() => _logoBytes = null);
      ref.invalidate(companySettingsProvider);
    } on ApiException catch (exception) {
      setState(() => _logoError = exception);
    } finally {
      if (mounted) setState(() => _logoBusy = false);
    }
  }

  Future<void> _save() async {
    final l10n = context.l10n;
    final messenger = ScaffoldMessenger.of(context);

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      await ref.read(companySettingsRepositoryProvider).update(
            name: _nameController.text.trim(),
            address: _addressController.text.trim().isEmpty
                ? null
                : _addressController.text.trim(),
            taxId: _taxIdController.text.trim().isEmpty
                ? null
                : _taxIdController.text.trim(),
            registrationNumber: _registrationController.text.trim().isEmpty
                ? null
                : _registrationController.text.trim(),
            vatNumber: _vatController.text.trim().isEmpty
                ? null
                : _vatController.text.trim(),
            phone: _phoneController.text.trim().isEmpty
                ? null
                : _phoneController.text.trim(),
            email: _emailController.text.trim().isEmpty
                ? null
                : _emailController.text.trim(),
            weeklyReportsForwardEmail:
                _forwardEmailController.text.trim().isEmpty
                    ? null
                    : _forwardEmailController.text.trim(),
          );

      ref.invalidate(companySettingsProvider);
      messenger.showSnackBar(
        SnackBar(content: Text(l10n.companySettingsSaved)),
      );
    } on ApiException catch (exception) {
      setState(() => _error = exception);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }
}
