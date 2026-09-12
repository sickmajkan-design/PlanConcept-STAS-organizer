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
  qrCode: json['qrCode'] as String?,
  fuelType: json['fuelType'] as String,
  status: json['status'] as String,
  ownershipType: json['ownershipType'] as String? ?? 'Owned',
  currentRentalMonthlyAmount: (json['currentRentalMonthlyAmount'] as num?)
      ?.toDouble(),
  currentRentalProvider: json['currentRentalProvider'] as String?,
  currentRentalOutRenterName: json['currentRentalOutRenterName'] as String?,
  currentRentalOutDailyRate: (json['currentRentalOutDailyRate'] as num?)
      ?.toDouble(),
  currentRentalOutStartDate: json['currentRentalOutStartDate'] as String?,
  lastRentalOutRenterName: json['lastRentalOutRenterName'] as String?,
  lastRentalOutEndDate: json['lastRentalOutEndDate'] as String?,
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
  'qrCode': ?instance.qrCode,
  'fuelType': instance.fuelType,
  'status': instance.status,
  'ownershipType': instance.ownershipType,
  'currentRentalMonthlyAmount': ?instance.currentRentalMonthlyAmount,
  'currentRentalProvider': ?instance.currentRentalProvider,
  'currentRentalOutRenterName': ?instance.currentRentalOutRenterName,
  'currentRentalOutDailyRate': ?instance.currentRentalOutDailyRate,
  'currentRentalOutStartDate': ?instance.currentRentalOutStartDate,
  'lastRentalOutRenterName': ?instance.lastRentalOutRenterName,
  'lastRentalOutEndDate': ?instance.lastRentalOutEndDate,
  'assignedEmployeeId': ?instance.assignedEmployeeId,
  'assignedEmployeeName': ?instance.assignedEmployeeName,
  'assignedEmployeeNumber': ?instance.assignedEmployeeNumber,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};
