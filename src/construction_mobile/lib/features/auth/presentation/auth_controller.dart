import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../../notifications/data/notification_repository.dart';
import '../../notifications/presentation/device_token.dart';
import '../data/auth_repository.dart';
import '../data/models/auth_session.dart';
import '../data/models/user.dart';

/// Who is signed in, as far as the app is concerned.
sealed class AuthState {
  const AuthState();
}

final class Authenticated extends AuthState {
  const Authenticated(this.user);

  final User user;
}

final class Unauthenticated extends AuthState {
  const Unauthenticated();
}

/// Owns the sign-in lifecycle. Commands throw [ApiException] on failure so
/// screens can show a precise message; the controller's own state only
/// changes when an operation actually succeeds.
class AuthController extends AsyncNotifier<AuthState> {
  @override
  Future<AuthState> build() async {
    final manager = ref.watch(authSessionManagerProvider);

    final restored = await manager.restore();

    // Subscribed after restoring so a forced sign-out (failed refresh)
    // reaches us, without emitting while this build is still running.
    final subscription = manager.changes.listen((session) {
      if (session == null) {
        state = const AsyncData(Unauthenticated());
      }
    });

    ref.onDispose(subscription.cancel);

    return restored == null
        ? const Unauthenticated()
        : Authenticated(restored.user);
  }

  Future<void> signIn({required String email, required String password}) async {
    final response = await ref
        .read(authRepositoryProvider)
        .login(email: email.trim(), password: password);

    await ref
        .read(authSessionManagerProvider)
        .start(AuthSession.fromResponse(response));

    state = AsyncData(Authenticated(response.user));
  }

  /// Signs the user out of this device, then tells the server.
  ///
  /// In that order, and the order is the whole point. Sign-out used to make
  /// two API calls first and clear the session afterwards, which meant that on
  /// a phone that could not reach the server — inside a building, in a
  /// basement, on a site with one bar — pressing the button did nothing
  /// visible for up to thirty seconds, and pressing it with no route to the
  /// server at all did nothing visible ever.
  ///
  /// Nothing below the local sign-out can fail it, and nothing above it takes
  /// any time: the session is gone and the app is on the sign-in screen within
  /// a frame. Revoking the refresh token server-side matters, but it is not
  /// worth making somebody stand there for, and it is not the app's only
  /// protection — the token expires on its own, and it left the device with
  /// the session that carried it.
  Future<void> signOut() async {
    final manager = ref.read(authSessionManagerProvider);

    // Captured before the app forgets them, because that is what the calls
    // below need to authenticate with.
    final session = manager.session;
    final deviceToken = ref.read(deviceTokenProvider);

    ref.read(deviceTokenProvider.notifier).forget();

    await manager.clear();
    state = const AsyncData(Unauthenticated());

    if (session != null) {
      unawaited(_revoke(session, deviceToken));
    }
  }

  /// Asks the API to forget the session and this handset's push token.
  ///
  /// Explicitly credentialed, because by the time this runs the session
  /// manager has nothing left to attach: the tokens travelled here as
  /// arguments from the moment before the sign-out.
  Future<void> _revoke(AuthSession session, String? deviceToken) async {
    try {
      // The captured access token may have expired while the app sat unopened
      // — a worker who unlocks the phone after lunch and signs out straight
      // away is holding one that died an hour ago. Trading the refresh token
      // for a live one is what keeps the revoke from being refused, and the
      // rotation it performs already invalidates the token captured above.
      final live = session.isAccessTokenExpired
          ? await ref.read(authSessionManagerProvider).refreshDetached(session)
          : session;

      if (live == null) {
        // Nothing left to revoke with, because there is nothing left to
        // revoke: the session had already expired on its own.
        return;
      }

      if (deviceToken != null) {
        await ref
            .read(notificationRepositoryProvider)
            .unregisterDeviceToken(deviceToken, accessToken: live.accessToken);
      }

      await ref
          .read(authRepositoryProvider)
          .logout(live.refreshToken, accessToken: live.accessToken);
    } catch (_) {
      // Offline, refused, or an access token that expired minutes ago. The
      // refresh token expires on its own and the server prunes device tokens
      // that FCM reports as dead, so there is nothing here worth retrying and
      // nothing worth telling somebody who has already left the app.
    }
  }

  /// Changes the password. The API revokes every refresh token as part of
  /// this, so the caller must follow up with [signOut] once the user has
  /// acknowledged that they need to sign in again.
  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) {
    return ref
        .read(authRepositoryProvider)
        .changePassword(
          currentPassword: currentPassword,
          newPassword: newPassword,
        );
  }

  Future<void> requestPasswordReset(String email) {
    return ref.read(authRepositoryProvider).requestPasswordReset(email.trim());
  }

  /// Re-reads the profile from the API (e.g. after an admin changed a role).
  Future<void> refreshProfile() async {
    final user = await ref.read(authRepositoryProvider).currentUser();

    await ref.read(authSessionManagerProvider).updateUser(user);
    state = AsyncData(Authenticated(user));
  }
}

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

/// The signed-in user, or `null` while unauthenticated or still restoring.
final currentUserProvider = Provider<User?>((ref) {
  final state = ref.watch(authControllerProvider).value;
  return state is Authenticated ? state.user : null;
});
