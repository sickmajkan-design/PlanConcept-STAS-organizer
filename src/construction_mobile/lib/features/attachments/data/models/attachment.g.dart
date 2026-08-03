// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'attachment.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Attachment _$AttachmentFromJson(Map<String, dynamic> json) => _Attachment(
  id: json['id'] as String,
  fileName: json['fileName'] as String,
  contentType: json['contentType'] as String,
  sizeBytes: (json['sizeBytes'] as num).toInt(),
  category: json['category'] as String,
  description: json['description'] as String?,
  expiresAt: json['expiresAt'] as String?,
  ownerType: json['ownerType'] as String,
  ownerId: json['ownerId'] as String,
  ownerName: json['ownerName'] as String?,
  uploadedByName: json['uploadedByName'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$AttachmentToJson(_Attachment instance) =>
    <String, dynamic>{
      'id': instance.id,
      'fileName': instance.fileName,
      'contentType': instance.contentType,
      'sizeBytes': instance.sizeBytes,
      'category': instance.category,
      'description': ?instance.description,
      'expiresAt': ?instance.expiresAt,
      'ownerType': instance.ownerType,
      'ownerId': instance.ownerId,
      'ownerName': ?instance.ownerName,
      'uploadedByName': ?instance.uploadedByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
