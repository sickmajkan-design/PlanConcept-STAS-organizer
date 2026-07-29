// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'employee.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Employee _$EmployeeFromJson(Map<String, dynamic> json) => _Employee(
  id: json['id'] as String,
  employeeNumber: json['employeeNumber'] as String,
  firstName: json['firstName'] as String,
  lastName: json['lastName'] as String,
  fullName: json['fullName'] as String,
  phone: json['phone'] as String?,
  email: json['email'] as String?,
  address: json['address'] as String?,
  dateOfBirth: json['dateOfBirth'] == null
      ? null
      : DateTime.parse(json['dateOfBirth'] as String),
  employmentDate: DateTime.parse(json['employmentDate'] as String),
  position: json['position'] as String,
  status: json['status'] as String,
  photoUrl: json['photoUrl'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
  updatedAt: json['updatedAt'] == null
      ? null
      : DateTime.parse(json['updatedAt'] as String),
);

Map<String, dynamic> _$EmployeeToJson(_Employee instance) => <String, dynamic>{
  'id': instance.id,
  'employeeNumber': instance.employeeNumber,
  'firstName': instance.firstName,
  'lastName': instance.lastName,
  'fullName': instance.fullName,
  'phone': ?instance.phone,
  'email': ?instance.email,
  'address': ?instance.address,
  'dateOfBirth': ?instance.dateOfBirth?.toIso8601String(),
  'employmentDate': instance.employmentDate.toIso8601String(),
  'position': instance.position,
  'status': instance.status,
  'photoUrl': ?instance.photoUrl,
  'createdAt': instance.createdAt.toIso8601String(),
  'updatedAt': ?instance.updatedAt?.toIso8601String(),
};

_EmployeeDetail _$EmployeeDetailFromJson(Map<String, dynamic> json) =>
    _EmployeeDetail(
      id: json['id'] as String,
      employeeNumber: json['employeeNumber'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      fullName: json['fullName'] as String,
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      address: json['address'] as String?,
      dateOfBirth: json['dateOfBirth'] == null
          ? null
          : DateTime.parse(json['dateOfBirth'] as String),
      employmentDate: DateTime.parse(json['employmentDate'] as String),
      position: json['position'] as String,
      status: json['status'] as String,
      photoUrl: json['photoUrl'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
      hasUserAccount: json['hasUserAccount'] as bool? ?? false,
      projects:
          (json['projects'] as List<dynamic>?)
              ?.map(
                (e) => EmployeeProjectAssignment.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          const <EmployeeProjectAssignment>[],
    );

Map<String, dynamic> _$EmployeeDetailToJson(_EmployeeDetail instance) =>
    <String, dynamic>{
      'id': instance.id,
      'employeeNumber': instance.employeeNumber,
      'firstName': instance.firstName,
      'lastName': instance.lastName,
      'fullName': instance.fullName,
      'phone': ?instance.phone,
      'email': ?instance.email,
      'address': ?instance.address,
      'dateOfBirth': ?instance.dateOfBirth?.toIso8601String(),
      'employmentDate': instance.employmentDate.toIso8601String(),
      'position': instance.position,
      'status': instance.status,
      'photoUrl': ?instance.photoUrl,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
      'hasUserAccount': instance.hasUserAccount,
      'projects': instance.projects.map((e) => e.toJson()).toList(),
    };

_EmployeeProjectAssignment _$EmployeeProjectAssignmentFromJson(
  Map<String, dynamic> json,
) => _EmployeeProjectAssignment(
  projectId: json['projectId'] as String,
  projectName: json['projectName'] as String,
  projectStatus: json['projectStatus'] as String,
  assignedAt: DateTime.parse(json['assignedAt'] as String),
);

Map<String, dynamic> _$EmployeeProjectAssignmentToJson(
  _EmployeeProjectAssignment instance,
) => <String, dynamic>{
  'projectId': instance.projectId,
  'projectName': instance.projectName,
  'projectStatus': instance.projectStatus,
  'assignedAt': instance.assignedAt.toIso8601String(),
};
