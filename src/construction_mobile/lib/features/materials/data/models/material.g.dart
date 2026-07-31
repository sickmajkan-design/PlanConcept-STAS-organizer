// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'material.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_MaterialItem _$MaterialItemFromJson(Map<String, dynamic> json) =>
    _MaterialItem(
      id: json['id'] as String,
      name: json['name'] as String,
      unit: json['unit'] as String,
      quantity: (json['quantity'] as num).toDouble(),
      warehouse: json['warehouse'] as String?,
      projectId: json['projectId'] as String?,
      projectName: json['projectName'] as String?,
      lastUpdated: DateTime.parse(json['lastUpdated'] as String),
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$MaterialItemToJson(_MaterialItem instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'unit': instance.unit,
      'quantity': instance.quantity,
      'warehouse': ?instance.warehouse,
      'projectId': ?instance.projectId,
      'projectName': ?instance.projectName,
      'lastUpdated': instance.lastUpdated.toIso8601String(),
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
    };
