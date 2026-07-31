// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'material.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$MaterialItem {

 String get id; String get name; String get unit; double get quantity; String? get warehouse; String? get projectId; String? get projectName; DateTime get lastUpdated; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of MaterialItem
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$MaterialItemCopyWith<MaterialItem> get copyWith => _$MaterialItemCopyWithImpl<MaterialItem>(this as MaterialItem, _$identity);

  /// Serializes this MaterialItem to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is MaterialItem&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.unit, unit) || other.unit == unit)&&(identical(other.quantity, quantity) || other.quantity == quantity)&&(identical(other.warehouse, warehouse) || other.warehouse == warehouse)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.lastUpdated, lastUpdated) || other.lastUpdated == lastUpdated)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,unit,quantity,warehouse,projectId,projectName,lastUpdated,createdAt,updatedAt);

@override
String toString() {
  return 'MaterialItem(id: $id, name: $name, unit: $unit, quantity: $quantity, warehouse: $warehouse, projectId: $projectId, projectName: $projectName, lastUpdated: $lastUpdated, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $MaterialItemCopyWith<$Res>  {
  factory $MaterialItemCopyWith(MaterialItem value, $Res Function(MaterialItem) _then) = _$MaterialItemCopyWithImpl;
@useResult
$Res call({
 String id, String name, String unit, double quantity, String? warehouse, String? projectId, String? projectName, DateTime lastUpdated, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$MaterialItemCopyWithImpl<$Res>
    implements $MaterialItemCopyWith<$Res> {
  _$MaterialItemCopyWithImpl(this._self, this._then);

  final MaterialItem _self;
  final $Res Function(MaterialItem) _then;

/// Create a copy of MaterialItem
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? unit = null,Object? quantity = null,Object? warehouse = freezed,Object? projectId = freezed,Object? projectName = freezed,Object? lastUpdated = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,unit: null == unit ? _self.unit : unit // ignore: cast_nullable_to_non_nullable
as String,quantity: null == quantity ? _self.quantity : quantity // ignore: cast_nullable_to_non_nullable
as double,warehouse: freezed == warehouse ? _self.warehouse : warehouse // ignore: cast_nullable_to_non_nullable
as String?,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,lastUpdated: null == lastUpdated ? _self.lastUpdated : lastUpdated // ignore: cast_nullable_to_non_nullable
as DateTime,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [MaterialItem].
extension MaterialItemPatterns on MaterialItem {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _MaterialItem value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _MaterialItem() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _MaterialItem value)  $default,){
final _that = this;
switch (_that) {
case _MaterialItem():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _MaterialItem value)?  $default,){
final _that = this;
switch (_that) {
case _MaterialItem() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String unit,  double quantity,  String? warehouse,  String? projectId,  String? projectName,  DateTime lastUpdated,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _MaterialItem() when $default != null:
return $default(_that.id,_that.name,_that.unit,_that.quantity,_that.warehouse,_that.projectId,_that.projectName,_that.lastUpdated,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String unit,  double quantity,  String? warehouse,  String? projectId,  String? projectName,  DateTime lastUpdated,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _MaterialItem():
return $default(_that.id,_that.name,_that.unit,_that.quantity,_that.warehouse,_that.projectId,_that.projectName,_that.lastUpdated,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String unit,  double quantity,  String? warehouse,  String? projectId,  String? projectName,  DateTime lastUpdated,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _MaterialItem() when $default != null:
return $default(_that.id,_that.name,_that.unit,_that.quantity,_that.warehouse,_that.projectId,_that.projectName,_that.lastUpdated,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _MaterialItem extends MaterialItem {
  const _MaterialItem({required this.id, required this.name, required this.unit, required this.quantity, this.warehouse, this.projectId, this.projectName, required this.lastUpdated, required this.createdAt, this.updatedAt}): super._();
  factory _MaterialItem.fromJson(Map<String, dynamic> json) => _$MaterialItemFromJson(json);

@override final  String id;
@override final  String name;
@override final  String unit;
@override final  double quantity;
@override final  String? warehouse;
@override final  String? projectId;
@override final  String? projectName;
@override final  DateTime lastUpdated;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of MaterialItem
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$MaterialItemCopyWith<_MaterialItem> get copyWith => __$MaterialItemCopyWithImpl<_MaterialItem>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$MaterialItemToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _MaterialItem&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.unit, unit) || other.unit == unit)&&(identical(other.quantity, quantity) || other.quantity == quantity)&&(identical(other.warehouse, warehouse) || other.warehouse == warehouse)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.lastUpdated, lastUpdated) || other.lastUpdated == lastUpdated)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,unit,quantity,warehouse,projectId,projectName,lastUpdated,createdAt,updatedAt);

@override
String toString() {
  return 'MaterialItem(id: $id, name: $name, unit: $unit, quantity: $quantity, warehouse: $warehouse, projectId: $projectId, projectName: $projectName, lastUpdated: $lastUpdated, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$MaterialItemCopyWith<$Res> implements $MaterialItemCopyWith<$Res> {
  factory _$MaterialItemCopyWith(_MaterialItem value, $Res Function(_MaterialItem) _then) = __$MaterialItemCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String unit, double quantity, String? warehouse, String? projectId, String? projectName, DateTime lastUpdated, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$MaterialItemCopyWithImpl<$Res>
    implements _$MaterialItemCopyWith<$Res> {
  __$MaterialItemCopyWithImpl(this._self, this._then);

  final _MaterialItem _self;
  final $Res Function(_MaterialItem) _then;

/// Create a copy of MaterialItem
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? unit = null,Object? quantity = null,Object? warehouse = freezed,Object? projectId = freezed,Object? projectName = freezed,Object? lastUpdated = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_MaterialItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,unit: null == unit ? _self.unit : unit // ignore: cast_nullable_to_non_nullable
as String,quantity: null == quantity ? _self.quantity : quantity // ignore: cast_nullable_to_non_nullable
as double,warehouse: freezed == warehouse ? _self.warehouse : warehouse // ignore: cast_nullable_to_non_nullable
as String?,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,lastUpdated: null == lastUpdated ? _self.lastUpdated : lastUpdated // ignore: cast_nullable_to_non_nullable
as DateTime,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
