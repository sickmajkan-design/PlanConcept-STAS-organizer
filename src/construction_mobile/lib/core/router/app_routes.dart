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

  /// Open to every employee-linked account: a worker's own list.
  static const workItems = '/work-items';

  /// Open to every employee-linked account: where this person is posted. The
  /// API narrows the same endpoint the admin board uses to the caller's own
  /// line, so it must not sit under a directory prefix.
  static const schedule = '/schedule';

  /// Open to every employee-linked account: their own leave.
  static const absences = '/absences';

  /// Open to every employee-linked account: filing the week's proof-of-work
  /// for a site they are posted to.
  static const weeklyReports = '/weekly-reports';

  /// Foreman and above: who clocked in/out today, scoped to a Foreman's own
  /// site(s). Read-only oversight, not the review/approve screen (desktop
  /// only, for now).
  static const teamToday = '/team-today';

  /// Foreman and above, matching the API's CanRecordSpending. Not under a
  /// directory prefix because it is not a directory screen — it is the pump.
  static const vehicleExpenses = '/vehicle-expenses';
  static const toolExpenses = '/tool-expenses';
  static const companySettings = '/company-settings';

  /// SuperAdmin-only, view-only on mobile — see [ledgerDetail].
  static const ledgers = '/ledgers';

  /// Open to every authenticated employee (mirrors the API's `by-qr`
  /// endpoints for tools and vehicles), so it must not sit under [tools] or
  /// [vehicles] or it would be swept into [isDirectoryLocation].
  static const scan = '/scan';

  /// Open to every signed-in account, employee-linked or not — the API's
  /// policy is just "authenticated", the same as the notification inbox.
  static const bulletin = '/bulletin';

  static String employeeDetail(String id) => '$employees/$id';

  static String projectDetail(String id) => '$projects/$id';

  static String vehicleDetail(String id) => '$vehicles/$id';

  static String toolDetail(String id) => '$tools/$id';

  static String materialDetail(String id) => '$materials/$id';

  static String ledgerDetail(String id) => '$ledgers/$id';

  /// Locations reachable without a session.
  static const anonymous = <String>{login, forgotPassword};

  /// Locations the API only serves to Foreman and above; a Worker opening one
  /// would only ever get a 403.
  static const directory = <String>{employees, projects, vehicles, tools, materials};

  static bool isDirectoryLocation(String location) =>
      directory.any((prefix) => location.startsWith(prefix));

  /// Locations mirroring the API's `SuperAdminOnly` policy — not even Admin
  /// may reach these, matching desktop's `RequireSuperAdmin` route guard.
  static const superAdminOnly = <String>{companySettings, ledgers};

  static bool isSuperAdminOnlyLocation(String location) =>
      superAdminOnly.any((prefix) => location.startsWith(prefix));
}
