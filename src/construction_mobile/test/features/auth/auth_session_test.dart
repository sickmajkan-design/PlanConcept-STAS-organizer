import 'package:construction_mobile/features/auth/data/models/auth_response.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:flutter_test/flutter_test.dart';

const _userJson = {
  'id': '019fad65-d635-76f2-880f-d8d25aea67d0',
  'email': 'ivan@construction.local',
  'role': 'Foreman',
  'employeeId': '019fad73-e894-791b-a6c3-715bddf61164',
  'firstName': 'Ivan',
  'lastName': 'Horvat',
  'lastLoginAt': '2026-07-29T10:22:59.550342Z',
};

AuthSession _sessionExpiringIn(Duration accessTokenLifetime) {
  final now = DateTime.now().toUtc();

  return AuthSession(
    accessToken: 'access',
    accessTokenExpiresAt: now.add(accessTokenLifetime),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: now.add(const Duration(days: 7)),
    user: User.fromJson(_userJson),
  );
}

void main() {
  group('User', () {
    test('parses the API payload', () {
      final user = User.fromJson(_userJson);

      expect(user.email, 'ivan@construction.local');
      expect(user.role, 'Foreman');
      expect(user.lastLoginAt, isNotNull);
    });

    test('shows the full name when the account is linked to an employee', () {
      expect(User.fromJson(_userJson).displayName, 'Ivan Horvat');
    });

    test('falls back to the email when no employee is linked', () {
      final user = User.fromJson({
        ..._userJson,
        'employeeId': null,
        'firstName': null,
        'lastName': null,
      });

      expect(user.displayName, 'ivan@construction.local');
      expect(user.isEmployee, isFalse);
    });

    test('only employee-linked accounts may report positions', () {
      expect(User.fromJson(_userJson).isEmployee, isTrue);
    });
  });

  group('AuthSession', () {
    test('is built from the login response', () {
      final response = AuthResponse.fromJson({
        'accessToken': 'access-token',
        'accessTokenExpiresAt': '2026-07-29T11:00:00Z',
        'refreshToken': 'refresh-token',
        'refreshTokenExpiresAt': '2026-08-05T10:45:00Z',
        'user': _userJson,
      });

      final session = AuthSession.fromResponse(response);

      expect(session.accessToken, 'access-token');
      expect(session.refreshToken, 'refresh-token');
      expect(session.user.displayName, 'Ivan Horvat');
    });

    test('survives a JSON round trip through secure storage', () {
      final session = _sessionExpiringIn(const Duration(minutes: 15));

      expect(AuthSession.fromJson(session.toJson()), session);
    });

    test('treats a comfortably fresh access token as usable', () {
      expect(
        _sessionExpiringIn(const Duration(minutes: 15)).isAccessTokenExpired,
        isFalse,
      );
    });

    test('treats an already-expired access token as expired', () {
      expect(
        _sessionExpiringIn(const Duration(minutes: -1)).isAccessTokenExpired,
        isTrue,
      );
    });

    test('expires a token that would die mid-flight', () {
      // Inside the 30-second safety margin: refresh before sending.
      expect(
        _sessionExpiringIn(const Duration(seconds: 10)).isAccessTokenExpired,
        isTrue,
      );
    });

    test('detects an expired refresh token', () {
      final session = _sessionExpiringIn(const Duration(minutes: 5)).copyWith(
        refreshTokenExpiresAt:
            DateTime.now().toUtc().subtract(const Duration(days: 1)),
      );

      expect(session.isRefreshTokenExpired, isTrue);
    });
  });
}
