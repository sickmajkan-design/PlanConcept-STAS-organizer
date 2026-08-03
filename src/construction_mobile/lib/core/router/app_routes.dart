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
  static const vehicles = '/vehicles';
  static const tools = '/tools';
  static const materials = '/materials';
  static const notifications = '/notifications';

  /// Open to every employee-linked account, Worker included, so it must not
  /// sit under a directory prefix.
  static const timeEntries = '/time-entries';

  /// Open to every authenticated employee (mirrors the API's `by-qr`
  /// endpoint), so it must not sit under [tools] or it would be swept into
  /// [isDirectoryLocation].
  static const toolLookup = '/tool-lookup';

  static String employeeDetail(String id) => '$employees/$id';

  static String projectDetail(String id) => '$projects/$id';

  static String vehicleDetail(String id) => '$vehicles/$id';

  static String toolDetail(String id) => '$tools/$id';

  static String materialDetail(String id) => '$materials/$id';

  /// Locations reachable without a session.
  static const anonymous = <String>{login, forgotPassword};

  /// Locations the API only serves to Foreman and above; a Worker opening one
  /// would only ever get a 403.
  static const directory = <String>{employees, projects, vehicles, tools, materials};

  static bool isDirectoryLocation(String location) =>
      directory.any((prefix) => location.startsWith(prefix));
}
