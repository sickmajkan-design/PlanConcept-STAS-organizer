import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
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

    return restored == null ? const Unauthenticated() : Authenticated(restored.user);
  }

  Future<void> signIn({
    required String email,
    required String password,
  }) async {
    final response = await ref.read(authRepositoryProvider).login(
          email: email.trim(),
          password: password,
        );

    await ref
        .read(authSessionManagerProvider)
        .start(AuthSession.fromResponse(response));

    state = AsyncData(Authenticated(response.user));
  }

  /// Revokes the refresh token server-side, then drops the local session.
  /// A failing call still signs the user out locally — the token expires on
  /// its own and the device must not stay stuck in a signed-in state.
  Future<void> signOut() async {
    final manager = ref.read(authSessionManagerProvider);
    final refreshToken = manager.session?.refreshToken;

    if (refreshToken != null) {
      try {
        await ref.read(authRepositoryProvider).logout(refreshToken);
      } on ApiException {
        // Ignored on purpose: local sign-out must always succeed.
      }
    }

    await manager.clear();
    state = const AsyncData(Unauthenticated());
  }

  /// Changes the password. The API revokes every refresh token as part of
  /// this, so the caller must follow up with [signOut] once the user has
  /// acknowledged that they need to sign in again.
  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) {
    return ref.read(authRepositoryProvider).changePassword(
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

final authControllerProvider =
    AsyncNotifierProvider<AuthController, AuthState>(AuthController.new);

/// The signed-in user, or `null` while unauthenticated or still restoring.
final currentUserProvider = Provider<User?>((ref) {
  final state = ref.watch(authControllerProvider).value;
  return state is Authenticated ? state.user : null;
});
