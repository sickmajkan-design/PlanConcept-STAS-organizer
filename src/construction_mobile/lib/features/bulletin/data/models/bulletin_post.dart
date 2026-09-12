import 'package:freezed_annotation/freezed_annotation.dart';

part 'bulletin_post.freezed.dart';
part 'bulletin_post.g.dart';

/// Mirrors the API's `BulletinPostDto`.
@freezed
abstract class BulletinPost with _$BulletinPost {
  const factory BulletinPost({
    required String id,
    required String title,
    required String body,
    required String createdByName,
    required DateTime createdAt,
    required int viewCount,

    /// Whether the signed-in user has already viewed this post.
    required bool viewed,
  }) = _BulletinPost;

  factory BulletinPost.fromJson(Map<String, dynamic> json) =>
      _$BulletinPostFromJson(json);
}
