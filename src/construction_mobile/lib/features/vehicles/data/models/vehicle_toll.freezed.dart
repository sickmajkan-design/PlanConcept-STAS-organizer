// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'vehicle_toll.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$VehicleToll {

 String get id; String get vehicleId; String get type; String get country; String? get routeSegment; String get status; DateTime? get validUntil; String get computedState; String? get paidByUserName; DateTime? get paidAt; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of VehicleToll
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$VehicleTollCopyWith<VehicleToll> get copyWith => _$VehicleTollCopyWithImpl<VehicleToll>(this as VehicleToll, _$identity);

  /// Serializes this VehicleToll to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is VehicleToll&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.type, type) || other.type == type)&&(identical(other.country, country) || other.country == country)&&(identical(other.routeSegment, routeSegment) || other.routeSegment == routeSegment)&&(identical(other.status, status) || other.status == status)&&(identical(other.validUntil, validUntil) || other.validUntil == validUntil)&&(identical(other.computedState, computedState) || other.computedState == computedState)&&(identical(other.paidByUserName, paidByUserName) || other.paidByUserName == paidByUserName)&&(identical(other.paidAt, paidAt) || other.paidAt == paidAt)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,type,country,routeSegment,status,validUntil,computedState,paidByUserName,paidAt,createdAt,updatedAt);

@override
String toString() {
  return 'VehicleToll(id: $id, vehicleId: $vehicleId, type: $type, country: $country, routeSegment: $routeSegment, status: $status, validUntil: $validUntil, computedState: $computedState, paidByUserName: $paidByUserName, paidAt: $paidAt, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $VehicleTollCopyWith<$Res>  {
  factory $VehicleTollCopyWith(VehicleToll value, $Res Function(VehicleToll) _then) = _$VehicleTollCopyWithImpl;
@useResult
$Res call({
 String id, String vehicleId, String type, String country, String? routeSegment, String status, DateTime? validUntil, String computedState, String? paidByUserName, DateTime? paidAt, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$VehicleTollCopyWithImpl<$Res>
    implements $VehicleTollCopyWith<$Res> {
  _$VehicleTollCopyWithImpl(this._self, this._then);

  final VehicleToll _self;
  final $Res Function(VehicleToll) _then;

/// Create a copy of VehicleToll
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? vehicleId = null,Object? type = null,Object? country = null,Object? routeSegment = freezed,Object? status = null,Object? validUntil = freezed,Object? computedState = null,Object? paidByUserName = freezed,Object? paidAt = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,country: null == country ? _self.country : country // ignore: cast_nullable_to_non_nullable
as String,routeSegment: freezed == routeSegment ? _self.routeSegment : routeSegment // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,validUntil: freezed == validUntil ? _self.validUntil : validUntil // ignore: cast_nullable_to_non_nullable
as DateTime?,computedState: null == computedState ? _self.computedState : computedState // ignore: cast_nullable_to_non_nullable
as String,paidByUserName: freezed == paidByUserName ? _self.paidByUserName : paidByUserName // ignore: cast_nullable_to_non_nullable
as String?,paidAt: freezed == paidAt ? _self.paidAt : paidAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [VehicleToll].
extension VehicleTollPatterns on VehicleToll {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _VehicleToll value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _VehicleToll() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _VehicleToll value)  $default,){
final _that = this;
switch (_that) {
case _VehicleToll():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _VehicleToll value)?  $default,){
final _that = this;
switch (_that) {
case _VehicleToll() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String type,  String country,  String? routeSegment,  String status,  DateTime? validUntil,  String computedState,  String? paidByUserName,  DateTime? paidAt,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _VehicleToll() when $default != null:
return $default(_that.id,_that.vehicleId,_that.type,_that.country,_that.routeSegment,_that.status,_that.validUntil,_that.computedState,_that.paidByUserName,_that.paidAt,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String type,  String country,  String? routeSegment,  String status,  DateTime? validUntil,  String computedState,  String? paidByUserName,  DateTime? paidAt,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _VehicleToll():
return $default(_that.id,_that.vehicleId,_that.type,_that.country,_that.routeSegment,_that.status,_that.validUntil,_that.computedState,_that.paidByUserName,_that.paidAt,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String vehicleId,  String type,  String country,  String? routeSegment,  String status,  DateTime? validUntil,  String computedState,  String? paidByUserName,  DateTime? paidAt,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _VehicleToll() when $default != null:
return $default(_that.id,_that.vehicleId,_that.type,_that.country,_that.routeSegment,_that.status,_that.validUntil,_that.computedState,_that.paidByUserName,_that.paidAt,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _VehicleToll implements VehicleToll {
  const _VehicleToll({required this.id, required this.vehicleId, required this.type, required this.country, this.routeSegment, required this.status, this.validUntil, required this.computedState, this.paidByUserName, this.paidAt, required this.createdAt, this.updatedAt});
  factory _VehicleToll.fromJson(Map<String, dynamic> json) => _$VehicleTollFromJson(json);

@override final  String id;
@override final  String vehicleId;
@override final  String type;
@override final  String country;
@override final  String? routeSegment;
@override final  String status;
@override final  DateTime? validUntil;
@override final  String computedState;
@override final  String? paidByUserName;
@override final  DateTime? paidAt;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of VehicleToll
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$VehicleTollCopyWith<_VehicleToll> get copyWith => __$VehicleTollCopyWithImpl<_VehicleToll>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$VehicleTollToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _VehicleToll&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.type, type) || other.type == type)&&(identical(other.country, country) || other.country == country)&&(identical(other.routeSegment, routeSegment) || other.routeSegment == routeSegment)&&(identical(other.status, status) || other.status == status)&&(identical(other.validUntil, validUntil) || other.validUntil == validUntil)&&(identical(other.computedState, computedState) || other.computedState == computedState)&&(identical(other.paidByUserName, paidByUserName) || other.paidByUserName == paidByUserName)&&(identical(other.paidAt, paidAt) || other.paidAt == paidAt)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,type,country,routeSegment,status,validUntil,computedState,paidByUserName,paidAt,createdAt,updatedAt);

@override
String toString() {
  return 'VehicleToll(id: $id, vehicleId: $vehicleId, type: $type, country: $country, routeSegment: $routeSegment, status: $status, validUntil: $validUntil, computedState: $computedState, paidByUserName: $paidByUserName, paidAt: $paidAt, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$VehicleTollCopyWith<$Res> implements $VehicleTollCopyWith<$Res> {
  factory _$VehicleTollCopyWith(_VehicleToll value, $Res Function(_VehicleToll) _then) = __$VehicleTollCopyWithImpl;
@override @useResult
$Res call({
 String id, String vehicleId, String type, String country, String? routeSegment, String status, DateTime? validUntil, String computedState, String? paidByUserName, DateTime? paidAt, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$VehicleTollCopyWithImpl<$Res>
    implements _$VehicleTollCopyWith<$Res> {
  __$VehicleTollCopyWithImpl(this._self, this._then);

  final _VehicleToll _self;
  final $Res Function(_VehicleToll) _then;

/// Create a copy of VehicleToll
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? vehicleId = null,Object? type = null,Object? country = null,Object? routeSegment = freezed,Object? status = null,Object? validUntil = freezed,Object? computedState = null,Object? paidByUserName = freezed,Object? paidAt = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_VehicleToll(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,country: null == country ? _self.country : country // ignore: cast_nullable_to_non_nullable
as String,routeSegment: freezed == routeSegment ? _self.routeSegment : routeSegment // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,validUntil: freezed == validUntil ? _self.validUntil : validUntil // ignore: cast_nullable_to_non_nullable
as DateTime?,computedState: null == computedState ? _self.computedState : computedState // ignore: cast_nullable_to_non_nullable
as String,paidByUserName: freezed == paidByUserName ? _self.paidByUserName : paidByUserName // ignore: cast_nullable_to_non_nullable
as String?,paidAt: freezed == paidAt ? _self.paidAt : paidAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
