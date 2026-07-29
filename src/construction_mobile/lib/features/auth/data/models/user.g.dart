// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_User _$UserFromJson(Map<String, dynamic> json) => _User(
  id: json['id'] as String,
  email: json['email'] as String,
  role: json['role'] as String,
  employeeId: json['employeeId'] as String?,
  firstName: json['firstName'] as String?,
  lastName: json['lastName'] as String?,
  lastLoginAt: json['lastLoginAt'] == null
      ? null
      : DateTime.parse(json['lastLoginAt'] as String),
);

Map<String, dynamic> _$UserToJson(_User instance) => <String, dynamic>{
  'id': instance.id,
  'email': instance.email,
  'role': instance.role,
  'employeeId': ?instance.employeeId,
  'firstName': ?instance.firstName,
  'lastName': ?instance.lastName,
  'lastLoginAt': ?instance.lastLoginAt?.toIso8601String(),
};
