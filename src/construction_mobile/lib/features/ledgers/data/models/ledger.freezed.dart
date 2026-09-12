// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ledger.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$LedgerSummary {

 String get id; String get name; int get year; int get month; String? get note; String? get createdByName; DateTime get createdAt;
/// Create a copy of LedgerSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerSummaryCopyWith<LedgerSummary> get copyWith => _$LedgerSummaryCopyWithImpl<LedgerSummary>(this as LedgerSummary, _$identity);

  /// Serializes this LedgerSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.year, year) || other.year == year)&&(identical(other.month, month) || other.month == month)&&(identical(other.note, note) || other.note == note)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,year,month,note,createdByName,createdAt);

@override
String toString() {
  return 'LedgerSummary(id: $id, name: $name, year: $year, month: $month, note: $note, createdByName: $createdByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $LedgerSummaryCopyWith<$Res>  {
  factory $LedgerSummaryCopyWith(LedgerSummary value, $Res Function(LedgerSummary) _then) = _$LedgerSummaryCopyWithImpl;
@useResult
$Res call({
 String id, String name, int year, int month, String? note, String? createdByName, DateTime createdAt
});




}
/// @nodoc
class _$LedgerSummaryCopyWithImpl<$Res>
    implements $LedgerSummaryCopyWith<$Res> {
  _$LedgerSummaryCopyWithImpl(this._self, this._then);

  final LedgerSummary _self;
  final $Res Function(LedgerSummary) _then;

/// Create a copy of LedgerSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? year = null,Object? month = null,Object? note = freezed,Object? createdByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,year: null == year ? _self.year : year // ignore: cast_nullable_to_non_nullable
as int,month: null == month ? _self.month : month // ignore: cast_nullable_to_non_nullable
as int,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerSummary].
extension LedgerSummaryPatterns on LedgerSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerSummary value)  $default,){
final _that = this;
switch (_that) {
case _LedgerSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerSummary value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerSummary() when $default != null:
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _LedgerSummary():
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _LedgerSummary() when $default != null:
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerSummary implements LedgerSummary {
  const _LedgerSummary({required this.id, required this.name, required this.year, required this.month, this.note, this.createdByName, required this.createdAt});
  factory _LedgerSummary.fromJson(Map<String, dynamic> json) => _$LedgerSummaryFromJson(json);

@override final  String id;
@override final  String name;
@override final  int year;
@override final  int month;
@override final  String? note;
@override final  String? createdByName;
@override final  DateTime createdAt;

/// Create a copy of LedgerSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerSummaryCopyWith<_LedgerSummary> get copyWith => __$LedgerSummaryCopyWithImpl<_LedgerSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.year, year) || other.year == year)&&(identical(other.month, month) || other.month == month)&&(identical(other.note, note) || other.note == note)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,year,month,note,createdByName,createdAt);

@override
String toString() {
  return 'LedgerSummary(id: $id, name: $name, year: $year, month: $month, note: $note, createdByName: $createdByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$LedgerSummaryCopyWith<$Res> implements $LedgerSummaryCopyWith<$Res> {
  factory _$LedgerSummaryCopyWith(_LedgerSummary value, $Res Function(_LedgerSummary) _then) = __$LedgerSummaryCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, int year, int month, String? note, String? createdByName, DateTime createdAt
});




}
/// @nodoc
class __$LedgerSummaryCopyWithImpl<$Res>
    implements _$LedgerSummaryCopyWith<$Res> {
  __$LedgerSummaryCopyWithImpl(this._self, this._then);

  final _LedgerSummary _self;
  final $Res Function(_LedgerSummary) _then;

/// Create a copy of LedgerSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? year = null,Object? month = null,Object? note = freezed,Object? createdByName = freezed,Object? createdAt = null,}) {
  return _then(_LedgerSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,year: null == year ? _self.year : year // ignore: cast_nullable_to_non_nullable
as int,month: null == month ? _self.month : month // ignore: cast_nullable_to_non_nullable
as int,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}


/// @nodoc
mixin _$LedgerColumn {

 String get id; String get name; String get dataType; String? get sourceMetric; int get sortOrder;
/// Create a copy of LedgerColumn
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerColumnCopyWith<LedgerColumn> get copyWith => _$LedgerColumnCopyWithImpl<LedgerColumn>(this as LedgerColumn, _$identity);

  /// Serializes this LedgerColumn to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerColumn&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.dataType, dataType) || other.dataType == dataType)&&(identical(other.sourceMetric, sourceMetric) || other.sourceMetric == sourceMetric)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,dataType,sourceMetric,sortOrder);

@override
String toString() {
  return 'LedgerColumn(id: $id, name: $name, dataType: $dataType, sourceMetric: $sourceMetric, sortOrder: $sortOrder)';
}


}

/// @nodoc
abstract mixin class $LedgerColumnCopyWith<$Res>  {
  factory $LedgerColumnCopyWith(LedgerColumn value, $Res Function(LedgerColumn) _then) = _$LedgerColumnCopyWithImpl;
@useResult
$Res call({
 String id, String name, String dataType, String? sourceMetric, int sortOrder
});




}
/// @nodoc
class _$LedgerColumnCopyWithImpl<$Res>
    implements $LedgerColumnCopyWith<$Res> {
  _$LedgerColumnCopyWithImpl(this._self, this._then);

  final LedgerColumn _self;
  final $Res Function(LedgerColumn) _then;

/// Create a copy of LedgerColumn
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? dataType = null,Object? sourceMetric = freezed,Object? sortOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,dataType: null == dataType ? _self.dataType : dataType // ignore: cast_nullable_to_non_nullable
as String,sourceMetric: freezed == sourceMetric ? _self.sourceMetric : sourceMetric // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerColumn].
extension LedgerColumnPatterns on LedgerColumn {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerColumn value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerColumn() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerColumn value)  $default,){
final _that = this;
switch (_that) {
case _LedgerColumn():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerColumn value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerColumn() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String dataType,  String? sourceMetric,  int sortOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerColumn() when $default != null:
return $default(_that.id,_that.name,_that.dataType,_that.sourceMetric,_that.sortOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String dataType,  String? sourceMetric,  int sortOrder)  $default,) {final _that = this;
switch (_that) {
case _LedgerColumn():
return $default(_that.id,_that.name,_that.dataType,_that.sourceMetric,_that.sortOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String dataType,  String? sourceMetric,  int sortOrder)?  $default,) {final _that = this;
switch (_that) {
case _LedgerColumn() when $default != null:
return $default(_that.id,_that.name,_that.dataType,_that.sourceMetric,_that.sortOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerColumn implements LedgerColumn {
  const _LedgerColumn({required this.id, required this.name, required this.dataType, this.sourceMetric, this.sortOrder = 0});
  factory _LedgerColumn.fromJson(Map<String, dynamic> json) => _$LedgerColumnFromJson(json);

@override final  String id;
@override final  String name;
@override final  String dataType;
@override final  String? sourceMetric;
@override@JsonKey() final  int sortOrder;

/// Create a copy of LedgerColumn
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerColumnCopyWith<_LedgerColumn> get copyWith => __$LedgerColumnCopyWithImpl<_LedgerColumn>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerColumnToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerColumn&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.dataType, dataType) || other.dataType == dataType)&&(identical(other.sourceMetric, sourceMetric) || other.sourceMetric == sourceMetric)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,dataType,sourceMetric,sortOrder);

@override
String toString() {
  return 'LedgerColumn(id: $id, name: $name, dataType: $dataType, sourceMetric: $sourceMetric, sortOrder: $sortOrder)';
}


}

/// @nodoc
abstract mixin class _$LedgerColumnCopyWith<$Res> implements $LedgerColumnCopyWith<$Res> {
  factory _$LedgerColumnCopyWith(_LedgerColumn value, $Res Function(_LedgerColumn) _then) = __$LedgerColumnCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String dataType, String? sourceMetric, int sortOrder
});




}
/// @nodoc
class __$LedgerColumnCopyWithImpl<$Res>
    implements _$LedgerColumnCopyWith<$Res> {
  __$LedgerColumnCopyWithImpl(this._self, this._then);

  final _LedgerColumn _self;
  final $Res Function(_LedgerColumn) _then;

/// Create a copy of LedgerColumn
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? dataType = null,Object? sourceMetric = freezed,Object? sortOrder = null,}) {
  return _then(_LedgerColumn(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,dataType: null == dataType ? _self.dataType : dataType // ignore: cast_nullable_to_non_nullable
as String,sourceMetric: freezed == sourceMetric ? _self.sourceMetric : sourceMetric // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$LedgerSection {

 String get id; String get name; String? get projectId; String? get projectName; int get sortOrder; int get rowCount; List<LedgerRow> get rows;
/// Create a copy of LedgerSection
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerSectionCopyWith<LedgerSection> get copyWith => _$LedgerSectionCopyWithImpl<LedgerSection>(this as LedgerSection, _$identity);

  /// Serializes this LedgerSection to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerSection&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.rowCount, rowCount) || other.rowCount == rowCount)&&const DeepCollectionEquality().equals(other.rows, rows));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,projectId,projectName,sortOrder,rowCount,const DeepCollectionEquality().hash(rows));

@override
String toString() {
  return 'LedgerSection(id: $id, name: $name, projectId: $projectId, projectName: $projectName, sortOrder: $sortOrder, rowCount: $rowCount, rows: $rows)';
}


}

/// @nodoc
abstract mixin class $LedgerSectionCopyWith<$Res>  {
  factory $LedgerSectionCopyWith(LedgerSection value, $Res Function(LedgerSection) _then) = _$LedgerSectionCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? projectId, String? projectName, int sortOrder, int rowCount, List<LedgerRow> rows
});




}
/// @nodoc
class _$LedgerSectionCopyWithImpl<$Res>
    implements $LedgerSectionCopyWith<$Res> {
  _$LedgerSectionCopyWithImpl(this._self, this._then);

  final LedgerSection _self;
  final $Res Function(LedgerSection) _then;

/// Create a copy of LedgerSection
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? projectId = freezed,Object? projectName = freezed,Object? sortOrder = null,Object? rowCount = null,Object? rows = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,rowCount: null == rowCount ? _self.rowCount : rowCount // ignore: cast_nullable_to_non_nullable
as int,rows: null == rows ? _self.rows : rows // ignore: cast_nullable_to_non_nullable
as List<LedgerRow>,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerSection].
extension LedgerSectionPatterns on LedgerSection {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerSection value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerSection() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerSection value)  $default,){
final _that = this;
switch (_that) {
case _LedgerSection():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerSection value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerSection() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? projectId,  String? projectName,  int sortOrder,  int rowCount,  List<LedgerRow> rows)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerSection() when $default != null:
return $default(_that.id,_that.name,_that.projectId,_that.projectName,_that.sortOrder,_that.rowCount,_that.rows);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? projectId,  String? projectName,  int sortOrder,  int rowCount,  List<LedgerRow> rows)  $default,) {final _that = this;
switch (_that) {
case _LedgerSection():
return $default(_that.id,_that.name,_that.projectId,_that.projectName,_that.sortOrder,_that.rowCount,_that.rows);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? projectId,  String? projectName,  int sortOrder,  int rowCount,  List<LedgerRow> rows)?  $default,) {final _that = this;
switch (_that) {
case _LedgerSection() when $default != null:
return $default(_that.id,_that.name,_that.projectId,_that.projectName,_that.sortOrder,_that.rowCount,_that.rows);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerSection implements LedgerSection {
  const _LedgerSection({required this.id, required this.name, this.projectId, this.projectName, this.sortOrder = 0, this.rowCount = 0, final  List<LedgerRow> rows = const <LedgerRow>[]}): _rows = rows;
  factory _LedgerSection.fromJson(Map<String, dynamic> json) => _$LedgerSectionFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? projectId;
@override final  String? projectName;
@override@JsonKey() final  int sortOrder;
@override@JsonKey() final  int rowCount;
 final  List<LedgerRow> _rows;
@override@JsonKey() List<LedgerRow> get rows {
  if (_rows is EqualUnmodifiableListView) return _rows;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_rows);
}


/// Create a copy of LedgerSection
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerSectionCopyWith<_LedgerSection> get copyWith => __$LedgerSectionCopyWithImpl<_LedgerSection>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerSectionToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerSection&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.rowCount, rowCount) || other.rowCount == rowCount)&&const DeepCollectionEquality().equals(other._rows, _rows));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,projectId,projectName,sortOrder,rowCount,const DeepCollectionEquality().hash(_rows));

@override
String toString() {
  return 'LedgerSection(id: $id, name: $name, projectId: $projectId, projectName: $projectName, sortOrder: $sortOrder, rowCount: $rowCount, rows: $rows)';
}


}

/// @nodoc
abstract mixin class _$LedgerSectionCopyWith<$Res> implements $LedgerSectionCopyWith<$Res> {
  factory _$LedgerSectionCopyWith(_LedgerSection value, $Res Function(_LedgerSection) _then) = __$LedgerSectionCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? projectId, String? projectName, int sortOrder, int rowCount, List<LedgerRow> rows
});




}
/// @nodoc
class __$LedgerSectionCopyWithImpl<$Res>
    implements _$LedgerSectionCopyWith<$Res> {
  __$LedgerSectionCopyWithImpl(this._self, this._then);

  final _LedgerSection _self;
  final $Res Function(_LedgerSection) _then;

/// Create a copy of LedgerSection
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? projectId = freezed,Object? projectName = freezed,Object? sortOrder = null,Object? rowCount = null,Object? rows = null,}) {
  return _then(_LedgerSection(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,rowCount: null == rowCount ? _self.rowCount : rowCount // ignore: cast_nullable_to_non_nullable
as int,rows: null == rows ? _self._rows : rows // ignore: cast_nullable_to_non_nullable
as List<LedgerRow>,
  ));
}


}


/// @nodoc
mixin _$LedgerRow {

 String get id; String get label; String? get employeeId; String? get employeeName; String? get vehicleId; String? get vehicleName; String? get toolId; String? get toolName; String? get materialId; String? get materialName; String? get promotedGeneralExpenseId; String? get promotedAccommodationRateId; String? get colorTag; int get sortOrder; List<LedgerCell> get cells;
/// Create a copy of LedgerRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerRowCopyWith<LedgerRow> get copyWith => _$LedgerRowCopyWithImpl<LedgerRow>(this as LedgerRow, _$identity);

  /// Serializes this LedgerRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerRow&&(identical(other.id, id) || other.id == id)&&(identical(other.label, label) || other.label == label)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.materialId, materialId) || other.materialId == materialId)&&(identical(other.materialName, materialName) || other.materialName == materialName)&&(identical(other.promotedGeneralExpenseId, promotedGeneralExpenseId) || other.promotedGeneralExpenseId == promotedGeneralExpenseId)&&(identical(other.promotedAccommodationRateId, promotedAccommodationRateId) || other.promotedAccommodationRateId == promotedAccommodationRateId)&&(identical(other.colorTag, colorTag) || other.colorTag == colorTag)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&const DeepCollectionEquality().equals(other.cells, cells));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,label,employeeId,employeeName,vehicleId,vehicleName,toolId,toolName,materialId,materialName,promotedGeneralExpenseId,promotedAccommodationRateId,colorTag,sortOrder,const DeepCollectionEquality().hash(cells));

@override
String toString() {
  return 'LedgerRow(id: $id, label: $label, employeeId: $employeeId, employeeName: $employeeName, vehicleId: $vehicleId, vehicleName: $vehicleName, toolId: $toolId, toolName: $toolName, materialId: $materialId, materialName: $materialName, promotedGeneralExpenseId: $promotedGeneralExpenseId, promotedAccommodationRateId: $promotedAccommodationRateId, colorTag: $colorTag, sortOrder: $sortOrder, cells: $cells)';
}


}

/// @nodoc
abstract mixin class $LedgerRowCopyWith<$Res>  {
  factory $LedgerRowCopyWith(LedgerRow value, $Res Function(LedgerRow) _then) = _$LedgerRowCopyWithImpl;
@useResult
$Res call({
 String id, String label, String? employeeId, String? employeeName, String? vehicleId, String? vehicleName, String? toolId, String? toolName, String? materialId, String? materialName, String? promotedGeneralExpenseId, String? promotedAccommodationRateId, String? colorTag, int sortOrder, List<LedgerCell> cells
});




}
/// @nodoc
class _$LedgerRowCopyWithImpl<$Res>
    implements $LedgerRowCopyWith<$Res> {
  _$LedgerRowCopyWithImpl(this._self, this._then);

  final LedgerRow _self;
  final $Res Function(LedgerRow) _then;

/// Create a copy of LedgerRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? label = null,Object? employeeId = freezed,Object? employeeName = freezed,Object? vehicleId = freezed,Object? vehicleName = freezed,Object? toolId = freezed,Object? toolName = freezed,Object? materialId = freezed,Object? materialName = freezed,Object? promotedGeneralExpenseId = freezed,Object? promotedAccommodationRateId = freezed,Object? colorTag = freezed,Object? sortOrder = null,Object? cells = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,employeeId: freezed == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String?,employeeName: freezed == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String?,vehicleId: freezed == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String?,vehicleName: freezed == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String?,toolId: freezed == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String?,toolName: freezed == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String?,materialId: freezed == materialId ? _self.materialId : materialId // ignore: cast_nullable_to_non_nullable
as String?,materialName: freezed == materialName ? _self.materialName : materialName // ignore: cast_nullable_to_non_nullable
as String?,promotedGeneralExpenseId: freezed == promotedGeneralExpenseId ? _self.promotedGeneralExpenseId : promotedGeneralExpenseId // ignore: cast_nullable_to_non_nullable
as String?,promotedAccommodationRateId: freezed == promotedAccommodationRateId ? _self.promotedAccommodationRateId : promotedAccommodationRateId // ignore: cast_nullable_to_non_nullable
as String?,colorTag: freezed == colorTag ? _self.colorTag : colorTag // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,cells: null == cells ? _self.cells : cells // ignore: cast_nullable_to_non_nullable
as List<LedgerCell>,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerRow].
extension LedgerRowPatterns on LedgerRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerRow value)  $default,){
final _that = this;
switch (_that) {
case _LedgerRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerRow value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String label,  String? employeeId,  String? employeeName,  String? vehicleId,  String? vehicleName,  String? toolId,  String? toolName,  String? materialId,  String? materialName,  String? promotedGeneralExpenseId,  String? promotedAccommodationRateId,  String? colorTag,  int sortOrder,  List<LedgerCell> cells)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerRow() when $default != null:
return $default(_that.id,_that.label,_that.employeeId,_that.employeeName,_that.vehicleId,_that.vehicleName,_that.toolId,_that.toolName,_that.materialId,_that.materialName,_that.promotedGeneralExpenseId,_that.promotedAccommodationRateId,_that.colorTag,_that.sortOrder,_that.cells);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String label,  String? employeeId,  String? employeeName,  String? vehicleId,  String? vehicleName,  String? toolId,  String? toolName,  String? materialId,  String? materialName,  String? promotedGeneralExpenseId,  String? promotedAccommodationRateId,  String? colorTag,  int sortOrder,  List<LedgerCell> cells)  $default,) {final _that = this;
switch (_that) {
case _LedgerRow():
return $default(_that.id,_that.label,_that.employeeId,_that.employeeName,_that.vehicleId,_that.vehicleName,_that.toolId,_that.toolName,_that.materialId,_that.materialName,_that.promotedGeneralExpenseId,_that.promotedAccommodationRateId,_that.colorTag,_that.sortOrder,_that.cells);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String label,  String? employeeId,  String? employeeName,  String? vehicleId,  String? vehicleName,  String? toolId,  String? toolName,  String? materialId,  String? materialName,  String? promotedGeneralExpenseId,  String? promotedAccommodationRateId,  String? colorTag,  int sortOrder,  List<LedgerCell> cells)?  $default,) {final _that = this;
switch (_that) {
case _LedgerRow() when $default != null:
return $default(_that.id,_that.label,_that.employeeId,_that.employeeName,_that.vehicleId,_that.vehicleName,_that.toolId,_that.toolName,_that.materialId,_that.materialName,_that.promotedGeneralExpenseId,_that.promotedAccommodationRateId,_that.colorTag,_that.sortOrder,_that.cells);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerRow extends LedgerRow {
  const _LedgerRow({required this.id, required this.label, this.employeeId, this.employeeName, this.vehicleId, this.vehicleName, this.toolId, this.toolName, this.materialId, this.materialName, this.promotedGeneralExpenseId, this.promotedAccommodationRateId, this.colorTag, this.sortOrder = 0, final  List<LedgerCell> cells = const <LedgerCell>[]}): _cells = cells,super._();
  factory _LedgerRow.fromJson(Map<String, dynamic> json) => _$LedgerRowFromJson(json);

@override final  String id;
@override final  String label;
@override final  String? employeeId;
@override final  String? employeeName;
@override final  String? vehicleId;
@override final  String? vehicleName;
@override final  String? toolId;
@override final  String? toolName;
@override final  String? materialId;
@override final  String? materialName;
@override final  String? promotedGeneralExpenseId;
@override final  String? promotedAccommodationRateId;
@override final  String? colorTag;
@override@JsonKey() final  int sortOrder;
 final  List<LedgerCell> _cells;
@override@JsonKey() List<LedgerCell> get cells {
  if (_cells is EqualUnmodifiableListView) return _cells;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_cells);
}


/// Create a copy of LedgerRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerRowCopyWith<_LedgerRow> get copyWith => __$LedgerRowCopyWithImpl<_LedgerRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerRowToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerRow&&(identical(other.id, id) || other.id == id)&&(identical(other.label, label) || other.label == label)&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeName, employeeName) || other.employeeName == employeeName)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.materialId, materialId) || other.materialId == materialId)&&(identical(other.materialName, materialName) || other.materialName == materialName)&&(identical(other.promotedGeneralExpenseId, promotedGeneralExpenseId) || other.promotedGeneralExpenseId == promotedGeneralExpenseId)&&(identical(other.promotedAccommodationRateId, promotedAccommodationRateId) || other.promotedAccommodationRateId == promotedAccommodationRateId)&&(identical(other.colorTag, colorTag) || other.colorTag == colorTag)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&const DeepCollectionEquality().equals(other._cells, _cells));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,label,employeeId,employeeName,vehicleId,vehicleName,toolId,toolName,materialId,materialName,promotedGeneralExpenseId,promotedAccommodationRateId,colorTag,sortOrder,const DeepCollectionEquality().hash(_cells));

@override
String toString() {
  return 'LedgerRow(id: $id, label: $label, employeeId: $employeeId, employeeName: $employeeName, vehicleId: $vehicleId, vehicleName: $vehicleName, toolId: $toolId, toolName: $toolName, materialId: $materialId, materialName: $materialName, promotedGeneralExpenseId: $promotedGeneralExpenseId, promotedAccommodationRateId: $promotedAccommodationRateId, colorTag: $colorTag, sortOrder: $sortOrder, cells: $cells)';
}


}

/// @nodoc
abstract mixin class _$LedgerRowCopyWith<$Res> implements $LedgerRowCopyWith<$Res> {
  factory _$LedgerRowCopyWith(_LedgerRow value, $Res Function(_LedgerRow) _then) = __$LedgerRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String label, String? employeeId, String? employeeName, String? vehicleId, String? vehicleName, String? toolId, String? toolName, String? materialId, String? materialName, String? promotedGeneralExpenseId, String? promotedAccommodationRateId, String? colorTag, int sortOrder, List<LedgerCell> cells
});




}
/// @nodoc
class __$LedgerRowCopyWithImpl<$Res>
    implements _$LedgerRowCopyWith<$Res> {
  __$LedgerRowCopyWithImpl(this._self, this._then);

  final _LedgerRow _self;
  final $Res Function(_LedgerRow) _then;

/// Create a copy of LedgerRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? label = null,Object? employeeId = freezed,Object? employeeName = freezed,Object? vehicleId = freezed,Object? vehicleName = freezed,Object? toolId = freezed,Object? toolName = freezed,Object? materialId = freezed,Object? materialName = freezed,Object? promotedGeneralExpenseId = freezed,Object? promotedAccommodationRateId = freezed,Object? colorTag = freezed,Object? sortOrder = null,Object? cells = null,}) {
  return _then(_LedgerRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,employeeId: freezed == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String?,employeeName: freezed == employeeName ? _self.employeeName : employeeName // ignore: cast_nullable_to_non_nullable
as String?,vehicleId: freezed == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String?,vehicleName: freezed == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String?,toolId: freezed == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String?,toolName: freezed == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String?,materialId: freezed == materialId ? _self.materialId : materialId // ignore: cast_nullable_to_non_nullable
as String?,materialName: freezed == materialName ? _self.materialName : materialName // ignore: cast_nullable_to_non_nullable
as String?,promotedGeneralExpenseId: freezed == promotedGeneralExpenseId ? _self.promotedGeneralExpenseId : promotedGeneralExpenseId // ignore: cast_nullable_to_non_nullable
as String?,promotedAccommodationRateId: freezed == promotedAccommodationRateId ? _self.promotedAccommodationRateId : promotedAccommodationRateId // ignore: cast_nullable_to_non_nullable
as String?,colorTag: freezed == colorTag ? _self.colorTag : colorTag // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,cells: null == cells ? _self._cells : cells // ignore: cast_nullable_to_non_nullable
as List<LedgerCell>,
  ));
}


}


/// @nodoc
mixin _$LedgerCell {

 String? get id; String get columnId; String? get value; String? get colorTag; bool get isComputed;
/// Create a copy of LedgerCell
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerCellCopyWith<LedgerCell> get copyWith => _$LedgerCellCopyWithImpl<LedgerCell>(this as LedgerCell, _$identity);

  /// Serializes this LedgerCell to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerCell&&(identical(other.id, id) || other.id == id)&&(identical(other.columnId, columnId) || other.columnId == columnId)&&(identical(other.value, value) || other.value == value)&&(identical(other.colorTag, colorTag) || other.colorTag == colorTag)&&(identical(other.isComputed, isComputed) || other.isComputed == isComputed));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,columnId,value,colorTag,isComputed);

@override
String toString() {
  return 'LedgerCell(id: $id, columnId: $columnId, value: $value, colorTag: $colorTag, isComputed: $isComputed)';
}


}

/// @nodoc
abstract mixin class $LedgerCellCopyWith<$Res>  {
  factory $LedgerCellCopyWith(LedgerCell value, $Res Function(LedgerCell) _then) = _$LedgerCellCopyWithImpl;
@useResult
$Res call({
 String? id, String columnId, String? value, String? colorTag, bool isComputed
});




}
/// @nodoc
class _$LedgerCellCopyWithImpl<$Res>
    implements $LedgerCellCopyWith<$Res> {
  _$LedgerCellCopyWithImpl(this._self, this._then);

  final LedgerCell _self;
  final $Res Function(LedgerCell) _then;

/// Create a copy of LedgerCell
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = freezed,Object? columnId = null,Object? value = freezed,Object? colorTag = freezed,Object? isComputed = null,}) {
  return _then(_self.copyWith(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,columnId: null == columnId ? _self.columnId : columnId // ignore: cast_nullable_to_non_nullable
as String,value: freezed == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as String?,colorTag: freezed == colorTag ? _self.colorTag : colorTag // ignore: cast_nullable_to_non_nullable
as String?,isComputed: null == isComputed ? _self.isComputed : isComputed // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerCell].
extension LedgerCellPatterns on LedgerCell {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerCell value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerCell() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerCell value)  $default,){
final _that = this;
switch (_that) {
case _LedgerCell():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerCell value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerCell() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String? id,  String columnId,  String? value,  String? colorTag,  bool isComputed)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerCell() when $default != null:
return $default(_that.id,_that.columnId,_that.value,_that.colorTag,_that.isComputed);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String? id,  String columnId,  String? value,  String? colorTag,  bool isComputed)  $default,) {final _that = this;
switch (_that) {
case _LedgerCell():
return $default(_that.id,_that.columnId,_that.value,_that.colorTag,_that.isComputed);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String? id,  String columnId,  String? value,  String? colorTag,  bool isComputed)?  $default,) {final _that = this;
switch (_that) {
case _LedgerCell() when $default != null:
return $default(_that.id,_that.columnId,_that.value,_that.colorTag,_that.isComputed);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerCell implements LedgerCell {
  const _LedgerCell({this.id, required this.columnId, this.value, this.colorTag, this.isComputed = false});
  factory _LedgerCell.fromJson(Map<String, dynamic> json) => _$LedgerCellFromJson(json);

@override final  String? id;
@override final  String columnId;
@override final  String? value;
@override final  String? colorTag;
@override@JsonKey() final  bool isComputed;

/// Create a copy of LedgerCell
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerCellCopyWith<_LedgerCell> get copyWith => __$LedgerCellCopyWithImpl<_LedgerCell>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerCellToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerCell&&(identical(other.id, id) || other.id == id)&&(identical(other.columnId, columnId) || other.columnId == columnId)&&(identical(other.value, value) || other.value == value)&&(identical(other.colorTag, colorTag) || other.colorTag == colorTag)&&(identical(other.isComputed, isComputed) || other.isComputed == isComputed));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,columnId,value,colorTag,isComputed);

@override
String toString() {
  return 'LedgerCell(id: $id, columnId: $columnId, value: $value, colorTag: $colorTag, isComputed: $isComputed)';
}


}

/// @nodoc
abstract mixin class _$LedgerCellCopyWith<$Res> implements $LedgerCellCopyWith<$Res> {
  factory _$LedgerCellCopyWith(_LedgerCell value, $Res Function(_LedgerCell) _then) = __$LedgerCellCopyWithImpl;
@override @useResult
$Res call({
 String? id, String columnId, String? value, String? colorTag, bool isComputed
});




}
/// @nodoc
class __$LedgerCellCopyWithImpl<$Res>
    implements _$LedgerCellCopyWith<$Res> {
  __$LedgerCellCopyWithImpl(this._self, this._then);

  final _LedgerCell _self;
  final $Res Function(_LedgerCell) _then;

/// Create a copy of LedgerCell
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = freezed,Object? columnId = null,Object? value = freezed,Object? colorTag = freezed,Object? isComputed = null,}) {
  return _then(_LedgerCell(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,columnId: null == columnId ? _self.columnId : columnId // ignore: cast_nullable_to_non_nullable
as String,value: freezed == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as String?,colorTag: freezed == colorTag ? _self.colorTag : colorTag // ignore: cast_nullable_to_non_nullable
as String?,isComputed: null == isComputed ? _self.isComputed : isComputed // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$LedgerDetail {

 String get id; String get name; int get year; int get month; String? get note; String? get createdByName; DateTime get createdAt; DateTime? get updatedAt; List<LedgerColumn> get columns; List<LedgerSection> get sections;
/// Create a copy of LedgerDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerDetailCopyWith<LedgerDetail> get copyWith => _$LedgerDetailCopyWithImpl<LedgerDetail>(this as LedgerDetail, _$identity);

  /// Serializes this LedgerDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.year, year) || other.year == year)&&(identical(other.month, month) || other.month == month)&&(identical(other.note, note) || other.note == note)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&const DeepCollectionEquality().equals(other.columns, columns)&&const DeepCollectionEquality().equals(other.sections, sections));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,year,month,note,createdByName,createdAt,updatedAt,const DeepCollectionEquality().hash(columns),const DeepCollectionEquality().hash(sections));

@override
String toString() {
  return 'LedgerDetail(id: $id, name: $name, year: $year, month: $month, note: $note, createdByName: $createdByName, createdAt: $createdAt, updatedAt: $updatedAt, columns: $columns, sections: $sections)';
}


}

/// @nodoc
abstract mixin class $LedgerDetailCopyWith<$Res>  {
  factory $LedgerDetailCopyWith(LedgerDetail value, $Res Function(LedgerDetail) _then) = _$LedgerDetailCopyWithImpl;
@useResult
$Res call({
 String id, String name, int year, int month, String? note, String? createdByName, DateTime createdAt, DateTime? updatedAt, List<LedgerColumn> columns, List<LedgerSection> sections
});




}
/// @nodoc
class _$LedgerDetailCopyWithImpl<$Res>
    implements $LedgerDetailCopyWith<$Res> {
  _$LedgerDetailCopyWithImpl(this._self, this._then);

  final LedgerDetail _self;
  final $Res Function(LedgerDetail) _then;

/// Create a copy of LedgerDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? year = null,Object? month = null,Object? note = freezed,Object? createdByName = freezed,Object? createdAt = null,Object? updatedAt = freezed,Object? columns = null,Object? sections = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,year: null == year ? _self.year : year // ignore: cast_nullable_to_non_nullable
as int,month: null == month ? _self.month : month // ignore: cast_nullable_to_non_nullable
as int,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,columns: null == columns ? _self.columns : columns // ignore: cast_nullable_to_non_nullable
as List<LedgerColumn>,sections: null == sections ? _self.sections : sections // ignore: cast_nullable_to_non_nullable
as List<LedgerSection>,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerDetail].
extension LedgerDetailPatterns on LedgerDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerDetail value)  $default,){
final _that = this;
switch (_that) {
case _LedgerDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerDetail value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt,  DateTime? updatedAt,  List<LedgerColumn> columns,  List<LedgerSection> sections)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerDetail() when $default != null:
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt,_that.updatedAt,_that.columns,_that.sections);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt,  DateTime? updatedAt,  List<LedgerColumn> columns,  List<LedgerSection> sections)  $default,) {final _that = this;
switch (_that) {
case _LedgerDetail():
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt,_that.updatedAt,_that.columns,_that.sections);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  int year,  int month,  String? note,  String? createdByName,  DateTime createdAt,  DateTime? updatedAt,  List<LedgerColumn> columns,  List<LedgerSection> sections)?  $default,) {final _that = this;
switch (_that) {
case _LedgerDetail() when $default != null:
return $default(_that.id,_that.name,_that.year,_that.month,_that.note,_that.createdByName,_that.createdAt,_that.updatedAt,_that.columns,_that.sections);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerDetail implements LedgerDetail {
  const _LedgerDetail({required this.id, required this.name, required this.year, required this.month, this.note, this.createdByName, required this.createdAt, this.updatedAt, final  List<LedgerColumn> columns = const <LedgerColumn>[], final  List<LedgerSection> sections = const <LedgerSection>[]}): _columns = columns,_sections = sections;
  factory _LedgerDetail.fromJson(Map<String, dynamic> json) => _$LedgerDetailFromJson(json);

@override final  String id;
@override final  String name;
@override final  int year;
@override final  int month;
@override final  String? note;
@override final  String? createdByName;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;
 final  List<LedgerColumn> _columns;
@override@JsonKey() List<LedgerColumn> get columns {
  if (_columns is EqualUnmodifiableListView) return _columns;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_columns);
}

 final  List<LedgerSection> _sections;
@override@JsonKey() List<LedgerSection> get sections {
  if (_sections is EqualUnmodifiableListView) return _sections;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_sections);
}


/// Create a copy of LedgerDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerDetailCopyWith<_LedgerDetail> get copyWith => __$LedgerDetailCopyWithImpl<_LedgerDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerDetailToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.year, year) || other.year == year)&&(identical(other.month, month) || other.month == month)&&(identical(other.note, note) || other.note == note)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&const DeepCollectionEquality().equals(other._columns, _columns)&&const DeepCollectionEquality().equals(other._sections, _sections));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,year,month,note,createdByName,createdAt,updatedAt,const DeepCollectionEquality().hash(_columns),const DeepCollectionEquality().hash(_sections));

@override
String toString() {
  return 'LedgerDetail(id: $id, name: $name, year: $year, month: $month, note: $note, createdByName: $createdByName, createdAt: $createdAt, updatedAt: $updatedAt, columns: $columns, sections: $sections)';
}


}

/// @nodoc
abstract mixin class _$LedgerDetailCopyWith<$Res> implements $LedgerDetailCopyWith<$Res> {
  factory _$LedgerDetailCopyWith(_LedgerDetail value, $Res Function(_LedgerDetail) _then) = __$LedgerDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, int year, int month, String? note, String? createdByName, DateTime createdAt, DateTime? updatedAt, List<LedgerColumn> columns, List<LedgerSection> sections
});




}
/// @nodoc
class __$LedgerDetailCopyWithImpl<$Res>
    implements _$LedgerDetailCopyWith<$Res> {
  __$LedgerDetailCopyWithImpl(this._self, this._then);

  final _LedgerDetail _self;
  final $Res Function(_LedgerDetail) _then;

/// Create a copy of LedgerDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? year = null,Object? month = null,Object? note = freezed,Object? createdByName = freezed,Object? createdAt = null,Object? updatedAt = freezed,Object? columns = null,Object? sections = null,}) {
  return _then(_LedgerDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,year: null == year ? _self.year : year // ignore: cast_nullable_to_non_nullable
as int,month: null == month ? _self.month : month // ignore: cast_nullable_to_non_nullable
as int,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,columns: null == columns ? _self._columns : columns // ignore: cast_nullable_to_non_nullable
as List<LedgerColumn>,sections: null == sections ? _self._sections : sections // ignore: cast_nullable_to_non_nullable
as List<LedgerSection>,
  ));
}


}


/// @nodoc
mixin _$LedgerSummaryBox {

 String get id; String get label; String? get sourceColumnId; String? get sourceColumnName; double? get manualValue; int get sign; String? get color; int get sortOrder; double get value;
/// Create a copy of LedgerSummaryBox
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerSummaryBoxCopyWith<LedgerSummaryBox> get copyWith => _$LedgerSummaryBoxCopyWithImpl<LedgerSummaryBox>(this as LedgerSummaryBox, _$identity);

  /// Serializes this LedgerSummaryBox to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerSummaryBox&&(identical(other.id, id) || other.id == id)&&(identical(other.label, label) || other.label == label)&&(identical(other.sourceColumnId, sourceColumnId) || other.sourceColumnId == sourceColumnId)&&(identical(other.sourceColumnName, sourceColumnName) || other.sourceColumnName == sourceColumnName)&&(identical(other.manualValue, manualValue) || other.manualValue == manualValue)&&(identical(other.sign, sign) || other.sign == sign)&&(identical(other.color, color) || other.color == color)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.value, value) || other.value == value));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,label,sourceColumnId,sourceColumnName,manualValue,sign,color,sortOrder,value);

@override
String toString() {
  return 'LedgerSummaryBox(id: $id, label: $label, sourceColumnId: $sourceColumnId, sourceColumnName: $sourceColumnName, manualValue: $manualValue, sign: $sign, color: $color, sortOrder: $sortOrder, value: $value)';
}


}

/// @nodoc
abstract mixin class $LedgerSummaryBoxCopyWith<$Res>  {
  factory $LedgerSummaryBoxCopyWith(LedgerSummaryBox value, $Res Function(LedgerSummaryBox) _then) = _$LedgerSummaryBoxCopyWithImpl;
@useResult
$Res call({
 String id, String label, String? sourceColumnId, String? sourceColumnName, double? manualValue, int sign, String? color, int sortOrder, double value
});




}
/// @nodoc
class _$LedgerSummaryBoxCopyWithImpl<$Res>
    implements $LedgerSummaryBoxCopyWith<$Res> {
  _$LedgerSummaryBoxCopyWithImpl(this._self, this._then);

  final LedgerSummaryBox _self;
  final $Res Function(LedgerSummaryBox) _then;

/// Create a copy of LedgerSummaryBox
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? label = null,Object? sourceColumnId = freezed,Object? sourceColumnName = freezed,Object? manualValue = freezed,Object? sign = null,Object? color = freezed,Object? sortOrder = null,Object? value = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,sourceColumnId: freezed == sourceColumnId ? _self.sourceColumnId : sourceColumnId // ignore: cast_nullable_to_non_nullable
as String?,sourceColumnName: freezed == sourceColumnName ? _self.sourceColumnName : sourceColumnName // ignore: cast_nullable_to_non_nullable
as String?,manualValue: freezed == manualValue ? _self.manualValue : manualValue // ignore: cast_nullable_to_non_nullable
as double?,sign: null == sign ? _self.sign : sign // ignore: cast_nullable_to_non_nullable
as int,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,value: null == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as double,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerSummaryBox].
extension LedgerSummaryBoxPatterns on LedgerSummaryBox {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerSummaryBox value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerSummaryBox() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerSummaryBox value)  $default,){
final _that = this;
switch (_that) {
case _LedgerSummaryBox():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerSummaryBox value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerSummaryBox() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String label,  String? sourceColumnId,  String? sourceColumnName,  double? manualValue,  int sign,  String? color,  int sortOrder,  double value)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerSummaryBox() when $default != null:
return $default(_that.id,_that.label,_that.sourceColumnId,_that.sourceColumnName,_that.manualValue,_that.sign,_that.color,_that.sortOrder,_that.value);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String label,  String? sourceColumnId,  String? sourceColumnName,  double? manualValue,  int sign,  String? color,  int sortOrder,  double value)  $default,) {final _that = this;
switch (_that) {
case _LedgerSummaryBox():
return $default(_that.id,_that.label,_that.sourceColumnId,_that.sourceColumnName,_that.manualValue,_that.sign,_that.color,_that.sortOrder,_that.value);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String label,  String? sourceColumnId,  String? sourceColumnName,  double? manualValue,  int sign,  String? color,  int sortOrder,  double value)?  $default,) {final _that = this;
switch (_that) {
case _LedgerSummaryBox() when $default != null:
return $default(_that.id,_that.label,_that.sourceColumnId,_that.sourceColumnName,_that.manualValue,_that.sign,_that.color,_that.sortOrder,_that.value);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerSummaryBox implements LedgerSummaryBox {
  const _LedgerSummaryBox({required this.id, required this.label, this.sourceColumnId, this.sourceColumnName, this.manualValue, this.sign = 1, this.color, this.sortOrder = 0, this.value = 0});
  factory _LedgerSummaryBox.fromJson(Map<String, dynamic> json) => _$LedgerSummaryBoxFromJson(json);

@override final  String id;
@override final  String label;
@override final  String? sourceColumnId;
@override final  String? sourceColumnName;
@override final  double? manualValue;
@override@JsonKey() final  int sign;
@override final  String? color;
@override@JsonKey() final  int sortOrder;
@override@JsonKey() final  double value;

/// Create a copy of LedgerSummaryBox
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerSummaryBoxCopyWith<_LedgerSummaryBox> get copyWith => __$LedgerSummaryBoxCopyWithImpl<_LedgerSummaryBox>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerSummaryBoxToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerSummaryBox&&(identical(other.id, id) || other.id == id)&&(identical(other.label, label) || other.label == label)&&(identical(other.sourceColumnId, sourceColumnId) || other.sourceColumnId == sourceColumnId)&&(identical(other.sourceColumnName, sourceColumnName) || other.sourceColumnName == sourceColumnName)&&(identical(other.manualValue, manualValue) || other.manualValue == manualValue)&&(identical(other.sign, sign) || other.sign == sign)&&(identical(other.color, color) || other.color == color)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.value, value) || other.value == value));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,label,sourceColumnId,sourceColumnName,manualValue,sign,color,sortOrder,value);

@override
String toString() {
  return 'LedgerSummaryBox(id: $id, label: $label, sourceColumnId: $sourceColumnId, sourceColumnName: $sourceColumnName, manualValue: $manualValue, sign: $sign, color: $color, sortOrder: $sortOrder, value: $value)';
}


}

/// @nodoc
abstract mixin class _$LedgerSummaryBoxCopyWith<$Res> implements $LedgerSummaryBoxCopyWith<$Res> {
  factory _$LedgerSummaryBoxCopyWith(_LedgerSummaryBox value, $Res Function(_LedgerSummaryBox) _then) = __$LedgerSummaryBoxCopyWithImpl;
@override @useResult
$Res call({
 String id, String label, String? sourceColumnId, String? sourceColumnName, double? manualValue, int sign, String? color, int sortOrder, double value
});




}
/// @nodoc
class __$LedgerSummaryBoxCopyWithImpl<$Res>
    implements _$LedgerSummaryBoxCopyWith<$Res> {
  __$LedgerSummaryBoxCopyWithImpl(this._self, this._then);

  final _LedgerSummaryBox _self;
  final $Res Function(_LedgerSummaryBox) _then;

/// Create a copy of LedgerSummaryBox
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? label = null,Object? sourceColumnId = freezed,Object? sourceColumnName = freezed,Object? manualValue = freezed,Object? sign = null,Object? color = freezed,Object? sortOrder = null,Object? value = null,}) {
  return _then(_LedgerSummaryBox(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,sourceColumnId: freezed == sourceColumnId ? _self.sourceColumnId : sourceColumnId // ignore: cast_nullable_to_non_nullable
as String?,sourceColumnName: freezed == sourceColumnName ? _self.sourceColumnName : sourceColumnName // ignore: cast_nullable_to_non_nullable
as String?,manualValue: freezed == manualValue ? _self.manualValue : manualValue // ignore: cast_nullable_to_non_nullable
as double?,sign: null == sign ? _self.sign : sign // ignore: cast_nullable_to_non_nullable
as int,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,value: null == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as double,
  ));
}


}


/// @nodoc
mixin _$LedgerSummaryPanel {

 List<LedgerSummaryBox> get boxes; double get netTotal;
/// Create a copy of LedgerSummaryPanel
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LedgerSummaryPanelCopyWith<LedgerSummaryPanel> get copyWith => _$LedgerSummaryPanelCopyWithImpl<LedgerSummaryPanel>(this as LedgerSummaryPanel, _$identity);

  /// Serializes this LedgerSummaryPanel to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LedgerSummaryPanel&&const DeepCollectionEquality().equals(other.boxes, boxes)&&(identical(other.netTotal, netTotal) || other.netTotal == netTotal));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,const DeepCollectionEquality().hash(boxes),netTotal);

@override
String toString() {
  return 'LedgerSummaryPanel(boxes: $boxes, netTotal: $netTotal)';
}


}

/// @nodoc
abstract mixin class $LedgerSummaryPanelCopyWith<$Res>  {
  factory $LedgerSummaryPanelCopyWith(LedgerSummaryPanel value, $Res Function(LedgerSummaryPanel) _then) = _$LedgerSummaryPanelCopyWithImpl;
@useResult
$Res call({
 List<LedgerSummaryBox> boxes, double netTotal
});




}
/// @nodoc
class _$LedgerSummaryPanelCopyWithImpl<$Res>
    implements $LedgerSummaryPanelCopyWith<$Res> {
  _$LedgerSummaryPanelCopyWithImpl(this._self, this._then);

  final LedgerSummaryPanel _self;
  final $Res Function(LedgerSummaryPanel) _then;

/// Create a copy of LedgerSummaryPanel
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? boxes = null,Object? netTotal = null,}) {
  return _then(_self.copyWith(
boxes: null == boxes ? _self.boxes : boxes // ignore: cast_nullable_to_non_nullable
as List<LedgerSummaryBox>,netTotal: null == netTotal ? _self.netTotal : netTotal // ignore: cast_nullable_to_non_nullable
as double,
  ));
}

}


/// Adds pattern-matching-related methods to [LedgerSummaryPanel].
extension LedgerSummaryPanelPatterns on LedgerSummaryPanel {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LedgerSummaryPanel value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LedgerSummaryPanel() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LedgerSummaryPanel value)  $default,){
final _that = this;
switch (_that) {
case _LedgerSummaryPanel():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LedgerSummaryPanel value)?  $default,){
final _that = this;
switch (_that) {
case _LedgerSummaryPanel() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( List<LedgerSummaryBox> boxes,  double netTotal)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LedgerSummaryPanel() when $default != null:
return $default(_that.boxes,_that.netTotal);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( List<LedgerSummaryBox> boxes,  double netTotal)  $default,) {final _that = this;
switch (_that) {
case _LedgerSummaryPanel():
return $default(_that.boxes,_that.netTotal);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( List<LedgerSummaryBox> boxes,  double netTotal)?  $default,) {final _that = this;
switch (_that) {
case _LedgerSummaryPanel() when $default != null:
return $default(_that.boxes,_that.netTotal);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LedgerSummaryPanel implements LedgerSummaryPanel {
  const _LedgerSummaryPanel({final  List<LedgerSummaryBox> boxes = const <LedgerSummaryBox>[], this.netTotal = 0}): _boxes = boxes;
  factory _LedgerSummaryPanel.fromJson(Map<String, dynamic> json) => _$LedgerSummaryPanelFromJson(json);

 final  List<LedgerSummaryBox> _boxes;
@override@JsonKey() List<LedgerSummaryBox> get boxes {
  if (_boxes is EqualUnmodifiableListView) return _boxes;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_boxes);
}

@override@JsonKey() final  double netTotal;

/// Create a copy of LedgerSummaryPanel
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LedgerSummaryPanelCopyWith<_LedgerSummaryPanel> get copyWith => __$LedgerSummaryPanelCopyWithImpl<_LedgerSummaryPanel>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LedgerSummaryPanelToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LedgerSummaryPanel&&const DeepCollectionEquality().equals(other._boxes, _boxes)&&(identical(other.netTotal, netTotal) || other.netTotal == netTotal));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,const DeepCollectionEquality().hash(_boxes),netTotal);

@override
String toString() {
  return 'LedgerSummaryPanel(boxes: $boxes, netTotal: $netTotal)';
}


}

/// @nodoc
abstract mixin class _$LedgerSummaryPanelCopyWith<$Res> implements $LedgerSummaryPanelCopyWith<$Res> {
  factory _$LedgerSummaryPanelCopyWith(_LedgerSummaryPanel value, $Res Function(_LedgerSummaryPanel) _then) = __$LedgerSummaryPanelCopyWithImpl;
@override @useResult
$Res call({
 List<LedgerSummaryBox> boxes, double netTotal
});




}
/// @nodoc
class __$LedgerSummaryPanelCopyWithImpl<$Res>
    implements _$LedgerSummaryPanelCopyWith<$Res> {
  __$LedgerSummaryPanelCopyWithImpl(this._self, this._then);

  final _LedgerSummaryPanel _self;
  final $Res Function(_LedgerSummaryPanel) _then;

/// Create a copy of LedgerSummaryPanel
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? boxes = null,Object? netTotal = null,}) {
  return _then(_LedgerSummaryPanel(
boxes: null == boxes ? _self._boxes : boxes // ignore: cast_nullable_to_non_nullable
as List<LedgerSummaryBox>,netTotal: null == netTotal ? _self.netTotal : netTotal // ignore: cast_nullable_to_non_nullable
as double,
  ));
}


}

// dart format on
