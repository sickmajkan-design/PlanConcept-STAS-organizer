// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'absence.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Absence _$AbsenceFromJson(Map<String, dynamic> json) => _Absence(
  id: json['id'] as String,
  employeeId: json['employeeId'] as String,
  employeeName: json['employeeName'] as String,
  type: json['type'] as String,
  status: json['status'] as String,
  startDate: json['startDate'] as String,
  endDate: json['endDate'] as String,
  dayCount: (json['dayCount'] as num).toInt(),
  reason: json['reason'] as String?,
  requestedByName: json['requestedByName'] as String?,
  reviewedByName: json['reviewedByName'] as String?,
  reviewedAt: json['reviewedAt'] == null
      ? null
      : DateTime.parse(json['reviewedAt'] as String),
  reviewNote: json['reviewNote'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$AbsenceToJson(_Absence instance) => <String, dynamic>{
  'id': instance.id,
  'employeeId': instance.employeeId,
  'employeeName': instance.employeeName,
  'type': instance.type,
  'status': instance.status,
  'startDate': instance.startDate,
  'endDate': instance.endDate,
  'dayCount': instance.dayCount,
  'reason': ?instance.reason,
  'requestedByName': ?instance.requestedByName,
  'reviewedByName': ?instance.reviewedByName,
  'reviewedAt': ?instance.reviewedAt?.toIso8601String(),
  'reviewNote': ?instance.reviewNote,
  'createdAt': instance.createdAt.toIso8601String(),
};
