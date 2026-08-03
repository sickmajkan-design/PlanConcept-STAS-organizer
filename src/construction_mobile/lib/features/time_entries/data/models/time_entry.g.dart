// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'time_entry.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_TimeEntry _$TimeEntryFromJson(Map<String, dynamic> json) => _TimeEntry(
  id: json['id'] as String,
  employeeId: json['employeeId'] as String,
  employeeName: json['employeeName'] as String,
  projectId: json['projectId'] as String?,
  projectName: json['projectName'] as String?,
  startedAt: DateTime.parse(json['startedAt'] as String),
  endedAt: json['endedAt'] == null
      ? null
      : DateTime.parse(json['endedAt'] as String),
  breakMinutes: (json['breakMinutes'] as num).toInt(),
  workedMinutes: (json['workedMinutes'] as num?)?.toInt(),
  workType: json['workType'] as String,
  status: json['status'] as String,
  note: json['note'] as String?,
  startLatitude: (json['startLatitude'] as num?)?.toDouble(),
  startLongitude: (json['startLongitude'] as num?)?.toDouble(),
  endLatitude: (json['endLatitude'] as num?)?.toDouble(),
  endLongitude: (json['endLongitude'] as num?)?.toDouble(),
  reviewedByName: json['reviewedByName'] as String?,
  reviewedAt: json['reviewedAt'] == null
      ? null
      : DateTime.parse(json['reviewedAt'] as String),
  reviewNote: json['reviewNote'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$TimeEntryToJson(_TimeEntry instance) =>
    <String, dynamic>{
      'id': instance.id,
      'employeeId': instance.employeeId,
      'employeeName': instance.employeeName,
      'projectId': ?instance.projectId,
      'projectName': ?instance.projectName,
      'startedAt': instance.startedAt.toIso8601String(),
      'endedAt': ?instance.endedAt?.toIso8601String(),
      'breakMinutes': instance.breakMinutes,
      'workedMinutes': ?instance.workedMinutes,
      'workType': instance.workType,
      'status': instance.status,
      'note': ?instance.note,
      'startLatitude': ?instance.startLatitude,
      'startLongitude': ?instance.startLongitude,
      'endLatitude': ?instance.endLatitude,
      'endLongitude': ?instance.endLongitude,
      'reviewedByName': ?instance.reviewedByName,
      'reviewedAt': ?instance.reviewedAt?.toIso8601String(),
      'reviewNote': ?instance.reviewNote,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
    };
