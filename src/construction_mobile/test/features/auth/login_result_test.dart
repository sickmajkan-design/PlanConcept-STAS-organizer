import 'package:construction_mobile/features/auth/data/auth_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('LoginResult.fromJson', () {
    test('reads a challenge when a second factor is still owed', () {
      // The API leaves the token fields null in this case; a model with
      // required tokens could not even be built from it.
      final result = LoginResult.fromJson(<String, dynamic>{
        'accessToken': null,
        'accessTokenExpiresAt': '0001-01-01T00:00:00',
        'refreshToken': null,
        'refreshTokenExpiresAt': '0001-01-01T00:00:00',
        'user': null,
        'mfaRequired': true,
        'mfaToken': 'abc123',
        'mfaEnrollmentRequired': false,
      });

      expect(result, isA<LoginMfaRequired>());
      expect((result as LoginMfaRequired).mfaToken, 'abc123');
    });

    test('reads the tokens when the password was enough', () {
      final result = LoginResult.fromJson(<String, dynamic>{
        'accessToken': 'access',
        'accessTokenExpiresAt': '2026-10-14T20:00:00Z',
        'refreshToken': 'refresh',
        'refreshTokenExpiresAt': '2026-10-21T20:00:00Z',
        'user': <String, dynamic>{
          'id': 'u1',
          'email': 'ana@example.test',
          'role': 'Worker',
        },
        'mfaRequired': false,
        'mfaToken': null,
      });

      expect(result, isA<LoginSignedIn>());
      final response = (result as LoginSignedIn).response;
      expect(response.accessToken, 'access');
      expect(response.refreshToken, 'refresh');
      expect(response.user.email, 'ana@example.test');
    });

    test('an older server that does not know the field still signs in', () {
      final result = LoginResult.fromJson(<String, dynamic>{
        'accessToken': 'access',
        'accessTokenExpiresAt': '2026-10-14T20:00:00Z',
        'refreshToken': 'refresh',
        'refreshTokenExpiresAt': '2026-10-21T20:00:00Z',
        'user': <String, dynamic>{
          'id': 'u1',
          'email': 'ana@example.test',
          'role': 'Worker',
        },
      });

      expect(result, isA<LoginSignedIn>());
    });

    test('a challenge flag without a token is not treated as a challenge', () {
      expect(
        () => LoginResult.fromJson(<String, dynamic>{
          'accessToken': null,
          'accessTokenExpiresAt': '0001-01-01T00:00:00',
          'refreshToken': null,
          'refreshTokenExpiresAt': '0001-01-01T00:00:00',
          'user': null,
          'mfaRequired': true,
          'mfaToken': '',
        }),
        throwsA(anything),
      );
    });
  });
}
