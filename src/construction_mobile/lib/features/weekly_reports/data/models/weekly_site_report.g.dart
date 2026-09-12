// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'weekly_site_report.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_WeeklySiteReport _$WeeklySiteReportFromJson(Map<String, dynamic> json) =>
    _WeeklySiteReport(
      id: json['id'] as String,
      projectId: json['projectId'] as String,
      projectName: json['projectName'] as String,
      submittedByEmployeeId: json['submittedByEmployeeId'] as String,
      submittedByEmployeeName: json['submittedByEmployeeName'] as String,
      isoYear: (json['isoYear'] as num).toInt(),
      isoWeek: (json['isoWeek'] as num).toInt(),
      type: json['type'] as String,
      quantity: (json['quantity'] as num?)?.toDouble(),
      note: json['note'] as String?,
      fileName: json['fileName'] as String,
      status: json['status'] as String,
      processedAt: json['processedAt'] == null
          ? null
          : DateTime.parse(json['processedAt'] as String),
      processedByEmail: json['processedByEmail'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$WeeklySiteReportToJson(_WeeklySiteReport instance) =>
    <String, dynamic>{
      'id': instance.id,
      'projectId': instance.projectId,
      'projectName': instance.projectName,
      'submittedByEmployeeId': instance.submittedByEmployeeId,
      'submittedByEmployeeName': instance.submittedByEmployeeName,
      'isoYear': instance.isoYear,
      'isoWeek': instance.isoWeek,
      'type': instance.type,
      'quantity': ?instance.quantity,
      'note': ?instance.note,
      'fileName': instance.fileName,
      'status': instance.status,
      'processedAt': ?instance.processedAt?.toIso8601String(),
      'processedByEmail': ?instance.processedByEmail,
      'createdAt': instance.createdAt.toIso8601String(),
    };

_ReportableProject _$ReportableProjectFromJson(Map<String, dynamic> json) =>
    _ReportableProject(id: json['id'] as String, name: json['name'] as String);

Map<String, dynamic> _$ReportableProjectToJson(_ReportableProject instance) =>
    <String, dynamic>{'id': instance.id, 'name': instance.name};
