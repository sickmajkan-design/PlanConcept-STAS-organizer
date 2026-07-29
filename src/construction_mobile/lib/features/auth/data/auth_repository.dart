import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/network_providers.dart';
import 'models/auth_response.dart';
import 'models/user.dart';

/// Talks to the API's authentication endpoints. Every Dio failure leaves
/// this class as an [ApiException], so callers never handle transport types.
class AuthRepository {
  AuthRepository(this._dio);

  final Dio _dio;

  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    return _guard(() async {
      final response = await _dio.post<Map<String, dynamic>>(
        '/api/auth/login',
        data: {'email': email, 'password': password},
      );

      return AuthResponse.fromJson(response.data!);
    });
  }

  Future<void> logout(String refreshToken) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/auth/logout',
        data: {'refreshToken': refreshToken},
      );
    });
  }

  Future<User> currentUser() async {
    return _guard(() async {
      final response = await _dio.get<Map<String, dynamic>>('/api/auth/me');
      return User.fromJson(response.data!);
    });
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/auth/change-password',
        data: {
          'currentPassword': currentPassword,
          'newPassword': newPassword,
        },
      );
    });
  }

  Future<void> requestPasswordReset(String email) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/auth/forgot-password',
        data: {'email': email},
      );
    });
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) async {
    return _guard(() async {
      await _dio.post<void>(
        '/api/auth/reset-password',
        data: {
          'email': email,
          'token': token,
          'newPassword': newPassword,
        },
      );
    });
  }

  Future<T> _guard<T>(Future<T> Function() request) async {
    try {
      return await request();
    } on DioException catch (exception) {
      throw ApiException.fromDioException(exception);
    }
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(apiClientProvider));
});
