import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/auth_response.dart';
import 'models/user.dart';

/// What the password step of a sign-in led to.
///
/// Not an [AuthResponse] directly: an account with a second factor gets a
/// challenge back instead of tokens, and the token fields of that reply are
/// null, which a model with required tokens could not even parse.
sealed class LoginResult {
  const LoginResult();

  factory LoginResult.fromJson(Map<String, dynamic> json) {
    final token = json['mfaToken'];

    if (json['mfaRequired'] == true && token is String && token.isNotEmpty) {
      return LoginMfaRequired(token);
    }

    return LoginSignedIn(AuthResponse.fromJson(json));
  }
}

/// The password was enough: here are the tokens.
final class LoginSignedIn extends LoginResult {
  const LoginSignedIn(this.response);

  final AuthResponse response;
}

/// The password was right but a code from the authenticator (or a recovery
/// code) is still needed; [mfaToken] goes back with it.
final class LoginMfaRequired extends LoginResult {
  const LoginMfaRequired(this.mfaToken);

  final String mfaToken;
}

/// Talks to the API's authentication endpoints. Every Dio failure leaves
/// this class as an [ApiException], so callers never handle transport types.
class AuthRepository extends ApiRepository {
  const AuthRepository(super.dio);

  Future<LoginResult> login({
    required String email,
    required String password,
  }) {
    return postJson(
      '/api/v1/auth/login',
      LoginResult.fromJson,
      data: {'email': email, 'password': password},
    );
  }

  /// Second step of a sign-in: the challenge from [login] and a code.
  Future<AuthResponse> verifyMfa({
    required String mfaToken,
    required String code,
  }) {
    return postJson(
      '/api/v1/auth/mfa/verify',
      AuthResponse.fromJson,
      data: {'mfaToken': mfaToken, 'code': code.trim()},
    );
  }

  /// Revokes [refreshToken] server-side.
  ///
  /// [accessToken] is passed explicitly because sign-out ends the local
  /// session before making this call — there is nothing left for the auth
  /// interceptor to attach by the time it runs.
  Future<void> logout(String refreshToken, {String? accessToken}) {
    return postVoid(
      '/api/v1/auth/logout',
      data: {'refreshToken': refreshToken},
      accessToken: accessToken,
    );
  }

  Future<User> currentUser() {
    return getJson('/api/v1/auth/me', User.fromJson);
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) {
    return postVoid(
      '/api/v1/auth/change-password',
      data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
  }

  /// Records which language this account reads the app in, so a push
  /// notification sent before the app is open can be rendered in it too —
  /// the in-app inbox already renders client-side and does not need this.
  Future<void> updatePreferredLanguage(String languageCode) {
    return putVoid(
      '/api/v1/auth/me/language',
      data: {'languageCode': languageCode},
    );
  }

  Future<void> requestPasswordReset(String email) {
    return postVoid('/api/v1/auth/forgot-password', data: {'email': email});
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) {
    return postVoid(
      '/api/v1/auth/reset-password',
      data: {
        'email': email,
        'token': token,
        'newPassword': newPassword,
      },
    );
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(apiClientProvider));
});
