import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'app_config.dart';

/// Which server this installation talks to, chosen on the device.
///
/// It used to be baked in at compile time by `--dart-define`, which is fine
/// for a developer and wrong for everybody else: the default is the Android
/// emulator's alias for a laptop's localhost, so a build handed to a real
/// phone points at nothing. Every customer would need their own build, and a
/// site that moved its server would need a new one.
///
/// This is the same decision the admin panel already makes with `config.js` —
/// one artefact, told where it is at run time — arrived at for the same
/// reason.
class ServerAddress {
  const ServerAddress._();

  static const String storageKey = 'server_base_url';

  /// Trims, drops a trailing slash, and refuses anything that is not an
  /// absolute http(s) URL.
  ///
  /// Returns null when the text cannot be used. A wrong address typed on a
  /// phone is the most likely mistake here, and the failure it causes — every
  /// request timing out — looks exactly like no signal, which is the last
  /// thing anybody would suspect.
  static String? normalize(String? raw) {
    final trimmed = raw?.trim();

    if (trimmed == null || trimmed.isEmpty) {
      return null;
    }

    final uri = Uri.tryParse(trimmed);

    // `host`, not `hasAuthority`: `https://` parses as an absolute URI with an
    // authority component that happens to be empty, and would otherwise be
    // accepted as an address pointing at nowhere.
    if (uri == null || !uri.isAbsolute || uri.host.isEmpty) {
      return null;
    }

    if (uri.scheme != 'http' && uri.scheme != 'https') {
      return null;
    }

    final withoutTrailingSlash = trimmed.endsWith('/')
        ? trimmed.substring(0, trimmed.length - 1)
        : trimmed;

    return withoutTrailingSlash;
  }

  /// Reads the stored address, or null if the device has never been told one.
  static Future<String?> read(FlutterSecureStorage storage) async {
    try {
      return normalize(await storage.read(key: storageKey));
    } catch (_) {
      // A device whose keystore is unavailable still has a compiled default.
      return null;
    }
  }

  static Future<void> write(FlutterSecureStorage storage, String address) =>
      storage.write(key: storageKey, value: address);
}

/// The address the app started with: stored if there is one, compiled in if not.
///
/// Overridden in `main` once storage has been read, because the network
/// providers need it synchronously and reading a keystore is not.
final initialServerAddressProvider = Provider<String>((ref) {
  return AppConfig.apiBaseUrl;
});

/// The address in use, and the way to change it.
final serverAddressProvider =
    NotifierProvider<ServerAddressNotifier, String>(ServerAddressNotifier.new);

class ServerAddressNotifier extends Notifier<String> {
  @override
  String build() => ref.watch(initialServerAddressProvider);

  /// Points the app at a different server.
  ///
  /// Returns false when the text is not a usable address, so the screen can
  /// say so rather than storing something that will only fail later, far from
  /// where it was typed.
  Future<bool> set(String raw) async {
    final normalized = ServerAddress.normalize(raw);

    if (normalized == null) {
      return false;
    }

    await ServerAddress.write(ref.read(serverAddressStorageProvider), normalized);

    // Every Dio in the app watches this, so changing it rebuilds them with
    // the new base URL. Nothing has to be restarted.
    state = normalized;

    return true;
  }
}

/// The storage the address is kept in.
///
/// Its own provider, with a working default, so a test can supply one without
/// dragging in the network layer — and so this file does not have to import
/// the module that imports it.
final serverAddressStorageProvider = Provider<FlutterSecureStorage>((ref) {
  return const FlutterSecureStorage();
});
