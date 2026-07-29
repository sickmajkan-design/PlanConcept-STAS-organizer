/// Every navigable location in the app, in one place.
class AppRoutes {
  const AppRoutes._();

  static const splash = '/';
  static const login = '/login';
  static const forgotPassword = '/forgot-password';
  static const home = '/home';
  static const changePassword = '/change-password';

  /// Locations reachable without a session.
  static const anonymous = <String>{login, forgotPassword};
}
