import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/company_settings.dart';

class CompanySettingsRepository extends ApiRepository {
  const CompanySettingsRepository(super.dio);

  /// Open to any signed-in role — nothing here is sensitive to a company's
  /// own staff, only writing it is SuperAdmin-gated.
  Future<CompanySettings> fetch() {
    return getJson('/api/v1/company-settings', CompanySettings.fromJson);
  }

  Future<CompanySettings> update({
    required String name,
    String? address,
    String? taxId,
    String? registrationNumber,
    String? vatNumber,
    String? phone,
    String? email,
    String? weeklyReportsForwardEmail,
  }) {
    return putJson(
      '/api/v1/company-settings',
      CompanySettings.fromJson,
      data: {
        'name': name,
        'address': ?address,
        'taxId': ?taxId,
        'registrationNumber': ?registrationNumber,
        'vatNumber': ?vatNumber,
        'phone': ?phone,
        'email': ?email,
        'weeklyReportsForwardEmail': ?weeklyReportsForwardEmail,
      },
    );
  }

  /// The logo's raw bytes, fetched through the authenticated client for
  /// consistency with the rest of the app — the endpoint itself allows
  /// anonymous access, but there is no harm in sending the token anyway.
  Future<Uint8List> fetchLogo() {
    return guard(() async {
      final response = await dio.get<List<int>>(
        '/api/v1/company-settings/logo',
        options: Options(responseType: ResponseType.bytes),
      );

      return Uint8List.fromList(response.data ?? const []);
    });
  }

  Future<CompanySettings> uploadLogo({
    required String filePath,
    required String fileName,
  }) {
    return guard(() async {
      final form = FormData.fromMap(<String, dynamic>{
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });

      final response = await dio.post<Map<String, dynamic>>(
        '/api/v1/company-settings/logo',
        data: form,
      );

      return CompanySettings.fromJson(response.data!);
    });
  }

  Future<void> removeLogo() {
    return deleteVoid('/api/v1/company-settings/logo');
  }
}

final companySettingsRepositoryProvider =
    Provider<CompanySettingsRepository>((ref) {
  return CompanySettingsRepository(ref.watch(apiClientProvider));
});

final companySettingsProvider =
    FutureProvider.autoDispose<CompanySettings>((ref) {
  return ref.watch(companySettingsRepositoryProvider).fetch();
});
