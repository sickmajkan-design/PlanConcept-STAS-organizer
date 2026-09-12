// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'tool_rental_rate.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_ToolRentalRate _$ToolRentalRateFromJson(Map<String, dynamic> json) =>
    _ToolRentalRate(
      id: json['id'] as String,
      toolId: json['toolId'] as String,
      toolName: json['toolName'] as String,
      monthlyAmount: (json['monthlyAmount'] as num).toDouble(),
      provider: json['provider'] as String?,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: json['endDate'] == null
          ? null
          : DateTime.parse(json['endDate'] as String),
      note: json['note'] as String?,
      setByName: json['setByName'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$ToolRentalRateToJson(_ToolRentalRate instance) =>
    <String, dynamic>{
      'id': instance.id,
      'toolId': instance.toolId,
      'toolName': instance.toolName,
      'monthlyAmount': instance.monthlyAmount,
      'provider': ?instance.provider,
      'startDate': instance.startDate.toIso8601String(),
      'endDate': ?instance.endDate?.toIso8601String(),
      'note': ?instance.note,
      'setByName': ?instance.setByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
