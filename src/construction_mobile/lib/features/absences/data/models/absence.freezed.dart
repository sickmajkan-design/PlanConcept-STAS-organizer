// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'absence.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Absence {

 String get id; String get employeeId; String get employeeName; String get type; String get status;/// `YYYY-MM-DD`.
 String get startDate;/// `YYYY-MM-DD`, inclusive.
 String get endDate;/// Calendar days covered, both ends included.
 int get dayCount; String? get reason; String? get requestedByName; String? get reviewedByName; DateTime? get reviewedAt; String? get reviewNote; DateTime get createdAt;
/// Create a copy of Absence
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AbsenceCopyWith<Absence> get copyWith => _$AbsenceCopyWithImpl<Absence>(this as Absence, _$identity);

  /// Serializes this Absence to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Absence&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.type, type) || other.type == type)&&(identical(other.status, status) || other.status == status)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.dayCount, dayCount) || other.dayCount == dayCount)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.requestedByName, requestedByName) || other.requestedByName == requestedByName)&&(identical(other.reviewedByName, reviewedByName) || other.reviewedByName == reviewedByName)&&(identical(other.reviewedAt, reviewedAt) || other.reviewedAt == reviewedAt)&&(identical(other.reviewNote, reviewNote) || other.reviewNote == reviewNote)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeId,employeeName,type,status,startDate,endDate,dayCount,reason,requestedByName,reviewedByName,reviewedAt,reviewNote,createdAt);

@override
String toString() {
  return 'Absence(id: $id, employeeId: $employeeId, employeeName: $employeeName, type: $type, status: $status, startDate: $startDate, endDate: $endDate, dayCount: $dayCount, reason: $reason, requestedByName: $requestedByName, reviewedByName: $reviewedByName, reviewedAt: $reviewedAt, reviewNote: $reviewNote, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $AbsenceCopyWith<$Res>  {
  factory $AbsenceCopyWith(Absence value, $Res Function(Absence) _then) = _$AbsenceCopyWithImpl;
@useResult
$Res call({
 String id, String employeeId, String employeeName, String type, String status, String startDate, String endDate, int dayCount, String? reason, String? requestedByName, String? reviewedByName, DateTime? reviewedAt, String? reviewNote, DateTime createdAt
});




}
/// @nodoc
class _$AbsenceCopyWithImpl<$Res>
    implements $AbsenceCopyWith<$Res> {
  _$AbsenceCopyWithImpl(this._self, this._then);

  final Absence _self;
  final $Res Function(Absence) _then;

/// Create a copy of Absence
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? employeeId = null,Object? employeeName = null,Object? type = null,Object? status = null,Object? startDate = null,Object? endDate = null,Object? dayCount = null,Object? reason = freezed,Object? requestedByName = freezed,Object? reviewedByName = freezed,Object? reviewedAt = freezed,Object? reviewNote = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as String,endDate: null == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as String,dayCount: null == dayCount ? _self.dayCount : dayCount // ignore: cast_nullable_to_non_nullable
as int,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,requestedByName: freezed == requestedByName ? _self.requestedByName : requestedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedByName: freezed == reviewedByName ? _self.reviewedByName : reviewedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedAt: freezed == reviewedAt ? _self.reviewedAt : reviewedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reviewNote: freezed == reviewNote ? _self.reviewNote : reviewNote // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [Absence].
extension AbsencePatterns on Absence {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Absence value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Absence() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Absence value)  $default,){
final _that = this;
switch (_that) {
case _Absence():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Absence value)?  $default,){
final _that = this;
switch (_that) {
case _Absence() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String employeeId,  String employeeName,  String type,  String status,  String startDate,  String endDate,  int dayCount,  String? reason,  String? requestedByName,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Absence() when $default != null:
return $default(_that.id,_that.employeeId,_that.employeeName,_that.type,_that.status,_that.startDate,_that.endDate,_that.dayCount,_that.reason,_that.requestedByName,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String employeeId,  String employeeName,  String type,  String status,  String startDate,  String endDate,  int dayCount,  String? reason,  String? requestedByName,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _Absence():
return $default(_that.id,_that.employeeId,_that.employeeName,_that.type,_that.status,_that.startDate,_that.endDate,_that.dayCount,_that.reason,_that.requestedByName,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String employeeId,  String employeeName,  String type,  String status,  String startDate,  String endDate,  int dayCount,  String? reason,  String? requestedByName,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _Absence() when $default != null:
return $default(_that.id,_that.employeeId,_that.employeeName,_that.type,_that.status,_that.startDate,_that.endDate,_that.dayCount,_that.reason,_that.requestedByName,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Absence extends Absence {
  const _Absence({required this.id, required this.employeeId, required this.employeeName, required this.type, required this.status, required this.startDate, required this.endDate, required this.dayCount, this.reason, this.requestedByName, this.reviewedByName, this.reviewedAt, this.reviewNote, required this.createdAt}): super._();
  factory _Absence.fromJson(Map<String, dynamic> json) => _$AbsenceFromJson(json);

@override final  String id;
@override final  String employeeId;
@override final  String employeeName;
@override final  String type;
@override final  String status;
/// `YYYY-MM-DD`.
@override final  String startDate;
/// `YYYY-MM-DD`, inclusive.
@override final  String endDate;
/// Calendar days covered, both ends included.
@override final  int dayCount;
@override final  String? reason;
@override final  String? requestedByName;
@override final  String? reviewedByName;
@override final  DateTime? reviewedAt;
@override final  String? reviewNote;
@override final  DateTime createdAt;

/// Create a copy of Absence
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AbsenceCopyWith<_Absence> get copyWith => __$AbsenceCopyWithImpl<_Absence>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AbsenceToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Absence&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.type, type) || other.type == type)&&(identical(other.status, status) || other.status == status)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.dayCount, dayCount) || other.dayCount == dayCount)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.requestedByName, requestedByName) || other.requestedByName == requestedByName)&&(identical(other.reviewedByName, reviewedByName) || other.reviewedByName == reviewedByName)&&(identical(other.reviewedAt, reviewedAt) || other.reviewedAt == reviewedAt)&&(identical(other.reviewNote, reviewNote) || other.reviewNote == reviewNote)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeId,employeeName,type,status,startDate,endDate,dayCount,reason,requestedByName,reviewedByName,reviewedAt,reviewNote,createdAt);

@override
String toString() {
  return 'Absence(id: $id, employeeId: $employeeId, employeeName: $employeeName, type: $type, status: $status, startDate: $startDate, endDate: $endDate, dayCount: $dayCount, reason: $reason, requestedByName: $requestedByName, reviewedByName: $reviewedByName, reviewedAt: $reviewedAt, reviewNote: $reviewNote, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$AbsenceCopyWith<$Res> implements $AbsenceCopyWith<$Res> {
  factory _$AbsenceCopyWith(_Absence value, $Res Function(_Absence) _then) = __$AbsenceCopyWithImpl;
@override @useResult
$Res call({
 String id, String employeeId, String employeeName, String type, String status, String startDate, String endDate, int dayCount, String? reason, String? requestedByName, String? reviewedByName, DateTime? reviewedAt, String? reviewNote, DateTime createdAt
});




}
/// @nodoc
class __$AbsenceCopyWithImpl<$Res>
    implements _$AbsenceCopyWith<$Res> {
  __$AbsenceCopyWithImpl(this._self, this._then);

  final _Absence _self;
  final $Res Function(_Absence) _then;

/// Create a copy of Absence
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? employeeId = null,Object? employeeName = null,Object? type = null,Object? status = null,Object? startDate = null,Object? endDate = null,Object? dayCount = null,Object? reason = freezed,Object? requestedByName = freezed,Object? reviewedByName = freezed,Object? reviewedAt = freezed,Object? reviewNote = freezed,Object? createdAt = null,}) {
  return _then(_Absence(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as String,endDate: null == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as String,dayCount: null == dayCount ? _self.dayCount : dayCount // ignore: cast_nullable_to_non_nullable
as int,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,requestedByName: freezed == requestedByName ? _self.requestedByName : requestedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedByName: freezed == reviewedByName ? _self.reviewedByName : reviewedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedAt: freezed == reviewedAt ? _self.reviewedAt : reviewedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reviewNote: freezed == reviewNote ? _self.reviewNote : reviewNote // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
