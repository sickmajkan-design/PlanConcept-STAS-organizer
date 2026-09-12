import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_message.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/notification_repository.dart';
import 'device_token.dart';
import 'notification_deep_link.dart';
import 'notifications_controller.dart';
import 'pending_acknowledgments_controller.dart';

/// Runs in its own isolate when a push arrives while the app is backgrounded
/// or terminated — Android and iOS both spin one up fresh for this, so
/// nothing from the running app (Riverpod, the router, the previous Firebase
/// instance) is reachable here.
///
/// There is no local cache to update, so today this only has to exist: FCM
/// requires a registered background handler before it will hand the app a
/// [RemoteMessage] at all, and a plain top-level function is what a
/// background isolate can call — an instance method or a closure over
/// controller state cannot survive the isolate boundary. The OS already
/// draws the notification itself from the payload's `notification` block;
/// this only runs for `data`-only follow-up work, which there isn't any of
/// yet.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  if (Firebase.apps.isEmpty) {
    try {
      await Firebase.initializeApp();
    } catch (_) {
      // Same "not configured in this build" case _ensureFirebase already
      // handles in the foreground isolate — nothing to do here either.
      return;
    }
  }
}

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
  StreamSubscription<RemoteMessage>? _openedAppSubscription;

  @override
  PushState build() {
    final user = ref.watch(currentUserProvider);

    ref.onDispose(() {
      _tokenRefreshSubscription?.cancel();
      _foregroundSubscription?.cancel();
      _openedAppSubscription?.cancel();
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

      // Tapped from the system tray while backgrounded.
      _openedAppSubscription ??=
          FirebaseMessaging.onMessageOpenedApp.listen(_openFrom);

      // Tapped from the system tray while the app was not running at all —
      // the message that actually launched this cold start, if any.
      final initialMessage = await messaging.getInitialMessage();
      if (initialMessage != null) {
        _openFrom(initialMessage);
      }
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

  /// Navigates to whatever the tapped push points at, the same way tapping
  /// the equivalent row in the in-app inbox would — see [deepLinkForData].
  void _openFrom(RemoteMessage message) {
    final target = deepLinkForData(
      message.data['notificationType'] as String?,
      message.data,
      canViewDirectory: ref.read(currentUserProvider)?.canViewDirectory ?? false,
    );

    if (target != null) {
      ref.read(routerProvider).push(target);
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
