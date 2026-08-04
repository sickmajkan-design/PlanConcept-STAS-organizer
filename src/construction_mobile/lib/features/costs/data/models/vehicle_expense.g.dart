// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'vehicle_expense.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_VehicleExpense _$VehicleExpenseFromJson(Map<String, dynamic> json) =>
    _VehicleExpense(
      id: json['id'] as String,
      vehicleId: json['vehicleId'] as String,
      vehicleName: json['vehicleName'] as String,
      kind: json['kind'] as String,
      amount: (json['amount'] as num).toDouble(),
      occurredOn: json['occurredOn'] as String,
      litres: (json['litres'] as num?)?.toDouble(),
      pricePerLitre: (json['pricePerLitre'] as num?)?.toDouble(),
      odometerKm: (json['odometerKm'] as num?)?.toInt(),
      supplier: json['supplier'] as String?,
      note: json['note'] as String?,
      recordedByName: json['recordedByName'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$VehicleExpenseToJson(_VehicleExpense instance) =>
    <String, dynamic>{
      'id': instance.id,
      'vehicleId': instance.vehicleId,
      'vehicleName': instance.vehicleName,
      'kind': instance.kind,
      'amount': instance.amount,
      'occurredOn': instance.occurredOn,
      'litres': ?instance.litres,
      'pricePerLitre': ?instance.pricePerLitre,
      'odometerKm': ?instance.odometerKm,
      'supplier': ?instance.supplier,
      'note': ?instance.note,
      'recordedByName': ?instance.recordedByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
