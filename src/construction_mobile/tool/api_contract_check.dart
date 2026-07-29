// Developer tool: runs the app's own models against a live API instance to
// prove the client/server contract still matches. Not shipped with the app.
//
//   dart run tool/api_contract_check.dart
//
// Expects the API on http://localhost:5000 with the development seed data.
import 'dart:convert';
import 'dart:io';

import 'package:construction_mobile/features/auth/data/models/auth_response.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';

const _baseUrl = 'http://localhost:5000';

Future<(int, Map<String, dynamic>?)> _post(
  String path,
  Map<String, dynamic> body, {
  String? bearer,
}) async {
  final client = HttpClient();
  final request = await client.postUrl(Uri.parse('$_baseUrl$path'));
  request.headers.contentType = ContentType.json;
  if (bearer != null) {
    request.headers.set('Authorization', 'Bearer $bearer');
  }
  request.write(jsonEncode(body));

  final response = await request.close();
  final text = await response.transform(utf8.decoder).join();
  client.close();

  final decoded = text.isEmpty ? null : jsonDecode(text);
  return (response.statusCode, decoded is Map<String, dynamic> ? decoded : null);
}

Future<(int, Map<String, dynamic>?)> _get(String path, String bearer) async {
  final client = HttpClient();
  final request = await client.getUrl(Uri.parse('$_baseUrl$path'));
  request.headers.set('Authorization', 'Bearer $bearer');

  final response = await request.close();
  final text = await response.transform(utf8.decoder).join();
  client.close();

  final decoded = text.isEmpty ? null : jsonDecode(text);
  return (response.statusCode, decoded is Map<String, dynamic> ? decoded : null);
}

Future<void> main() async {
  var failures = 0;

  void check(String label, bool condition, [Object? detail]) {
    stdout.writeln('${condition ? "PASS" : "FAIL"}  $label'
        '${detail == null ? "" : "  -> $detail"}');
    if (!condition) failures++;
  }

  // 1. Login: the API's AuthResponse must parse into the app's model.
  final (loginStatus, loginBody) = await _post('/api/auth/login', {
    'email': 'ivan@construction.local',
    'password': 'Changed123!',
  });
  check('login returns 200', loginStatus == 200, loginStatus);

  final auth = AuthResponse.fromJson(loginBody!);
  check('access token parsed', auth.accessToken.isNotEmpty);
  check('refresh token parsed', auth.refreshToken.isNotEmpty);
  check(
    'token expiry parsed as a future UTC instant',
    auth.accessTokenExpiresAt.isAfter(DateTime.now().toUtc()),
    auth.accessTokenExpiresAt.toIso8601String(),
  );
  check('user parsed', auth.user.email == 'ivan@construction.local');
  check('display name derived from employee', auth.user.displayName == 'Ivan Horvat',
      auth.user.displayName);
  check('employee link detected', auth.user.isEmployee);
  check('role carried through', auth.user.role == 'Worker', auth.user.role);

  // 2. Session persistence: the JSON written to secure storage must round-trip.
  final session = AuthSession.fromResponse(auth);
  final restored = AuthSession.fromJson(
    jsonDecode(jsonEncode(session.toJson())) as Map<String, dynamic>,
  );
  check('session survives the secure-storage round trip', restored == session);
  check('restored token is not considered expired', !restored.isAccessTokenExpired);

  // 3. /me: the same User model must parse the profile endpoint.
  final (meStatus, meBody) = await _get('/api/auth/me', auth.accessToken);
  check('GET /me returns 200', meStatus == 200, meStatus);
  final me = User.fromJson(meBody!);
  check('/me parses into User', me.id == auth.user.id);

  // 4. Refresh rotation: the refresh response uses the same shape.
  final (refreshStatus, refreshBody) = await _post('/api/auth/refresh', {
    'refreshToken': auth.refreshToken,
  });
  check('refresh returns 200', refreshStatus == 200, refreshStatus);
  final refreshed = AuthResponse.fromJson(refreshBody!);
  check('rotation issued a different refresh token',
      refreshed.refreshToken != auth.refreshToken);

  // 5. Validation errors: the shape the ApiException parser depends on.
  final (badStatus, badBody) = await _post('/api/auth/login', {
    'email': 'not-an-email',
    'password': '',
  });
  check('invalid login returns 400', badStatus == 400, badStatus);
  check('field errors present under "errors"', badBody?['errors'] != null);
  check('field names are PascalCase as the client assumes',
      (badBody?['errors'] as Map).containsKey('Email'),
      (badBody?['errors'] as Map).keys.toList());

  // 6. Unauthorized: problem-details "detail" is what the client shows.
  final (unauthorizedStatus, unauthorizedBody) = await _post('/api/auth/login', {
    'email': 'ivan@construction.local',
    'password': 'WrongPassword1',
  });
  check('wrong password returns 401', unauthorizedStatus == 401, unauthorizedStatus);
  check('problem details carry a detail message',
      (unauthorizedBody?['detail'] as String?)?.isNotEmpty ?? false,
      unauthorizedBody?['detail']);

  // 7. Logout revokes the rotated refresh token.
  final (logoutStatus, _) = await _post(
    '/api/auth/logout',
    {'refreshToken': refreshed.refreshToken},
    bearer: refreshed.accessToken,
  );
  check('logout returns 204', logoutStatus == 204, logoutStatus);

  final (replayStatus, _) = await _post('/api/auth/refresh', {
    'refreshToken': refreshed.refreshToken,
  });
  check('revoked refresh token is rejected', replayStatus == 401, replayStatus);

  stdout.writeln(failures == 0
      ? '\nAll contract checks passed.'
      : '\n$failures contract check(s) FAILED.');
  exit(failures == 0 ? 0 : 1);
}
