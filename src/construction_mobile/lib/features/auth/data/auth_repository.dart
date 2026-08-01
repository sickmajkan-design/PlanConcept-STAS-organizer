import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/auth_response.dart';
import 'models/user.dart';

/// Talks to the API's authentication endpoints. Every Dio failure leaves
/// this class as an [ApiException], so callers never handle transport types.
class AuthRepository extends ApiRepository {
  const AuthRepository(super.dio);

  Future<AuthResponse> login({
    required String email,
    required String password,
  }) {
    return postJson(
      '/api/auth/login',
      AuthResponse.fromJson,
      data: {'email': email, 'password': password},
    );
  }

  Future<void> logout(String refreshToken) {
    return postVoid('/api/auth/logout', data: {'refreshToken': refreshToken});
  }

  Future<User> currentUser() {
    return getJson('/api/auth/me', User.fromJson);
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) {
    return postVoid(
      '/api/auth/change-password',
      data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
  }

  Future<void> requestPasswordReset(String email) {
    return postVoid('/api/auth/forgot-password', data: {'email': email});
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) {
    return postVoid(
      '/api/auth/reset-password',
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
