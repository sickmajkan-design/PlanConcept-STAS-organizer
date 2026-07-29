/// Every navigable location in the app, in one place.
class AppRoutes {
  const AppRoutes._();

  static const splash = '/';
  static const login = '/login';
  static const forgotPassword = '/forgot-password';
  static const changePassword = '/change-password';

  static const home = '/home';
  static const employees = '/employees';
  static const projects = '/projects';
  static const notifications = '/notifications';

  static String employeeDetail(String id) => '$employees/$id';

  static String projectDetail(String id) => '$projects/$id';

  /// Locations reachable without a session.
  static const anonymous = <String>{login, forgotPassword};

  /// Locations the API only serves to Foreman and above; a Worker opening one
  /// would only ever get a 403.
  static const directory = <String>{employees, projects};

  static bool isDirectoryLocation(String location) =>
      directory.any((prefix) => location.startsWith(prefix));
}
