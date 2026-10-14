import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/l10n/api_failure_text.dart';
import '../../../core/l10n/app_locales.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_routes.dart';
import 'server_address_sheet.dart';
import '../../../core/validation/validators.dart';
import '../../../core/widgets/message_banner.dart';
import '../../../core/widgets/password_field.dart';
import 'auth_controller.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  final _codeController = TextEditingController();

  bool _submitting = false;
  ApiException? _error;

  /// Set once the password was accepted but a code is still owed. Lives only
  /// on this screen: leaving it drops the challenge and the password step has
  /// to be done again.
  String? _mfaToken;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _verifyCode() async {
    final token = _mfaToken;
    final code = _codeController.text.trim();

    if (_submitting || token == null || code.isEmpty) {
      return;
    }

    FocusScope.of(context).unfocus();
    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await ref
          .read(authControllerProvider.notifier)
          .completeMfa(mfaToken: token, code: code);
      // On success the router redirects away from this screen.
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() {
        _error = exception;
        _codeController.clear();
      });
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  void _backToPassword() {
    setState(() {
      _mfaToken = null;
      _error = null;
      _codeController.clear();
    });
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
      final challenge =
          await ref.read(authControllerProvider.notifier).signIn(
                email: _emailController.text,
                password: _passwordController.text,
              );

      if (challenge != null && mounted) {
        // The password was right and a code is still needed.
        setState(() => _mfaToken = challenge);
      }
      // Otherwise the router redirects away from this screen.
    } on ApiException catch (exception) {
      if (!mounted) return;
      setState(() => _error = exception);
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  Widget _buildCodeStep(
    BuildContext context,
    ThemeData theme,
    ApiException? error,
  ) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Icon(
                    Icons.verified_user_outlined,
                    size: 64,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(height: 20),
                  Text(
                    context.l10n.authMfaTitle,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    context.l10n.authMfaHint,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 32),
                  if (error != null) ...[
                    MessageBanner(message: error.describe(context.l10n)),
                    const SizedBox(height: 16),
                  ],
                  TextField(
                    controller: _codeController,
                    enabled: !_submitting,
                    autofocus: true,
                    autocorrect: false,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.oneTimeCode],
                    onSubmitted: (_) => _verifyCode(),
                    decoration: InputDecoration(
                      labelText: context.l10n.authMfaCode,
                      prefixIcon: const Icon(Icons.pin_outlined),
                    ),
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _submitting ? null : _verifyCode,
                    child: _submitting
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(strokeWidth: 2.5),
                          )
                        : Text(context.l10n.authMfaConfirm),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: _submitting ? null : _backToPassword,
                    child: Text(context.l10n.authMfaBack),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final error = _error;

    if (_mfaToken != null) {
      return _buildCodeStep(context, theme, error);
    }

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(
                      Icons.engineering,
                      size: 64,
                      color: theme.colorScheme.primary,
                    ),
                    const SizedBox(height: 20),
                    Text(
                      context.l10n.appName,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      context.l10n.authSignInSubtitle,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 32),
                    if (error != null) ...[
                      MessageBanner(message: error.describe(context.l10n)),
                      const SizedBox(height: 16),
                    ],
                    TextFormField(
                      controller: _emailController,
                      enabled: !_submitting,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autocorrect: false,
                      autofillHints: const [AutofillHints.username],
                      validator: (value) => Validators.email(value, context.l10n),
                      decoration: InputDecoration(
                        labelText: context.l10n.authEmail,
                        prefixIcon: const Icon(Icons.alternate_email),
                        errorText: error?.errorFor('email'),
                      ),
                    ),
                    const SizedBox(height: 16),
                    PasswordField(
                      controller: _passwordController,
                      label: context.l10n.authPassword,
                      enabled: !_submitting,
                      validator: (value) => Validators.notEmpty(
                        value,
                        context.l10n.authPassword,
                        context.l10n,
                      ),
                      errorText: error?.errorFor('password'),
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
                          : Text(context.l10n.commonSignIn),
                    ),
                    const SizedBox(height: 8),
                    TextButton(
                      onPressed: _submitting
                          ? null
                          : () => context.push(AppRoutes.forgotPassword),
                      child: Text(context.l10n.authForgotPassword),
                    ),
                    // The only place this belongs. An app pointed at the wrong
                    // server cannot sign in, so anything behind sign-in would
                    // be behind the very thing that is broken.
                    TextButton.icon(
                      onPressed: _submitting
                          ? null
                          : () => ServerAddressSheet.show(context),
                      icon: const Icon(Icons.dns_outlined, size: 18),
                      label: Text(context.l10n.serverAddressTitle),
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
