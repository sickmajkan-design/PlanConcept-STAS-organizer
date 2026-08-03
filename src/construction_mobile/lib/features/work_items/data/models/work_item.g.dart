// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'work_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_WorkItem _$WorkItemFromJson(Map<String, dynamic> json) => _WorkItem(
  id: json['id'] as String,
  kind: json['kind'] as String,
  title: json['title'] as String,
  description: json['description'] as String?,
  projectId: json['projectId'] as String?,
  projectName: json['projectName'] as String?,
  assignedEmployeeId: json['assignedEmployeeId'] as String?,
  assignedEmployeeName: json['assignedEmployeeName'] as String?,
  priority: json['priority'] as String,
  status: json['status'] as String,
  dueDate: json['dueDate'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
  createdByName: json['createdByName'] as String?,
  resolvedByName: json['resolvedByName'] as String?,
  resolvedAt: json['resolvedAt'] == null
      ? null
      : DateTime.parse(json['resolvedAt'] as String),
  attachmentCount: (json['attachmentCount'] as num?)?.toInt() ?? 0,
  isFinished: json['isFinished'] as bool? ?? false,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$WorkItemToJson(_WorkItem instance) => <String, dynamic>{
  'id': instance.id,
  'kind': instance.kind,
  'title': instance.title,
  'description': ?instance.description,
  'projectId': ?instance.projectId,
  'projectName': ?instance.projectName,
  'assignedEmployeeId': ?instance.assignedEmployeeId,
  'assignedEmployeeName': ?instance.assignedEmployeeName,
  'priority': instance.priority,
  'status': instance.status,
  'dueDate': ?instance.dueDate,
  'latitude': ?instance.latitude,
  'longitude': ?instance.longitude,
  'createdByName': ?instance.createdByName,
  'resolvedByName': ?instance.resolvedByName,
  'resolvedAt': ?instance.resolvedAt?.toIso8601String(),
  'attachmentCount': instance.attachmentCount,
  'isFinished': instance.isFinished,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};
