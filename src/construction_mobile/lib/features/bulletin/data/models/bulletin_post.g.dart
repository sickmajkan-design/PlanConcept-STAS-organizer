// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'bulletin_post.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_BulletinPost _$BulletinPostFromJson(Map<String, dynamic> json) =>
    _BulletinPost(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String,
      createdByName: json['createdByName'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      viewCount: (json['viewCount'] as num).toInt(),
      viewed: json['viewed'] as bool,
    );

Map<String, dynamic> _$BulletinPostToJson(_BulletinPost instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'body': instance.body,
      'createdByName': instance.createdByName,
      'createdAt': instance.createdAt.toIso8601String(),
      'viewCount': instance.viewCount,
      'viewed': instance.viewed,
    };
