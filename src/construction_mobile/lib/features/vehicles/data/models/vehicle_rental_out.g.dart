// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'vehicle_rental_out.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_VehicleRentalOut _$VehicleRentalOutFromJson(Map<String, dynamic> json) =>
    _VehicleRentalOut(
      id: json['id'] as String,
      vehicleId: json['vehicleId'] as String,
      vehicleName: json['vehicleName'] as String,
      customerId: json['customerId'] as String?,
      renterDisplayName: json['renterDisplayName'] as String,
      renterName: json['renterName'] as String,
      dailyRate: (json['dailyRate'] as num).toDouble(),
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: json['endDate'] == null
          ? null
          : DateTime.parse(json['endDate'] as String),
      isOpen: json['isOpen'] as bool,
      note: json['note'] as String?,
      setByName: json['setByName'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$VehicleRentalOutToJson(_VehicleRentalOut instance) =>
    <String, dynamic>{
      'id': instance.id,
      'vehicleId': instance.vehicleId,
      'vehicleName': instance.vehicleName,
      'customerId': ?instance.customerId,
      'renterDisplayName': instance.renterDisplayName,
      'renterName': instance.renterName,
      'dailyRate': instance.dailyRate,
      'startDate': instance.startDate.toIso8601String(),
      'endDate': ?instance.endDate?.toIso8601String(),
      'isOpen': instance.isOpen,
      'note': ?instance.note,
      'setByName': ?instance.setByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
