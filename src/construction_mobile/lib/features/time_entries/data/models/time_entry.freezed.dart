// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'time_entry.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$TimeEntry {

 String get id; String get employeeId; String get employeeName; String? get projectId; String? get projectName; DateTime get startedAt; DateTime? get endedAt; int get breakMinutes;/// Null while the shift is still running.
 int? get workedMinutes; String get workType; String get status; String? get note; double? get startLatitude; double? get startLongitude; double? get endLatitude; double? get endLongitude; String? get reviewedByName; DateTime? get reviewedAt; String? get reviewNote; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of TimeEntry
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$TimeEntryCopyWith<TimeEntry> get copyWith => _$TimeEntryCopyWithImpl<TimeEntry>(this as TimeEntry, _$identity);

  /// Serializes this TimeEntry to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is TimeEntry&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.startedAt, startedAt) || other.startedAt == startedAt)&&(identical(other.endedAt, endedAt) || other.endedAt == endedAt)&&(identical(other.breakMinutes, breakMinutes) || other.breakMinutes == breakMinutes)&&(identical(other.workedMinutes, workedMinutes) || other.workedMinutes == workedMinutes)&&(identical(other.workType, workType) || other.workType == workType)&&(identical(other.status, status) || other.status == status)&&(identical(other.note, note) || other.note == note)&&(identical(other.startLatitude, startLatitude) || other.startLatitude == startLatitude)&&(identical(other.startLongitude, startLongitude) || other.startLongitude == startLongitude)&&(identical(other.endLatitude, endLatitude) || other.endLatitude == endLatitude)&&(identical(other.endLongitude, endLongitude) || other.endLongitude == endLongitude)&&(identical(other.reviewedByName, reviewedByName) || other.reviewedByName == reviewedByName)&&(identical(other.reviewedAt, reviewedAt) || other.reviewedAt == reviewedAt)&&(identical(other.reviewNote, reviewNote) || other.reviewNote == reviewNote)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,employeeId,employeeName,projectId,projectName,startedAt,endedAt,breakMinutes,workedMinutes,workType,status,note,startLatitude,startLongitude,endLatitude,endLongitude,reviewedByName,reviewedAt,reviewNote,createdAt,updatedAt]);

@override
String toString() {
  return 'TimeEntry(id: $id, employeeId: $employeeId, employeeName: $employeeName, projectId: $projectId, projectName: $projectName, startedAt: $startedAt, endedAt: $endedAt, breakMinutes: $breakMinutes, workedMinutes: $workedMinutes, workType: $workType, status: $status, note: $note, startLatitude: $startLatitude, startLongitude: $startLongitude, endLatitude: $endLatitude, endLongitude: $endLongitude, reviewedByName: $reviewedByName, reviewedAt: $reviewedAt, reviewNote: $reviewNote, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $TimeEntryCopyWith<$Res>  {
  factory $TimeEntryCopyWith(TimeEntry value, $Res Function(TimeEntry) _then) = _$TimeEntryCopyWithImpl;
@useResult
$Res call({
 String id, String employeeId, String employeeName, String? projectId, String? projectName, DateTime startedAt, DateTime? endedAt, int breakMinutes, int? workedMinutes, String workType, String status, String? note, double? startLatitude, double? startLongitude, double? endLatitude, double? endLongitude, String? reviewedByName, DateTime? reviewedAt, String? reviewNote, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$TimeEntryCopyWithImpl<$Res>
    implements $TimeEntryCopyWith<$Res> {
  _$TimeEntryCopyWithImpl(this._self, this._then);

  final TimeEntry _self;
  final $Res Function(TimeEntry) _then;

/// Create a copy of TimeEntry
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? employeeId = null,Object? employeeName = null,Object? projectId = freezed,Object? projectName = freezed,Object? startedAt = null,Object? endedAt = freezed,Object? breakMinutes = null,Object? workedMinutes = freezed,Object? workType = null,Object? status = null,Object? note = freezed,Object? startLatitude = freezed,Object? startLongitude = freezed,Object? endLatitude = freezed,Object? endLongitude = freezed,Object? reviewedByName = freezed,Object? reviewedAt = freezed,Object? reviewNote = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,startedAt: null == startedAt ? _self.startedAt : startedAt // ignore: cast_nullable_to_non_nullable
as DateTime,endedAt: freezed == endedAt ? _self.endedAt : endedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,breakMinutes: null == breakMinutes ? _self.breakMinutes : breakMinutes // ignore: cast_nullable_to_non_nullable
as int,workedMinutes: freezed == workedMinutes ? _self.workedMinutes : workedMinutes // ignore: cast_nullable_to_non_nullable
as int?,workType: null == workType ? _self.workType : workType // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,startLatitude: freezed == startLatitude ? _self.startLatitude : startLatitude // ignore: cast_nullable_to_non_nullable
as double?,startLongitude: freezed == startLongitude ? _self.startLongitude : startLongitude // ignore: cast_nullable_to_non_nullable
as double?,endLatitude: freezed == endLatitude ? _self.endLatitude : endLatitude // ignore: cast_nullable_to_non_nullable
as double?,endLongitude: freezed == endLongitude ? _self.endLongitude : endLongitude // ignore: cast_nullable_to_non_nullable
as double?,reviewedByName: freezed == reviewedByName ? _self.reviewedByName : reviewedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedAt: freezed == reviewedAt ? _self.reviewedAt : reviewedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reviewNote: freezed == reviewNote ? _self.reviewNote : reviewNote // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [TimeEntry].
extension TimeEntryPatterns on TimeEntry {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _TimeEntry value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _TimeEntry() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _TimeEntry value)  $default,){
final _that = this;
switch (_that) {
case _TimeEntry():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _TimeEntry value)?  $default,){
final _that = this;
switch (_that) {
case _TimeEntry() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String employeeId,  String employeeName,  String? projectId,  String? projectName,  DateTime startedAt,  DateTime? endedAt,  int breakMinutes,  int? workedMinutes,  String workType,  String status,  String? note,  double? startLatitude,  double? startLongitude,  double? endLatitude,  double? endLongitude,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _TimeEntry() when $default != null:
return $default(_that.id,_that.employeeId,_that.employeeName,_that.projectId,_that.projectName,_that.startedAt,_that.endedAt,_that.breakMinutes,_that.workedMinutes,_that.workType,_that.status,_that.note,_that.startLatitude,_that.startLongitude,_that.endLatitude,_that.endLongitude,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String employeeId,  String employeeName,  String? projectId,  String? projectName,  DateTime startedAt,  DateTime? endedAt,  int breakMinutes,  int? workedMinutes,  String workType,  String status,  String? note,  double? startLatitude,  double? startLongitude,  double? endLatitude,  double? endLongitude,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _TimeEntry():
return $default(_that.id,_that.employeeId,_that.employeeName,_that.projectId,_that.projectName,_that.startedAt,_that.endedAt,_that.breakMinutes,_that.workedMinutes,_that.workType,_that.status,_that.note,_that.startLatitude,_that.startLongitude,_that.endLatitude,_that.endLongitude,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String employeeId,  String employeeName,  String? projectId,  String? projectName,  DateTime startedAt,  DateTime? endedAt,  int breakMinutes,  int? workedMinutes,  String workType,  String status,  String? note,  double? startLatitude,  double? startLongitude,  double? endLatitude,  double? endLongitude,  String? reviewedByName,  DateTime? reviewedAt,  String? reviewNote,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _TimeEntry() when $default != null:
return $default(_that.id,_that.employeeId,_that.employeeName,_that.projectId,_that.projectName,_that.startedAt,_that.endedAt,_that.breakMinutes,_that.workedMinutes,_that.workType,_that.status,_that.note,_that.startLatitude,_that.startLongitude,_that.endLatitude,_that.endLongitude,_that.reviewedByName,_that.reviewedAt,_that.reviewNote,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _TimeEntry extends TimeEntry {
  const _TimeEntry({required this.id, required this.employeeId, required this.employeeName, this.projectId, this.projectName, required this.startedAt, this.endedAt, required this.breakMinutes, this.workedMinutes, required this.workType, required this.status, this.note, this.startLatitude, this.startLongitude, this.endLatitude, this.endLongitude, this.reviewedByName, this.reviewedAt, this.reviewNote, required this.createdAt, this.updatedAt}): super._();
  factory _TimeEntry.fromJson(Map<String, dynamic> json) => _$TimeEntryFromJson(json);

@override final  String id;
@override final  String employeeId;
@override final  String employeeName;
@override final  String? projectId;
@override final  String? projectName;
@override final  DateTime startedAt;
@override final  DateTime? endedAt;
@override final  int breakMinutes;
/// Null while the shift is still running.
@override final  int? workedMinutes;
@override final  String workType;
@override final  String status;
@override final  String? note;
@override final  double? startLatitude;
@override final  double? startLongitude;
@override final  double? endLatitude;
@override final  double? endLongitude;
@override final  String? reviewedByName;
@override final  DateTime? reviewedAt;
@override final  String? reviewNote;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of TimeEntry
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$TimeEntryCopyWith<_TimeEntry> get copyWith => __$TimeEntryCopyWithImpl<_TimeEntry>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$TimeEntryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _TimeEntry&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.startedAt, startedAt) || other.startedAt == startedAt)&&(identical(other.endedAt, endedAt) || other.endedAt == endedAt)&&(identical(other.breakMinutes, breakMinutes) || other.breakMinutes == breakMinutes)&&(identical(other.workedMinutes, workedMinutes) || other.workedMinutes == workedMinutes)&&(identical(other.workType, workType) || other.workType == workType)&&(identical(other.status, status) || other.status == status)&&(identical(other.note, note) || other.note == note)&&(identical(other.startLatitude, startLatitude) || other.startLatitude == startLatitude)&&(identical(other.startLongitude, startLongitude) || other.startLongitude == startLongitude)&&(identical(other.endLatitude, endLatitude) || other.endLatitude == endLatitude)&&(identical(other.endLongitude, endLongitude) || other.endLongitude == endLongitude)&&(identical(other.reviewedByName, reviewedByName) || other.reviewedByName == reviewedByName)&&(identical(other.reviewedAt, reviewedAt) || other.reviewedAt == reviewedAt)&&(identical(other.reviewNote, reviewNote) || other.reviewNote == reviewNote)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,employeeId,employeeName,projectId,projectName,startedAt,endedAt,breakMinutes,workedMinutes,workType,status,note,startLatitude,startLongitude,endLatitude,endLongitude,reviewedByName,reviewedAt,reviewNote,createdAt,updatedAt]);

@override
String toString() {
  return 'TimeEntry(id: $id, employeeId: $employeeId, employeeName: $employeeName, projectId: $projectId, projectName: $projectName, startedAt: $startedAt, endedAt: $endedAt, breakMinutes: $breakMinutes, workedMinutes: $workedMinutes, workType: $workType, status: $status, note: $note, startLatitude: $startLatitude, startLongitude: $startLongitude, endLatitude: $endLatitude, endLongitude: $endLongitude, reviewedByName: $reviewedByName, reviewedAt: $reviewedAt, reviewNote: $reviewNote, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$TimeEntryCopyWith<$Res> implements $TimeEntryCopyWith<$Res> {
  factory _$TimeEntryCopyWith(_TimeEntry value, $Res Function(_TimeEntry) _then) = __$TimeEntryCopyWithImpl;
@override @useResult
$Res call({
 String id, String employeeId, String employeeName, String? projectId, String? projectName, DateTime startedAt, DateTime? endedAt, int breakMinutes, int? workedMinutes, String workType, String status, String? note, double? startLatitude, double? startLongitude, double? endLatitude, double? endLongitude, String? reviewedByName, DateTime? reviewedAt, String? reviewNote, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$TimeEntryCopyWithImpl<$Res>
    implements _$TimeEntryCopyWith<$Res> {
  __$TimeEntryCopyWithImpl(this._self, this._then);

  final _TimeEntry _self;
  final $Res Function(_TimeEntry) _then;

/// Create a copy of TimeEntry
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? employeeId = null,Object? employeeName = null,Object? projectId = freezed,Object? projectName = freezed,Object? startedAt = null,Object? endedAt = freezed,Object? breakMinutes = null,Object? workedMinutes = freezed,Object? workType = null,Object? status = null,Object? note = freezed,Object? startLatitude = freezed,Object? startLongitude = freezed,Object? endLatitude = freezed,Object? endLongitude = freezed,Object? reviewedByName = freezed,Object? reviewedAt = freezed,Object? reviewNote = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_TimeEntry(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,startedAt: null == startedAt ? _self.startedAt : startedAt // ignore: cast_nullable_to_non_nullable
as DateTime,endedAt: freezed == endedAt ? _self.endedAt : endedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,breakMinutes: null == breakMinutes ? _self.breakMinutes : breakMinutes // ignore: cast_nullable_to_non_nullable
as int,workedMinutes: freezed == workedMinutes ? _self.workedMinutes : workedMinutes // ignore: cast_nullable_to_non_nullable
as int?,workType: null == workType ? _self.workType : workType // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,startLatitude: freezed == startLatitude ? _self.startLatitude : startLatitude // ignore: cast_nullable_to_non_nullable
as double?,startLongitude: freezed == startLongitude ? _self.startLongitude : startLongitude // ignore: cast_nullable_to_non_nullable
as double?,endLatitude: freezed == endLatitude ? _self.endLatitude : endLatitude // ignore: cast_nullable_to_non_nullable
as double?,endLongitude: freezed == endLongitude ? _self.endLongitude : endLongitude // ignore: cast_nullable_to_non_nullable
as double?,reviewedByName: freezed == reviewedByName ? _self.reviewedByName : reviewedByName // ignore: cast_nullable_to_non_nullable
as String?,reviewedAt: freezed == reviewedAt ? _self.reviewedAt : reviewedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reviewNote: freezed == reviewNote ? _self.reviewNote : reviewNote // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
