import 'package:freezed_annotation/freezed_annotation.dart';

part 'company_settings.freezed.dart';
part 'company_settings.g.dart';

/// Mirrors the API's `CompanySettingsDto` — the platform's own company
/// profile. A singleton: there is one company using this app.
@freezed
abstract class CompanySettings with _$CompanySettings {
  const factory CompanySettings({
    String? name,
    String? address,
    String? taxId,
    String? registrationNumber,
    String? vatNumber,
    String? phone,
    String? email,
    String? weeklyReportsForwardEmail,
    @Default(false) bool hasLogo,
    DateTime? updatedAt,
  }) = _CompanySettings;

  factory CompanySettings.fromJson(Map<String, dynamic> json) =>
      _$CompanySettingsFromJson(json);
}
