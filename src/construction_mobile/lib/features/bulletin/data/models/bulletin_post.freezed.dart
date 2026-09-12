// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'bulletin_post.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$BulletinPost {

 String get id; String get title; String get body; String get createdByName; DateTime get createdAt; int get viewCount;/// Whether the signed-in user has already viewed this post.
 bool get viewed;
/// Create a copy of BulletinPost
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BulletinPostCopyWith<BulletinPost> get copyWith => _$BulletinPostCopyWithImpl<BulletinPost>(this as BulletinPost, _$identity);

  /// Serializes this BulletinPost to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BulletinPost&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.body, body) || other.body == body)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.viewed, viewed) || other.viewed == viewed));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,body,createdByName,createdAt,viewCount,viewed);

@override
String toString() {
  return 'BulletinPost(id: $id, title: $title, body: $body, createdByName: $createdByName, createdAt: $createdAt, viewCount: $viewCount, viewed: $viewed)';
}


}

/// @nodoc
abstract mixin class $BulletinPostCopyWith<$Res>  {
  factory $BulletinPostCopyWith(BulletinPost value, $Res Function(BulletinPost) _then) = _$BulletinPostCopyWithImpl;
@useResult
$Res call({
 String id, String title, String body, String createdByName, DateTime createdAt, int viewCount, bool viewed
});




}
/// @nodoc
class _$BulletinPostCopyWithImpl<$Res>
    implements $BulletinPostCopyWith<$Res> {
  _$BulletinPostCopyWithImpl(this._self, this._then);

  final BulletinPost _self;
  final $Res Function(BulletinPost) _then;

/// Create a copy of BulletinPost
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? body = null,Object? createdByName = null,Object? createdAt = null,Object? viewCount = null,Object? viewed = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,createdByName: null == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,viewed: null == viewed ? _self.viewed : viewed // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [BulletinPost].
extension BulletinPostPatterns on BulletinPost {
/// A variant of `map` that fallback to returning `orElse`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BulletinPost value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BulletinPost() when $default != null:
return $default(_that);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// Callbacks receives the raw object, upcasted.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case final Subclass2 value:
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BulletinPost value)  $default,){
final _that = this;
switch (_that) {
case _BulletinPost():
return $default(_that);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `map` that fallback to returning `null`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BulletinPost value)?  $default,){
final _that = this;
switch (_that) {
case _BulletinPost() when $default != null:
return $default(_that);case _:
  return null;

}
}
/// A variant of `when` that fallback to an `orElse` callback.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String body,  String createdByName,  DateTime createdAt,  int viewCount,  bool viewed)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BulletinPost() when $default != null:
return $default(_that.id,_that.title,_that.body,_that.createdByName,_that.createdAt,_that.viewCount,_that.viewed);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// As opposed to `map`, this offers destructuring.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case Subclass2(:final field2):
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String body,  String createdByName,  DateTime createdAt,  int viewCount,  bool viewed)  $default,) {final _that = this;
switch (_that) {
case _BulletinPost():
return $default(_that.id,_that.title,_that.body,_that.createdByName,_that.createdAt,_that.viewCount,_that.viewed);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `when` that fallback to returning `null`
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String body,  String createdByName,  DateTime createdAt,  int viewCount,  bool viewed)?  $default,) {final _that = this;
switch (_that) {
case _BulletinPost() when $default != null:
return $default(_that.id,_that.title,_that.body,_that.createdByName,_that.createdAt,_that.viewCount,_that.viewed);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BulletinPost implements BulletinPost {
  const _BulletinPost({required this.id, required this.title, required this.body, required this.createdByName, required this.createdAt, required this.viewCount, required this.viewed});
  factory _BulletinPost.fromJson(Map<String, dynamic> json) => _$BulletinPostFromJson(json);

@override final  String id;
@override final  String title;
@override final  String body;
@override final  String createdByName;
@override final  DateTime createdAt;
@override final  int viewCount;
/// Whether the signed-in user has already viewed this post.
@override final  bool viewed;

/// Create a copy of BulletinPost
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BulletinPostCopyWith<_BulletinPost> get copyWith => __$BulletinPostCopyWithImpl<_BulletinPost>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BulletinPostToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _BulletinPost&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.body, body) || other.body == body)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.viewed, viewed) || other.viewed == viewed));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,body,createdByName,createdAt,viewCount,viewed);

@override
String toString() {
  return 'BulletinPost(id: $id, title: $title, body: $body, createdByName: $createdByName, createdAt: $createdAt, viewCount: $viewCount, viewed: $viewed)';
}


}

/// @nodoc
abstract mixin class _$BulletinPostCopyWith<$Res> implements $BulletinPostCopyWith<$Res> {
  factory _$BulletinPostCopyWith(_BulletinPost value, $Res Function(_BulletinPost) _then) = __$BulletinPostCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String body, String createdByName, DateTime createdAt, int viewCount, bool viewed
});




}
/// @nodoc
class __$BulletinPostCopyWithImpl<$Res>
    implements _$BulletinPostCopyWith<$Res> {
  __$BulletinPostCopyWithImpl(this._self, this._then);

  final _BulletinPost _self;
  final $Res Function(_BulletinPost) _then;

/// Create a copy of BulletinPost
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? body = null,Object? createdByName = null,Object? createdAt = null,Object? viewCount = null,Object? viewed = null,}) {
  return _then(_BulletinPost(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,createdByName: null == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,viewed: null == viewed ? _self.viewed : viewed // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
