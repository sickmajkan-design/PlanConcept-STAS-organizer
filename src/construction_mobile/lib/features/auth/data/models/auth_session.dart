import 'package:freezed_annotation/freezed_annotation.dart';

import 'auth_response.dart';
import 'user.dart';

part 'auth_session.freezed.dart';
part 'auth_session.g.dart';

/// The signed-in session as persisted in secure storage.
@freezed
abstract class AuthSession with _$AuthSession {
  const factory AuthSession({
    required String accessToken,
    required DateTime accessTokenExpiresAt,
    required String refreshToken,
    required DateTime refreshTokenExpiresAt,
    required User user,
  }) = _AuthSession;

  const AuthSession._();

  factory AuthSession.fromJson(Map<String, dynamic> json) =>
      _$AuthSessionFromJson(json);

  factory AuthSession.fromResponse(AuthResponse response) => AuthSession(
        accessToken: response.accessToken,
        accessTokenExpiresAt: response.accessTokenExpiresAt,
        refreshToken: response.refreshToken,
        refreshTokenExpiresAt: response.refreshTokenExpiresAt,
        user: response.user,
      );

  /// Treats the access token as expired shortly before it really is, so a
  /// request is never sent with a token that dies in flight.
  bool get isAccessTokenExpired => DateTime.now().toUtc().isAfter(
        accessTokenExpiresAt.toUtc().subtract(const Duration(seconds: 30)),
      );

  bool get isRefreshTokenExpired =>
      DateTime.now().toUtc().isAfter(refreshTokenExpiresAt.toUtc());
}
