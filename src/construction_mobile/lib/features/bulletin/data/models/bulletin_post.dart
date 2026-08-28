/// Mirrors the API's `BulletinPostDto`.
///
/// A plain class rather than `@freezed` like every other model in this
/// codebase: this feature was added in an environment with no Flutter/Dart
/// toolchain available to run `build_runner`, so it cannot generate the
/// `.freezed.dart`/`.g.dart` companions a `@freezed` class needs to compile.
/// Once someone with the toolchain touches this file, converting it to
/// `@freezed` to match the rest of the app is reasonable — nothing here
/// depends on staying a plain class.
class BulletinPost {
  const BulletinPost({
    required this.id,
    required this.title,
    required this.body,
    required this.createdByName,
    required this.createdAt,
    required this.viewCount,
    required this.viewed,
  });

  final String id;
  final String title;
  final String body;
  final String createdByName;
  final DateTime createdAt;
  final int viewCount;

  /// Whether the signed-in user has already viewed this post.
  final bool viewed;

  factory BulletinPost.fromJson(Map<String, dynamic> json) {
    return BulletinPost(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String,
      createdByName: json['createdByName'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      viewCount: json['viewCount'] as int,
      viewed: json['viewed'] as bool,
    );
  }
}
