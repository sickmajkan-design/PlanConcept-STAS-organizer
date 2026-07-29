/// Compile-time configuration, supplied with `--dart-define`.
///
/// Example:
/// ```
/// flutter run --dart-define=API_BASE_URL=https://api.example.com
/// ```
class AppConfig {
  const AppConfig._();

  /// Base URL of the Construction API.
  ///
  /// The default targets the host machine from an Android emulator
  /// (10.0.2.2 is the emulator's alias for the developer's localhost).
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000',
  );

  /// How often the app reports its position while the user is signed in.
  static const Duration locationReportInterval = Duration(seconds: 60);

  static const Duration connectTimeout = Duration(seconds: 15);

  static const Duration receiveTimeout = Duration(seconds: 20);
}
