// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'company_settings.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_CompanySettings _$CompanySettingsFromJson(Map<String, dynamic> json) =>
    _CompanySettings(
      name: json['name'] as String?,
      address: json['address'] as String?,
      taxId: json['taxId'] as String?,
      registrationNumber: json['registrationNumber'] as String?,
      vatNumber: json['vatNumber'] as String?,
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      weeklyReportsForwardEmail: json['weeklyReportsForwardEmail'] as String?,
      hasLogo: json['hasLogo'] as bool? ?? false,
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$CompanySettingsToJson(_CompanySettings instance) =>
    <String, dynamic>{
      'name': ?instance.name,
      'address': ?instance.address,
      'taxId': ?instance.taxId,
      'registrationNumber': ?instance.registrationNumber,
      'vatNumber': ?instance.vatNumber,
      'phone': ?instance.phone,
      'email': ?instance.email,
      'weeklyReportsForwardEmail': ?instance.weeklyReportsForwardEmail,
      'hasLogo': instance.hasLogo,
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
    };
