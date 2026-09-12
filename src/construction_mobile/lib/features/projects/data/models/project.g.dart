// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'project.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Project _$ProjectFromJson(Map<String, dynamic> json) => _Project(
  id: json['id'] as String,
  name: json['name'] as String,
  description: json['description'] as String?,
  customerId: json['customerId'] as String?,
  customerName: json['customerName'] as String?,
  parentProjectId: json['parentProjectId'] as String?,
  parentProjectName: json['parentProjectName'] as String?,
  kind: json['kind'] as String? ?? 'Main',
  subProjectCount: (json['subProjectCount'] as num?)?.toInt() ?? 0,
  address: json['address'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
  countryCode: json['countryCode'] as String?,
  shiftStartTime: json['shiftStartTime'] as String?,
  startDate: json['startDate'] == null
      ? null
      : DateTime.parse(json['startDate'] as String),
  endDate: json['endDate'] == null
      ? null
      : DateTime.parse(json['endDate'] as String),
  status: json['status'] as String,
  contractValue: (json['contractValue'] as num?)?.toDouble(),
  employeeCount: (json['employeeCount'] as num?)?.toInt() ?? 0,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$ProjectToJson(_Project instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'description': ?instance.description,
  'customerId': ?instance.customerId,
  'customerName': ?instance.customerName,
  'parentProjectId': ?instance.parentProjectId,
  'parentProjectName': ?instance.parentProjectName,
  'kind': instance.kind,
  'subProjectCount': instance.subProjectCount,
  'address': ?instance.address,
  'latitude': ?instance.latitude,
  'longitude': ?instance.longitude,
  'countryCode': ?instance.countryCode,
  'shiftStartTime': ?instance.shiftStartTime,
  'startDate': ?instance.startDate?.toIso8601String(),
  'endDate': ?instance.endDate?.toIso8601String(),
  'status': instance.status,
  'contractValue': ?instance.contractValue,
  'employeeCount': instance.employeeCount,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};

_ProjectDetail _$ProjectDetailFromJson(Map<String, dynamic> json) =>
    _ProjectDetail(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      customerId: json['customerId'] as String?,
      customerName: json['customerName'] as String?,
      parentProjectId: json['parentProjectId'] as String?,
      parentProjectName: json['parentProjectName'] as String?,
      kind: json['kind'] as String? ?? 'Main',
      subProjectCount: (json['subProjectCount'] as num?)?.toInt() ?? 0,
      address: json['address'] as String?,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      countryCode: json['countryCode'] as String?,
      shiftStartTime: json['shiftStartTime'] as String?,
      startDate: json['startDate'] == null
          ? null
          : DateTime.parse(json['startDate'] as String),
      endDate: json['endDate'] == null
          ? null
          : DateTime.parse(json['endDate'] as String),
      status: json['status'] as String,
      contractValue: (json['contractValue'] as num?)?.toDouble(),
      employeeCount: (json['employeeCount'] as num?)?.toInt() ?? 0,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
      employees:
          (json['employees'] as List<dynamic>?)
              ?.map((e) => ProjectEmployee.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <ProjectEmployee>[],
    );

Map<String, dynamic> _$ProjectDetailToJson(_ProjectDetail instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'description': ?instance.description,
      'customerId': ?instance.customerId,
      'customerName': ?instance.customerName,
      'parentProjectId': ?instance.parentProjectId,
      'parentProjectName': ?instance.parentProjectName,
      'kind': instance.kind,
      'subProjectCount': instance.subProjectCount,
      'address': ?instance.address,
      'latitude': ?instance.latitude,
      'longitude': ?instance.longitude,
      'countryCode': ?instance.countryCode,
      'shiftStartTime': ?instance.shiftStartTime,
      'startDate': ?instance.startDate?.toIso8601String(),
      'endDate': ?instance.endDate?.toIso8601String(),
      'status': instance.status,
      'contractValue': ?instance.contractValue,
      'employeeCount': instance.employeeCount,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
      'employees': instance.employees.map((e) => e.toJson()).toList(),
    };

_ProjectEmployee _$ProjectEmployeeFromJson(Map<String, dynamic> json) =>
    _ProjectEmployee(
      employeeId: json['employeeId'] as String,
      employeeNumber: json['employeeNumber'] as String,
      fullName: json['fullName'] as String,
      position: json['position'] as String,
      status: json['status'] as String,
      assignedAt: DateTime.parse(json['assignedAt'] as String),
    );

Map<String, dynamic> _$ProjectEmployeeToJson(_ProjectEmployee instance) =>
    <String, dynamic>{
      'employeeId': instance.employeeId,
      'employeeNumber': instance.employeeNumber,
      'fullName': instance.fullName,
      'position': instance.position,
      'status': instance.status,
      'assignedAt': instance.assignedAt.toIso8601String(),
    };
