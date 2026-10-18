import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../network/network_providers.dart';
import 'company_time.dart';

/// Where the last zone the organisation was seen to use is kept, so the first screen of the next start already counts
/// days the organisation's way.
const companyTimeZoneStorageKey = 'company_time_zone';

/// Restores the zone from the previous run. Before the first screen, with the zone database loaded.
Future<void> restoreCompanyTimeZone(FlutterSecureStorage storage) async {
  CompanyTime.ensureReady();

  try {
    CompanyTime.setZone(await storage.read(key: companyTimeZoneStorageKey));
  } catch (_) {
    // A keystore that cannot be read leaves the default until the server has answered.
  }
}

/// Asks the server which time zone the organisation chose, and takes it up.
///
/// The zone is part of the organisation's public branding, so it needs no sign-in. A failed request changes nothing:
/// the zone from the last run, or the default, stays until the next try (the app asks again each time it comes back
/// to the foreground).
final companyTimeSyncProvider = FutureProvider<void>((ref) async {
  final dio = ref.watch(plainClientProvider);
  final storage = ref.watch(secureStorageProvider);

  try {
    final response = await dio.get<Map<String, dynamic>>('/api/v1/company-settings/branding');
    final zone = response.data?['timeZoneId'];

    if (zone is String && CompanyTime.setZone(zone)) {
      await storage.write(key: companyTimeZoneStorageKey, value: zone);
    }
  } catch (_) {
    // No signal, or an old server that does not say: keep what is in force.
  }
});
