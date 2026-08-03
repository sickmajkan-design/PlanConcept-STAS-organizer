// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'schedule.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Schedule _$ScheduleFromJson(Map<String, dynamic> json) => _Schedule(
  from: json['from'] as String,
  to: json['to'] as String,
  rows:
      (json['rows'] as List<dynamic>?)
          ?.map((e) => ScheduleRow.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <ScheduleRow>[],
);

Map<String, dynamic> _$ScheduleToJson(_Schedule instance) => <String, dynamic>{
  'from': instance.from,
  'to': instance.to,
  'rows': instance.rows.map((e) => e.toJson()).toList(),
};

_ScheduleRow _$ScheduleRowFromJson(Map<String, dynamic> json) => _ScheduleRow(
  employeeId: json['employeeId'] as String,
  employeeName: json['employeeName'] as String,
  position: json['position'] as String,
  assignments:
      (json['assignments'] as List<dynamic>?)
          ?.map((e) => ScheduleAssignment.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <ScheduleAssignment>[],
  absences:
      (json['absences'] as List<dynamic>?)
          ?.map((e) => ScheduleAbsence.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <ScheduleAbsence>[],
);

Map<String, dynamic> _$ScheduleRowToJson(_ScheduleRow instance) =>
    <String, dynamic>{
      'employeeId': instance.employeeId,
      'employeeName': instance.employeeName,
      'position': instance.position,
      'assignments': instance.assignments.map((e) => e.toJson()).toList(),
      'absences': instance.absences.map((e) => e.toJson()).toList(),
    };

_ScheduleAssignment _$ScheduleAssignmentFromJson(Map<String, dynamic> json) =>
    _ScheduleAssignment(
      id: json['id'] as String,
      projectId: json['projectId'] as String,
      projectName: json['projectName'] as String,
      from: json['from'] as String,
      to: json['to'] as String,
      continuesAfter: json['continuesAfter'] as bool? ?? false,
    );

Map<String, dynamic> _$ScheduleAssignmentToJson(_ScheduleAssignment instance) =>
    <String, dynamic>{
      'id': instance.id,
      'projectId': instance.projectId,
      'projectName': instance.projectName,
      'from': instance.from,
      'to': instance.to,
      'continuesAfter': instance.continuesAfter,
    };

_ScheduleAbsence _$ScheduleAbsenceFromJson(Map<String, dynamic> json) =>
    _ScheduleAbsence(
      id: json['id'] as String,
      type: json['type'] as String,
      from: json['from'] as String,
      to: json['to'] as String,
    );

Map<String, dynamic> _$ScheduleAbsenceToJson(_ScheduleAbsence instance) =>
    <String, dynamic>{
      'id': instance.id,
      'type': instance.type,
      'from': instance.from,
      'to': instance.to,
    };
