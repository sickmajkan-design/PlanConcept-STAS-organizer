// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'schedule.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Schedule {

 String get from; String get to; List<ScheduleRow> get rows;
/// Create a copy of Schedule
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ScheduleCopyWith<Schedule> get copyWith => _$ScheduleCopyWithImpl<Schedule>(this as Schedule, _$identity);

  /// Serializes this Schedule to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Schedule&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to)&&const DeepCollectionEquality().equals(other.rows, rows));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,from,to,const DeepCollectionEquality().hash(rows));

@override
String toString() {
  return 'Schedule(from: $from, to: $to, rows: $rows)';
}


}

/// @nodoc
abstract mixin class $ScheduleCopyWith<$Res>  {
  factory $ScheduleCopyWith(Schedule value, $Res Function(Schedule) _then) = _$ScheduleCopyWithImpl;
@useResult
$Res call({
 String from, String to, List<ScheduleRow> rows
});




}
/// @nodoc
class _$ScheduleCopyWithImpl<$Res>
    implements $ScheduleCopyWith<$Res> {
  _$ScheduleCopyWithImpl(this._self, this._then);

  final Schedule _self;
  final $Res Function(Schedule) _then;

/// Create a copy of Schedule
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? from = null,Object? to = null,Object? rows = null,}) {
  return _then(_self.copyWith(
from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,rows: null == rows ? _self.rows : rows // ignore: cast_nullable_to_non_nullable
as List<ScheduleRow>,
  ));
}

}


/// Adds pattern-matching-related methods to [Schedule].
extension SchedulePatterns on Schedule {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Schedule value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Schedule() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Schedule value)  $default,){
final _that = this;
switch (_that) {
case _Schedule():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Schedule value)?  $default,){
final _that = this;
switch (_that) {
case _Schedule() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String from,  String to,  List<ScheduleRow> rows)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Schedule() when $default != null:
return $default(_that.from,_that.to,_that.rows);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String from,  String to,  List<ScheduleRow> rows)  $default,) {final _that = this;
switch (_that) {
case _Schedule():
return $default(_that.from,_that.to,_that.rows);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String from,  String to,  List<ScheduleRow> rows)?  $default,) {final _that = this;
switch (_that) {
case _Schedule() when $default != null:
return $default(_that.from,_that.to,_that.rows);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Schedule extends Schedule {
  const _Schedule({required this.from, required this.to, final  List<ScheduleRow> rows = const <ScheduleRow>[]}): _rows = rows,super._();
  factory _Schedule.fromJson(Map<String, dynamic> json) => _$ScheduleFromJson(json);

@override final  String from;
@override final  String to;
 final  List<ScheduleRow> _rows;
@override@JsonKey() List<ScheduleRow> get rows {
  if (_rows is EqualUnmodifiableListView) return _rows;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_rows);
}


/// Create a copy of Schedule
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ScheduleCopyWith<_Schedule> get copyWith => __$ScheduleCopyWithImpl<_Schedule>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ScheduleToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Schedule&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to)&&const DeepCollectionEquality().equals(other._rows, _rows));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,from,to,const DeepCollectionEquality().hash(_rows));

@override
String toString() {
  return 'Schedule(from: $from, to: $to, rows: $rows)';
}


}

/// @nodoc
abstract mixin class _$ScheduleCopyWith<$Res> implements $ScheduleCopyWith<$Res> {
  factory _$ScheduleCopyWith(_Schedule value, $Res Function(_Schedule) _then) = __$ScheduleCopyWithImpl;
@override @useResult
$Res call({
 String from, String to, List<ScheduleRow> rows
});




}
/// @nodoc
class __$ScheduleCopyWithImpl<$Res>
    implements _$ScheduleCopyWith<$Res> {
  __$ScheduleCopyWithImpl(this._self, this._then);

  final _Schedule _self;
  final $Res Function(_Schedule) _then;

/// Create a copy of Schedule
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? from = null,Object? to = null,Object? rows = null,}) {
  return _then(_Schedule(
from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,rows: null == rows ? _self._rows : rows // ignore: cast_nullable_to_non_nullable
as List<ScheduleRow>,
  ));
}


}


/// @nodoc
mixin _$ScheduleRow {

 String get employeeId; String get employeeName; String get position; List<ScheduleAssignment> get assignments;/// Granted leave only. A request nobody has answered is not on the board.
 List<ScheduleAbsence> get absences;
/// Create a copy of ScheduleRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ScheduleRowCopyWith<ScheduleRow> get copyWith => _$ScheduleRowCopyWithImpl<ScheduleRow>(this as ScheduleRow, _$identity);

  /// Serializes this ScheduleRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ScheduleRow&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.position, position) || other.position == position)&&const DeepCollectionEquality().equals(other.assignments, assignments)&&const DeepCollectionEquality().equals(other.absences, absences));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,employeeId,employeeName,position,const DeepCollectionEquality().hash(assignments),const DeepCollectionEquality().hash(absences));

@override
String toString() {
  return 'ScheduleRow(employeeId: $employeeId, employeeName: $employeeName, position: $position, assignments: $assignments, absences: $absences)';
}


}

/// @nodoc
abstract mixin class $ScheduleRowCopyWith<$Res>  {
  factory $ScheduleRowCopyWith(ScheduleRow value, $Res Function(ScheduleRow) _then) = _$ScheduleRowCopyWithImpl;
@useResult
$Res call({
 String employeeId, String employeeName, String position, List<ScheduleAssignment> assignments, List<ScheduleAbsence> absences
});




}
/// @nodoc
class _$ScheduleRowCopyWithImpl<$Res>
    implements $ScheduleRowCopyWith<$Res> {
  _$ScheduleRowCopyWithImpl(this._self, this._then);

  final ScheduleRow _self;
  final $Res Function(ScheduleRow) _then;

/// Create a copy of ScheduleRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? employeeId = null,Object? employeeName = null,Object? position = null,Object? assignments = null,Object? absences = null,}) {
  return _then(_self.copyWith(
employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,assignments: null == assignments ? _self.assignments : assignments // ignore: cast_nullable_to_non_nullable
as List<ScheduleAssignment>,absences: null == absences ? _self.absences : absences // ignore: cast_nullable_to_non_nullable
as List<ScheduleAbsence>,
  ));
}

}


/// Adds pattern-matching-related methods to [ScheduleRow].
extension ScheduleRowPatterns on ScheduleRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ScheduleRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ScheduleRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ScheduleRow value)  $default,){
final _that = this;
switch (_that) {
case _ScheduleRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ScheduleRow value)?  $default,){
final _that = this;
switch (_that) {
case _ScheduleRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String employeeId,  String employeeName,  String position,  List<ScheduleAssignment> assignments,  List<ScheduleAbsence> absences)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ScheduleRow() when $default != null:
return $default(_that.employeeId,_that.employeeName,_that.position,_that.assignments,_that.absences);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String employeeId,  String employeeName,  String position,  List<ScheduleAssignment> assignments,  List<ScheduleAbsence> absences)  $default,) {final _that = this;
switch (_that) {
case _ScheduleRow():
return $default(_that.employeeId,_that.employeeName,_that.position,_that.assignments,_that.absences);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String employeeId,  String employeeName,  String position,  List<ScheduleAssignment> assignments,  List<ScheduleAbsence> absences)?  $default,) {final _that = this;
switch (_that) {
case _ScheduleRow() when $default != null:
return $default(_that.employeeId,_that.employeeName,_that.position,_that.assignments,_that.absences);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ScheduleRow extends ScheduleRow {
  const _ScheduleRow({required this.employeeId, required this.employeeName, required this.position, final  List<ScheduleAssignment> assignments = const <ScheduleAssignment>[], final  List<ScheduleAbsence> absences = const <ScheduleAbsence>[]}): _assignments = assignments,_absences = absences,super._();
  factory _ScheduleRow.fromJson(Map<String, dynamic> json) => _$ScheduleRowFromJson(json);

@override final  String employeeId;
@override final  String employeeName;
@override final  String position;
 final  List<ScheduleAssignment> _assignments;
@override@JsonKey() List<ScheduleAssignment> get assignments {
  if (_assignments is EqualUnmodifiableListView) return _assignments;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_assignments);
}

/// Granted leave only. A request nobody has answered is not on the board.
 final  List<ScheduleAbsence> _absences;
/// Granted leave only. A request nobody has answered is not on the board.
@override@JsonKey() List<ScheduleAbsence> get absences {
  if (_absences is EqualUnmodifiableListView) return _absences;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_absences);
}


/// Create a copy of ScheduleRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ScheduleRowCopyWith<_ScheduleRow> get copyWith => __$ScheduleRowCopyWithImpl<_ScheduleRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ScheduleRowToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ScheduleRow&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.position, position) || other.position == position)&&const DeepCollectionEquality().equals(other._assignments, _assignments)&&const DeepCollectionEquality().equals(other._absences, _absences));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,employeeId,employeeName,position,const DeepCollectionEquality().hash(_assignments),const DeepCollectionEquality().hash(_absences));

@override
String toString() {
  return 'ScheduleRow(employeeId: $employeeId, employeeName: $employeeName, position: $position, assignments: $assignments, absences: $absences)';
}


}

/// @nodoc
abstract mixin class _$ScheduleRowCopyWith<$Res> implements $ScheduleRowCopyWith<$Res> {
  factory _$ScheduleRowCopyWith(_ScheduleRow value, $Res Function(_ScheduleRow) _then) = __$ScheduleRowCopyWithImpl;
@override @useResult
$Res call({
 String employeeId, String employeeName, String position, List<ScheduleAssignment> assignments, List<ScheduleAbsence> absences
});




}
/// @nodoc
class __$ScheduleRowCopyWithImpl<$Res>
    implements _$ScheduleRowCopyWith<$Res> {
  __$ScheduleRowCopyWithImpl(this._self, this._then);

  final _ScheduleRow _self;
  final $Res Function(_ScheduleRow) _then;

/// Create a copy of ScheduleRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? employeeId = null,Object? employeeName = null,Object? position = null,Object? assignments = null,Object? absences = null,}) {
  return _then(_ScheduleRow(
employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeName: null == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,assignments: null == assignments ? _self._assignments : assignments // ignore: cast_nullable_to_non_nullable
as List<ScheduleAssignment>,absences: null == absences ? _self._absences : absences // ignore: cast_nullable_to_non_nullable
as List<ScheduleAbsence>,
  ));
}


}


/// @nodoc
mixin _$ScheduleAssignment {

 String get id; String get projectId; String get projectName; String get from; String get to;/// True when the posting runs on past the end of the window.
 bool get continuesAfter;
/// Create a copy of ScheduleAssignment
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ScheduleAssignmentCopyWith<ScheduleAssignment> get copyWith => _$ScheduleAssignmentCopyWithImpl<ScheduleAssignment>(this as ScheduleAssignment, _$identity);

  /// Serializes this ScheduleAssignment to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ScheduleAssignment&&(identical(other.id, id) || other.id == id)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to)&&(identical(other.continuesAfter, continuesAfter) || other.continuesAfter == continuesAfter));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,projectId,projectName,from,to,continuesAfter);

@override
String toString() {
  return 'ScheduleAssignment(id: $id, projectId: $projectId, projectName: $projectName, from: $from, to: $to, continuesAfter: $continuesAfter)';
}


}

/// @nodoc
abstract mixin class $ScheduleAssignmentCopyWith<$Res>  {
  factory $ScheduleAssignmentCopyWith(ScheduleAssignment value, $Res Function(ScheduleAssignment) _then) = _$ScheduleAssignmentCopyWithImpl;
@useResult
$Res call({
 String id, String projectId, String projectName, String from, String to, bool continuesAfter
});




}
/// @nodoc
class _$ScheduleAssignmentCopyWithImpl<$Res>
    implements $ScheduleAssignmentCopyWith<$Res> {
  _$ScheduleAssignmentCopyWithImpl(this._self, this._then);

  final ScheduleAssignment _self;
  final $Res Function(ScheduleAssignment) _then;

/// Create a copy of ScheduleAssignment
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? projectId = null,Object? projectName = null,Object? from = null,Object? to = null,Object? continuesAfter = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,continuesAfter: null == continuesAfter ? _self.continuesAfter : continuesAfter // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [ScheduleAssignment].
extension ScheduleAssignmentPatterns on ScheduleAssignment {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ScheduleAssignment value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ScheduleAssignment() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ScheduleAssignment value)  $default,){
final _that = this;
switch (_that) {
case _ScheduleAssignment():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ScheduleAssignment value)?  $default,){
final _that = this;
switch (_that) {
case _ScheduleAssignment() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String projectId,  String projectName,  String from,  String to,  bool continuesAfter)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ScheduleAssignment() when $default != null:
return $default(_that.id,_that.projectId,_that.projectName,_that.from,_that.to,_that.continuesAfter);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String projectId,  String projectName,  String from,  String to,  bool continuesAfter)  $default,) {final _that = this;
switch (_that) {
case _ScheduleAssignment():
return $default(_that.id,_that.projectId,_that.projectName,_that.from,_that.to,_that.continuesAfter);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String projectId,  String projectName,  String from,  String to,  bool continuesAfter)?  $default,) {final _that = this;
switch (_that) {
case _ScheduleAssignment() when $default != null:
return $default(_that.id,_that.projectId,_that.projectName,_that.from,_that.to,_that.continuesAfter);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ScheduleAssignment extends ScheduleAssignment {
  const _ScheduleAssignment({required this.id, required this.projectId, required this.projectName, required this.from, required this.to, this.continuesAfter = false}): super._();
  factory _ScheduleAssignment.fromJson(Map<String, dynamic> json) => _$ScheduleAssignmentFromJson(json);

@override final  String id;
@override final  String projectId;
@override final  String projectName;
@override final  String from;
@override final  String to;
/// True when the posting runs on past the end of the window.
@override@JsonKey() final  bool continuesAfter;

/// Create a copy of ScheduleAssignment
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ScheduleAssignmentCopyWith<_ScheduleAssignment> get copyWith => __$ScheduleAssignmentCopyWithImpl<_ScheduleAssignment>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ScheduleAssignmentToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ScheduleAssignment&&(identical(other.id, id) || other.id == id)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to)&&(identical(other.continuesAfter, continuesAfter) || other.continuesAfter == continuesAfter));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,projectId,projectName,from,to,continuesAfter);

@override
String toString() {
  return 'ScheduleAssignment(id: $id, projectId: $projectId, projectName: $projectName, from: $from, to: $to, continuesAfter: $continuesAfter)';
}


}

/// @nodoc
abstract mixin class _$ScheduleAssignmentCopyWith<$Res> implements $ScheduleAssignmentCopyWith<$Res> {
  factory _$ScheduleAssignmentCopyWith(_ScheduleAssignment value, $Res Function(_ScheduleAssignment) _then) = __$ScheduleAssignmentCopyWithImpl;
@override @useResult
$Res call({
 String id, String projectId, String projectName, String from, String to, bool continuesAfter
});




}
/// @nodoc
class __$ScheduleAssignmentCopyWithImpl<$Res>
    implements _$ScheduleAssignmentCopyWith<$Res> {
  __$ScheduleAssignmentCopyWithImpl(this._self, this._then);

  final _ScheduleAssignment _self;
  final $Res Function(_ScheduleAssignment) _then;

/// Create a copy of ScheduleAssignment
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? projectId = null,Object? projectName = null,Object? from = null,Object? to = null,Object? continuesAfter = null,}) {
  return _then(_ScheduleAssignment(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,continuesAfter: null == continuesAfter ? _self.continuesAfter : continuesAfter // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$ScheduleAbsence {

 String get id; String get type; String get from; String get to;
/// Create a copy of ScheduleAbsence
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ScheduleAbsenceCopyWith<ScheduleAbsence> get copyWith => _$ScheduleAbsenceCopyWithImpl<ScheduleAbsence>(this as ScheduleAbsence, _$identity);

  /// Serializes this ScheduleAbsence to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ScheduleAbsence&&(identical(other.id, id) || other.id == id)&&(identical(other.type, type) || other.type == type)&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,type,from,to);

@override
String toString() {
  return 'ScheduleAbsence(id: $id, type: $type, from: $from, to: $to)';
}


}

/// @nodoc
abstract mixin class $ScheduleAbsenceCopyWith<$Res>  {
  factory $ScheduleAbsenceCopyWith(ScheduleAbsence value, $Res Function(ScheduleAbsence) _then) = _$ScheduleAbsenceCopyWithImpl;
@useResult
$Res call({
 String id, String type, String from, String to
});




}
/// @nodoc
class _$ScheduleAbsenceCopyWithImpl<$Res>
    implements $ScheduleAbsenceCopyWith<$Res> {
  _$ScheduleAbsenceCopyWithImpl(this._self, this._then);

  final ScheduleAbsence _self;
  final $Res Function(ScheduleAbsence) _then;

/// Create a copy of ScheduleAbsence
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? type = null,Object? from = null,Object? to = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [ScheduleAbsence].
extension ScheduleAbsencePatterns on ScheduleAbsence {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ScheduleAbsence value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ScheduleAbsence() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ScheduleAbsence value)  $default,){
final _that = this;
switch (_that) {
case _ScheduleAbsence():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ScheduleAbsence value)?  $default,){
final _that = this;
switch (_that) {
case _ScheduleAbsence() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String type,  String from,  String to)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ScheduleAbsence() when $default != null:
return $default(_that.id,_that.type,_that.from,_that.to);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String type,  String from,  String to)  $default,) {final _that = this;
switch (_that) {
case _ScheduleAbsence():
return $default(_that.id,_that.type,_that.from,_that.to);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String type,  String from,  String to)?  $default,) {final _that = this;
switch (_that) {
case _ScheduleAbsence() when $default != null:
return $default(_that.id,_that.type,_that.from,_that.to);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ScheduleAbsence extends ScheduleAbsence {
  const _ScheduleAbsence({required this.id, required this.type, required this.from, required this.to}): super._();
  factory _ScheduleAbsence.fromJson(Map<String, dynamic> json) => _$ScheduleAbsenceFromJson(json);

@override final  String id;
@override final  String type;
@override final  String from;
@override final  String to;

/// Create a copy of ScheduleAbsence
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ScheduleAbsenceCopyWith<_ScheduleAbsence> get copyWith => __$ScheduleAbsenceCopyWithImpl<_ScheduleAbsence>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ScheduleAbsenceToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ScheduleAbsence&&(identical(other.id, id) || other.id == id)&&(identical(other.type, type) || other.type == type)&&(identical(other.from, from) || other.from == from)&&(identical(other.to, to) || other.to == to));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,type,from,to);

@override
String toString() {
  return 'ScheduleAbsence(id: $id, type: $type, from: $from, to: $to)';
}


}

/// @nodoc
abstract mixin class _$ScheduleAbsenceCopyWith<$Res> implements $ScheduleAbsenceCopyWith<$Res> {
  factory _$ScheduleAbsenceCopyWith(_ScheduleAbsence value, $Res Function(_ScheduleAbsence) _then) = __$ScheduleAbsenceCopyWithImpl;
@override @useResult
$Res call({
 String id, String type, String from, String to
});




}
/// @nodoc
class __$ScheduleAbsenceCopyWithImpl<$Res>
    implements _$ScheduleAbsenceCopyWith<$Res> {
  __$ScheduleAbsenceCopyWithImpl(this._self, this._then);

  final _ScheduleAbsence _self;
  final $Res Function(_ScheduleAbsence) _then;

/// Create a copy of ScheduleAbsence
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? type = null,Object? from = null,Object? to = null,}) {
  return _then(_ScheduleAbsence(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,from: null == from ? _self.from : from // ignore: cast_nullable_to_non_nullable
as String,to: null == to ? _self.to : to // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on
