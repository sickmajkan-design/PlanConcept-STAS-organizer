// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'vehicle.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Vehicle _$VehicleFromJson(Map<String, dynamic> json) => _Vehicle(
  id: json['id'] as String,
  brand: json['brand'] as String,
  model: json['model'] as String,
  registrationNumber: json['registrationNumber'] as String,
  vin: json['vin'] as String?,
  fuelType: json['fuelType'] as String,
  status: json['status'] as String,
  assignedEmployeeId: json['assignedEmployeeId'] as String?,
  assignedEmployeeName: json['assignedEmployeeName'] as String?,
  assignedEmployeeNumber: json['assignedEmployeeNumber'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$VehicleToJson(_Vehicle instance) => <String, dynamic>{
  'id': instance.id,
  'brand': instance.brand,
  'model': instance.model,
  'registrationNumber': instance.registrationNumber,
  'vin': ?instance.vin,
  'fuelType': instance.fuelType,
  'status': instance.status,
  'assignedEmployeeId': ?instance.assignedEmployeeId,
  'assignedEmployeeName': ?instance.assignedEmployeeName,
  'assignedEmployeeNumber': ?instance.assignedEmployeeNumber,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};
