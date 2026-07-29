import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/validation/validators.dart';
import '../../../core/widgets/message_banner.dart';
import '../../../core/widgets/password_field.dart';
import 'auth_controller.dart';

class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() =>
      _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentController = TextEditingController();
  final _newController = TextEditingController();
  final _confirmController = TextEditingController();

  bool _submitting = false;
  ApiException? _error;

  @override
  void dispose() {
    _currentController.dispose();
    _newController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting || !(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    FocusScope.of(context).unfocus();
    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await ref.read(authControllerProvider.notifier).changePassword(
            currentPassword: _currentController.text,
            newPassword: _newController.text,
          );

      if (!mounted) return;
      await _confirmSignOut();
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _error = exception);
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  /// The API revokes every session when the password changes, so the user has
  /// to sign in again. Shown as a modal so the message cannot be missed.
  Future<void> _confirmSignOut() async {
    await showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => AlertDialog(
        icon: const Icon(Icons.check_circle_outline),
        title: const Text('Password changed'),
        content: const Text(
          'Your password has been updated. For security, all your signed-in '
          'devices were signed out. Please sign in again with the new password.',
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Sign in again'),
          ),
        ],
      ),
    );

    await ref.read(authControllerProvider.notifier).signOut();
  }

  String? _validateConfirmation(String? value) {
    if ((value ?? '').isEmpty) {
      return 'Confirm the new password.';
    }

    if (value != _newController.text) {
      return 'The passwords do not match.';
    }

    return null;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final error = _error;

    return Scaffold(
      appBar: AppBar(title: const Text('Change password')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Use at least 8 characters with an upper-case letter, a '
                      'lower-case letter and a digit.',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 24),
                    if (error != null) ...[
                      MessageBanner(message: error.message),
                      const SizedBox(height: 16),
                    ],
                    PasswordField(
                      controller: _currentController,
                      label: 'Current password',
                      enabled: !_submitting,
                      textInputAction: TextInputAction.next,
                      validator: (value) =>
                          Validators.notEmpty(value, 'Current password'),
                      errorText: error?.errorFor('currentPassword'),
                    ),
                    const SizedBox(height: 16),
                    PasswordField(
                      controller: _newController,
                      label: 'New password',
                      enabled: !_submitting,
                      textInputAction: TextInputAction.next,
                      validator: Validators.strongPassword,
                      errorText: error?.errorFor('newPassword'),
                    ),
                    const SizedBox(height: 16),
                    PasswordField(
                      controller: _confirmController,
                      label: 'Confirm new password',
                      enabled: !_submitting,
                      validator: _validateConfirmation,
                      onSubmitted: _submit,
                    ),
                    const SizedBox(height: 24),
                    FilledButton(
                      onPressed: _submitting ? null : _submit,
                      child: _submitting
                          ? const SizedBox(
                              width: 22,
                              height: 22,
                              child: CircularProgressIndicator(strokeWidth: 2.5),
                            )
                          : const Text('Change password'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
