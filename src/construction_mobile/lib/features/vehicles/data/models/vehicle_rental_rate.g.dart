// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'vehicle_rental_rate.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_VehicleRentalRate _$VehicleRentalRateFromJson(Map<String, dynamic> json) =>
    _VehicleRentalRate(
      id: json['id'] as String,
      vehicleId: json['vehicleId'] as String,
      vehicleName: json['vehicleName'] as String,
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

Map<String, dynamic> _$VehicleRentalRateToJson(_VehicleRentalRate instance) =>
    <String, dynamic>{
      'id': instance.id,
      'vehicleId': instance.vehicleId,
      'vehicleName': instance.vehicleName,
      'monthlyAmount': instance.monthlyAmount,
      'provider': ?instance.provider,
      'startDate': instance.startDate.toIso8601String(),
      'endDate': ?instance.endDate?.toIso8601String(),
      'note': ?instance.note,
      'setByName': ?instance.setByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
