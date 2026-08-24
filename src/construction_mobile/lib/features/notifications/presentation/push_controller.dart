import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_message.dart';
import '../../../core/network/api_exception.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/notification_repository.dart';
import 'device_token.dart';
import 'notifications_controller.dart';
import 'pending_acknowledgments_controller.dart';

enum PushStatus {
  /// Nobody signed in.
  off,
  initializing,

  /// Firebase is not configured in this build (no google-services.json /
  /// GoogleService-Info.plist). The in-app inbox still works.
  unconfigured,
  permissionDenied,
  registered,
  error,
}

class PushState {
  const PushState({
    required this.status,
    this.token,
    this.message,
    this.failure,
    this.detail,
  });

  final PushStatus status;
  final String? token;
  /// A translatable app message; resolved by the widget that shows it.
  final AppMessage? message;

  /// An API failure, kept whole so the widget can say it in the reader's
  /// language.
  final ApiException? failure;

  /// Firebase's own text. This one really cannot be translated — it comes out
  /// of the SDK — so it is shown as it arrives, and only when there is nothing
  /// better.
  final String? detail;

  bool get isRegistered => status == PushStatus.registered;
}

/// Registers this device with Firebase Cloud Messaging and keeps the token in
/// sync with the API. Degrades quietly when Firebase is not configured — the
/// notification inbox is served by the API either way.
class PushController extends Notifier<PushState> {
  StreamSubscription<String>? _tokenRefreshSubscription;
  StreamSubscription<RemoteMessage>? _foregroundSubscription;

  @override
  PushState build() {
    final user = ref.watch(currentUserProvider);

    ref.onDispose(() {
      _tokenRefreshSubscription?.cancel();
      _foregroundSubscription?.cancel();
    });

    if (user == null) {
      return const PushState(status: PushStatus.off);
    }

    scheduleMicrotask(_initialize);
    return const PushState(status: PushStatus.initializing);
  }

  static String get _platform => switch (defaultTargetPlatform) {
        TargetPlatform.android => 'Android',
        TargetPlatform.iOS => 'Ios',
        _ => 'Web',
      };

  Future<void> _initialize() async {
    if (!await _ensureFirebase()) {
      return;
    }

    final messaging = FirebaseMessaging.instance;

    final settings = await messaging.requestPermission();

    if (settings.authorizationStatus == AuthorizationStatus.denied) {
      state = const PushState(
        status: PushStatus.permissionDenied,
        message: AppMessage.notificationsDisabled,
      );
      return;
    }

    try {
      final token = await messaging.getToken();

      if (token == null) {
        state = const PushState(
          status: PushStatus.error,
          message: AppMessage.notificationsTokenFailed,
        );
        return;
      }

      await _register(token);

      // A token can be rotated by the OS at any time.
      _tokenRefreshSubscription ??=
          messaging.onTokenRefresh.listen((refreshed) => _register(refreshed));

      // A push arriving while the app is open should update the inbox badge.
      _foregroundSubscription ??= FirebaseMessaging.onMessage.listen((_) {
        ref.invalidate(unreadNotificationCountProvider);
        ref.invalidate(notificationsControllerProvider);
        ref.invalidate(pendingAcknowledgmentsProvider);
      });
    } on ApiException catch (exception) {
      state = PushState(status: PushStatus.error, failure: exception);
    } on FirebaseException catch (exception) {
      state = PushState(
        status: PushStatus.error,
        message: exception.message == null ? AppMessage.notificationsFirebaseFailed : null,
        detail: exception.message,
      );
    }
  }

  Future<bool> _ensureFirebase() async {
    if (Firebase.apps.isNotEmpty) {
      return true;
    }

    try {
      await Firebase.initializeApp();
      return true;
    } catch (_) {
      // No Firebase configuration bundled with this build.
      state = const PushState(
        status: PushStatus.unconfigured,
        message: AppMessage.notificationsNotConfigured,
      );
      return false;
    }
  }

  Future<void> _register(String token) async {
    await ref.read(notificationRepositoryProvider).registerDeviceToken(
          token: token,
          platform: _platform,
        );

    // Published where sign-out can reach it without depending on this
    // controller, which depends on who is signed in. See [deviceTokenProvider].
    ref.read(deviceTokenProvider.notifier).remember(token);

    state = PushState(status: PushStatus.registered, token: token);
  }
}

final pushControllerProvider = NotifierProvider<PushController, PushState>(
  PushController.new,
);
