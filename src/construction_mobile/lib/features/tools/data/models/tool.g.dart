// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'tool.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Tool _$ToolFromJson(Map<String, dynamic> json) => _Tool(
  id: json['id'] as String,
  name: json['name'] as String,
  category: json['category'] as String?,
  serialNumber: json['serialNumber'] as String?,
  qrCode: json['qrCode'] as String?,
  status: json['status'] as String,
  assignedEmployeeId: json['assignedEmployeeId'] as String?,
  assignedEmployeeName: json['assignedEmployeeName'] as String?,
  assignedEmployeeNumber: json['assignedEmployeeNumber'] as String?,
  assignedProjectId: json['assignedProjectId'] as String?,
  assignedProjectName: json['assignedProjectName'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$ToolToJson(_Tool instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'category': ?instance.category,
  'serialNumber': ?instance.serialNumber,
  'qrCode': ?instance.qrCode,
  'status': instance.status,
  'assignedEmployeeId': ?instance.assignedEmployeeId,
  'assignedEmployeeName': ?instance.assignedEmployeeName,
  'assignedEmployeeNumber': ?instance.assignedEmployeeNumber,
  'assignedProjectId': ?instance.assignedProjectId,
  'assignedProjectName': ?instance.assignedProjectName,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};
