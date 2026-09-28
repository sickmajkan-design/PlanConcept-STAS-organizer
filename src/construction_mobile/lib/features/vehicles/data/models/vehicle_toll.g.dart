// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'vehicle_toll.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_VehicleToll _$VehicleTollFromJson(Map<String, dynamic> json) => _VehicleToll(
  id: json['id'] as String,
  vehicleId: json['vehicleId'] as String,
  type: json['type'] as String,
  country: json['country'] as String,
  routeSegment: json['routeSegment'] as String?,
  status: json['status'] as String,
  validUntil: json['validUntil'] == null
      ? null
      : DateTime.parse(json['validUntil'] as String),
  computedState: json['computedState'] as String,
  paidByUserName: json['paidByUserName'] as String?,
  paidAt: json['paidAt'] == null
      ? null
      : DateTime.parse(json['paidAt'] as String),
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$VehicleTollToJson(_VehicleToll instance) =>
    <String, dynamic>{
      'id': instance.id,
      'vehicleId': instance.vehicleId,
      'type': instance.type,
      'country': instance.country,
      'routeSegment': ?instance.routeSegment,
      'status': instance.status,
      'validUntil': ?instance.validUntil?.toIso8601String(),
      'computedState': instance.computedState,
      'paidByUserName': ?instance.paidByUserName,
      'paidAt': ?instance.paidAt?.toIso8601String(),
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
    };
