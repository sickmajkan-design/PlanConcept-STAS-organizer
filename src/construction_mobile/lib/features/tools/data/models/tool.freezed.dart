// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'tool.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Tool {

 String get id; String get name; String? get category; String? get serialNumber; String? get qrCode; String get status; String? get assignedEmployeeId; String? get assignedEmployeeName; String? get assignedEmployeeNumber; String? get assignedProjectId; String? get assignedProjectName; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of Tool
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ToolCopyWith<Tool> get copyWith => _$ToolCopyWithImpl<Tool>(this as Tool, _$identity);

  /// Serializes this Tool to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Tool&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.category, category) || other.category == category)&&(identical(other.serialNumber, serialNumber) || other.serialNumber == serialNumber)&&(identical(other.qrCode, qrCode) || other.qrCode == qrCode)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.assignedEmployeeNumber, assignedEmployeeNumber) || other.assignedEmployeeNumber == assignedEmployeeNumber)&&(identical(other.assignedProjectId, assignedProjectId) || other.assignedProjectId == assignedProjectId)&&(identical(other.assignedProjectName, assignedProjectName) || other.assignedProjectName == assignedProjectName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,category,serialNumber,qrCode,status,assignedEmployeeId,assignedEmployeeName,assignedEmployeeNumber,assignedProjectId,assignedProjectName,createdAt,updatedAt);

@override
String toString() {
  return 'Tool(id: $id, name: $name, category: $category, serialNumber: $serialNumber, qrCode: $qrCode, status: $status, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, assignedEmployeeNumber: $assignedEmployeeNumber, assignedProjectId: $assignedProjectId, assignedProjectName: $assignedProjectName, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $ToolCopyWith<$Res>  {
  factory $ToolCopyWith(Tool value, $Res Function(Tool) _then) = _$ToolCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? category, String? serialNumber, String? qrCode, String status, String? assignedEmployeeId, String? assignedEmployeeName, String? assignedEmployeeNumber, String? assignedProjectId, String? assignedProjectName, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$ToolCopyWithImpl<$Res>
    implements $ToolCopyWith<$Res> {
  _$ToolCopyWithImpl(this._self, this._then);

  final Tool _self;
  final $Res Function(Tool) _then;

/// Create a copy of Tool
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? category = freezed,Object? serialNumber = freezed,Object? qrCode = freezed,Object? status = null,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? assignedEmployeeNumber = freezed,Object? assignedProjectId = freezed,Object? assignedProjectName = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,category: freezed == category ? _self.category : category // ignore: cast_nullable_to_non_nullable
as String?,serialNumber: freezed == serialNumber ? _self.serialNumber : serialNumber // ignore: cast_nullable_to_non_nullable
as String?,qrCode: freezed == qrCode ? _self.qrCode : qrCode // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeNumber: freezed == assignedEmployeeNumber ? _self.assignedEmployeeNumber : assignedEmployeeNumber // ignore: cast_nullable_to_non_nullable
as String?,assignedProjectId: freezed == assignedProjectId ? _self.assignedProjectId : assignedProjectId // ignore: cast_nullable_to_non_nullable
as String?,assignedProjectName: freezed == assignedProjectName ? _self.assignedProjectName : assignedProjectName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Tool].
extension ToolPatterns on Tool {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Tool value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Tool() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Tool value)  $default,){
final _that = this;
switch (_that) {
case _Tool():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Tool value)?  $default,){
final _that = this;
switch (_that) {
case _Tool() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? category,  String? serialNumber,  String? qrCode,  String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  String? assignedProjectId,  String? assignedProjectName,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Tool() when $default != null:
return $default(_that.id,_that.name,_that.category,_that.serialNumber,_that.qrCode,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.assignedProjectId,_that.assignedProjectName,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? category,  String? serialNumber,  String? qrCode,  String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  String? assignedProjectId,  String? assignedProjectName,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _Tool():
return $default(_that.id,_that.name,_that.category,_that.serialNumber,_that.qrCode,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.assignedProjectId,_that.assignedProjectName,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? category,  String? serialNumber,  String? qrCode,  String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  String? assignedProjectId,  String? assignedProjectName,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _Tool() when $default != null:
return $default(_that.id,_that.name,_that.category,_that.serialNumber,_that.qrCode,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.assignedProjectId,_that.assignedProjectName,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Tool extends Tool {
  const _Tool({required this.id, required this.name, this.category, this.serialNumber, this.qrCode, required this.status, this.assignedEmployeeId, this.assignedEmployeeName, this.assignedEmployeeNumber, this.assignedProjectId, this.assignedProjectName, required this.createdAt, this.updatedAt}): super._();
  factory _Tool.fromJson(Map<String, dynamic> json) => _$ToolFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? category;
@override final  String? serialNumber;
@override final  String? qrCode;
@override final  String status;
@override final  String? assignedEmployeeId;
@override final  String? assignedEmployeeName;
@override final  String? assignedEmployeeNumber;
@override final  String? assignedProjectId;
@override final  String? assignedProjectName;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of Tool
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ToolCopyWith<_Tool> get copyWith => __$ToolCopyWithImpl<_Tool>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ToolToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Tool&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.category, category) || other.category == category)&&(identical(other.serialNumber, serialNumber) || other.serialNumber == serialNumber)&&(identical(other.qrCode, qrCode) || other.qrCode == qrCode)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.assignedEmployeeNumber, assignedEmployeeNumber) || other.assignedEmployeeNumber == assignedEmployeeNumber)&&(identical(other.assignedProjectId, assignedProjectId) || other.assignedProjectId == assignedProjectId)&&(identical(other.assignedProjectName, assignedProjectName) || other.assignedProjectName == assignedProjectName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,category,serialNumber,qrCode,status,assignedEmployeeId,assignedEmployeeName,assignedEmployeeNumber,assignedProjectId,assignedProjectName,createdAt,updatedAt);

@override
String toString() {
  return 'Tool(id: $id, name: $name, category: $category, serialNumber: $serialNumber, qrCode: $qrCode, status: $status, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, assignedEmployeeNumber: $assignedEmployeeNumber, assignedProjectId: $assignedProjectId, assignedProjectName: $assignedProjectName, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$ToolCopyWith<$Res> implements $ToolCopyWith<$Res> {
  factory _$ToolCopyWith(_Tool value, $Res Function(_Tool) _then) = __$ToolCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? category, String? serialNumber, String? qrCode, String status, String? assignedEmployeeId, String? assignedEmployeeName, String? assignedEmployeeNumber, String? assignedProjectId, String? assignedProjectName, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$ToolCopyWithImpl<$Res>
    implements _$ToolCopyWith<$Res> {
  __$ToolCopyWithImpl(this._self, this._then);

  final _Tool _self;
  final $Res Function(_Tool) _then;

/// Create a copy of Tool
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? category = freezed,Object? serialNumber = freezed,Object? qrCode = freezed,Object? status = null,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? assignedEmployeeNumber = freezed,Object? assignedProjectId = freezed,Object? assignedProjectName = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_Tool(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,category: freezed == category ? _self.category : category // ignore: cast_nullable_to_non_nullable
as String?,serialNumber: freezed == serialNumber ? _self.serialNumber : serialNumber // ignore: cast_nullable_to_non_nullable
as String?,qrCode: freezed == qrCode ? _self.qrCode : qrCode // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeNumber: freezed == assignedEmployeeNumber ? _self.assignedEmployeeNumber : assignedEmployeeNumber // ignore: cast_nullable_to_non_nullable
as String?,assignedProjectId: freezed == assignedProjectId ? _self.assignedProjectId : assignedProjectId // ignore: cast_nullable_to_non_nullable
as String?,assignedProjectName: freezed == assignedProjectName ? _self.assignedProjectName : assignedProjectName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
