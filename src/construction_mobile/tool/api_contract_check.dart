// Developer tool: runs the app's own models against a live API instance to
// prove the client/server contract still matches. Not shipped with the app.
//
//   dart run tool/api_contract_check.dart
//
// Expects the API on http://localhost:5000 with the development seed data.
import 'dart:convert';
import 'dart:io';

import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/features/auth/data/models/auth_response.dart';
import 'package:construction_mobile/features/auth/data/models/auth_session.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/employees/data/models/employee.dart';
import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/projects/data/models/project.dart';

const _baseUrl = 'http://localhost:5000';

int _failures = 0;

void check(String label, bool condition, [Object? detail]) {
  stdout.writeln('${condition ? "PASS" : "FAIL"}  $label'
      '${detail == null ? "" : "  -> $detail"}');
  if (!condition) _failures++;
}

void section(String title) => stdout.writeln('\n— $title');

class _Response {
  const _Response(this.statusCode, this.body);

  final int statusCode;
  final Object? body;

  Map<String, dynamic>? get map =>
      body is Map<String, dynamic> ? body as Map<String, dynamic> : null;
}

Future<_Response> _send(
  String method,
  String path, {
  Object? body,
  String? bearer,
}) async {
  final client = HttpClient();
  final request = await client.openUrl(method, Uri.parse('$_baseUrl$path'));

  if (bearer != null) {
    request.headers.set('Authorization', 'Bearer $bearer');
  }

  if (body != null) {
    request.headers.contentType = ContentType.json;
    request.write(jsonEncode(body));
  }

  final response = await request.close();
  final text = await response.transform(utf8.decoder).join();
  client.close();

  return _Response(
    response.statusCode,
    text.isEmpty ? null : jsonDecode(text),
  );
}

Future<_Response> _get(String path, String bearer) =>
    _send('GET', path, bearer: bearer);

Future<_Response> _post(String path, {Object? body, String? bearer}) =>
    _send('POST', path, body: body, bearer: bearer);

Future<String> _login(String email, String password) async {
  final response = await _post(
    '/api/auth/login',
    body: {'email': email, 'password': password},
  );

  if (response.statusCode != 200) {
    stderr.writeln('Cannot sign in as $email (${response.statusCode}).');
    exit(2);
  }

  return AuthResponse.fromJson(response.map!).accessToken;
}

Future<void> _checkAuth() async {
  section('Authentication');

  final login = await _post(
    '/api/auth/login',
    body: {'email': 'ivan@construction.local', 'password': 'Changed123!'},
  );
  check('login returns 200', login.statusCode == 200, login.statusCode);

  final auth = AuthResponse.fromJson(login.map!);
  check('access token parsed', auth.accessToken.isNotEmpty);
  check(
    'token expiry parsed as a future UTC instant',
    auth.accessTokenExpiresAt.isAfter(DateTime.now().toUtc()),
  );
  check('user parsed', auth.user.email == 'ivan@construction.local');
  check('display name derived from employee',
      auth.user.displayName == 'Ivan Horvat', auth.user.displayName);
  check('employee link detected', auth.user.isEmployee);

  final session = AuthSession.fromResponse(auth);
  final restored = AuthSession.fromJson(
    jsonDecode(jsonEncode(session.toJson())) as Map<String, dynamic>,
  );
  check('session survives the secure-storage round trip', restored == session);

  final me = await _get('/api/auth/me', auth.accessToken);
  check('GET /me returns 200', me.statusCode == 200, me.statusCode);
  check('/me parses into User', User.fromJson(me.map!).id == auth.user.id);

  final refresh = await _post(
    '/api/auth/refresh',
    body: {'refreshToken': auth.refreshToken},
  );
  check('refresh returns 200', refresh.statusCode == 200, refresh.statusCode);
  final refreshed = AuthResponse.fromJson(refresh.map!);
  check('rotation issued a different refresh token',
      refreshed.refreshToken != auth.refreshToken);

  final invalid = await _post(
    '/api/auth/login',
    body: {'email': 'not-an-email', 'password': ''},
  );
  check('invalid login returns 400', invalid.statusCode == 400);
  check('field names are PascalCase as the client assumes',
      (invalid.map?['errors'] as Map?)?.containsKey('Email') ?? false);

  final wrongPassword = await _post(
    '/api/auth/login',
    body: {'email': 'ivan@construction.local', 'password': 'WrongPassword1'},
  );
  check('wrong password returns 401', wrongPassword.statusCode == 401);
  check('problem details carry a detail message',
      (wrongPassword.map?['detail'] as String?)?.isNotEmpty ?? false);

  final logout = await _post(
    '/api/auth/logout',
    body: {'refreshToken': refreshed.refreshToken},
    bearer: refreshed.accessToken,
  );
  check('logout returns 204', logout.statusCode == 204, logout.statusCode);

  final replay = await _post(
    '/api/auth/refresh',
    body: {'refreshToken': refreshed.refreshToken},
  );
  check('revoked refresh token is rejected', replay.statusCode == 401);
}

Future<void> _checkEmployees(String adminToken) async {
  section('Employees');

  final list = await _get('/api/employees?pageNumber=1&pageSize=2', adminToken);
  check('list returns 200', list.statusCode == 200, list.statusCode);

  final page = PagedList<Employee>.fromJson(list.map!, Employee.fromJson);
  check('pagination envelope parsed',
      page.pageNumber == 1 && page.pageSize == 2, 'total ${page.totalCount}');
  check('employees parsed', page.items.isNotEmpty);
  check('date-only fields parsed',
      page.items.every((e) => e.employmentDate.year > 1900));
  check('initials derived', page.items.first.initials.isNotEmpty,
      page.items.first.initials);

  final search = await _get('/api/employees?search=horv', adminToken);
  final found = PagedList<Employee>.fromJson(search.map!, Employee.fromJson);
  check('search narrows the list', found.items.length == 1,
      found.items.map((e) => e.fullName).toList());

  final detail = await _get('/api/employees/${found.items.first.id}', adminToken);
  check('detail returns 200', detail.statusCode == 200);
  final employee = EmployeeDetail.fromJson(detail.map!);
  check('detail parses project assignments',
      employee.projects.isNotEmpty, employee.projects.length);
  check('detail exposes the account flag', employee.hasUserAccount);

  final missing = await _get(
    '/api/employees/00000000-0000-0000-0000-000000000000',
    adminToken,
  );
  check('unknown employee returns 404', missing.statusCode == 404);

  final badSort = await _get('/api/employees?sortBy=hackme', adminToken);
  check('unknown sort field is rejected', badSort.statusCode == 400);
}

Future<void> _checkProjects(String adminToken) async {
  section('Projects');

  final list = await _get('/api/projects', adminToken);
  check('list returns 200', list.statusCode == 200, list.statusCode);

  final page = PagedList<Project>.fromJson(list.map!, Project.fromJson);
  check('projects parsed', page.items.isNotEmpty, page.totalCount);
  check('employee count present',
      page.items.every((project) => project.employeeCount >= 0));

  final detail = await _get('/api/projects/${page.items.first.id}', adminToken);
  check('detail returns 200', detail.statusCode == 200);

  final project = ProjectDetail.fromJson(detail.map!);
  check('detail parses the crew', project.employees.isNotEmpty,
      project.employees.length);
  check('coordinate helper matches the payload',
      project.hasCoordinates == (project.latitude != null));
}

Future<void> _checkDirectoryIsGated(String workerToken) async {
  section('Role gating');

  final employees = await _get('/api/employees', workerToken);
  check('Worker cannot read employees', employees.statusCode == 403,
      employees.statusCode);

  final projects = await _get('/api/projects', workerToken);
  check('Worker cannot read projects', projects.statusCode == 403,
      projects.statusCode);
}

Future<void> _checkNotifications(String workerToken) async {
  section('Notifications');

  final list = await _get('/api/notifications', workerToken);
  check('inbox returns 200', list.statusCode == 200, list.statusCode);

  final page =
      PagedList<AppNotification>.fromJson(list.map!, AppNotification.fromJson);
  check('notifications parsed', page.items.isNotEmpty, page.totalCount);
  check('assignment payload is present',
      page.items.any((item) => item.dataJson != null));

  final unread = await _get('/api/notifications/unread-count', workerToken);
  check('unread count returns a number', unread.body is int, unread.body);

  final unreadOnly =
      await _get('/api/notifications?unreadOnly=true', workerToken);
  final unreadPage = PagedList<AppNotification>.fromJson(
      unreadOnly.map!, AppNotification.fromJson);
  check('unreadOnly returns only unread rows',
      unreadPage.items.every((item) => !item.isRead));

  final token = 'contract-check-token-${DateTime.now().millisecondsSinceEpoch}';

  final register = await _post(
    '/api/notifications/device-tokens',
    body: {'token': token, 'platform': 'Android'},
    bearer: workerToken,
  );
  check('device token registered', register.statusCode == 204,
      register.statusCode);

  final unregister = await _post(
    '/api/notifications/device-tokens/unregister',
    body: {'token': token},
    bearer: workerToken,
  );
  check('device token unregistered', unregister.statusCode == 204,
      unregister.statusCode);
}

Future<void> _checkLocations(String workerToken, String adminToken) async {
  section('GPS reporting');

  final now = DateTime.now().toUtc();

  final report = await _post(
    '/api/locations',
    body: {
      'pings': [
        {
          'latitude': 45.8131,
          'longitude': 15.9775,
          'accuracy': 7.5,
          'timestamp':
              now.subtract(const Duration(minutes: 1)).toIso8601String(),
        },
        {
          'latitude': 45.8132,
          'longitude': 15.9778,
          'accuracy': 5.0,
          'timestamp': now.toIso8601String(),
        },
      ],
    },
    bearer: workerToken,
  );
  check('batched pings accepted', report.statusCode == 202, report.statusCode);
  check('server reports how many were stored', report.body == 2, report.body);

  final rejected = await _post(
    '/api/locations',
    body: {
      'pings': [
        {'latitude': 95.0, 'longitude': 15.0, 'timestamp': now.toIso8601String()},
      ],
    },
    bearer: workerToken,
  );
  check('out-of-range latitude rejected', rejected.statusCode == 400);

  final notAnEmployee = await _post(
    '/api/locations',
    body: {
      'pings': [
        {'latitude': 45.0, 'longitude': 15.0, 'timestamp': now.toIso8601String()},
      ],
    },
    bearer: adminToken,
  );
  check('account without an employee link is refused',
      notAnEmployee.statusCode == 403, notAnEmployee.statusCode);

  final current = await _get('/api/locations/current', adminToken);
  check('live map data returns 200', current.statusCode == 200);
  check('map payload is a list', current.body is List,
      (current.body as List?)?.length);
}

Future<void> main() async {
  final adminToken = await _login('admin@construction.local', 'Changed123!');
  final workerToken = await _login('ivan@construction.local', 'Changed123!');

  await _checkAuth();
  await _checkEmployees(adminToken);
  await _checkProjects(adminToken);
  await _checkDirectoryIsGated(workerToken);
  await _checkNotifications(workerToken);
  await _checkLocations(workerToken, adminToken);

  stdout.writeln(_failures == 0
      ? '\nAll contract checks passed.'
      : '\n$_failures contract check(s) FAILED.');
  exit(_failures == 0 ? 0 : 1);
}
