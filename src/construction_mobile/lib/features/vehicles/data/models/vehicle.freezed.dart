// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'vehicle.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Vehicle {

 String get id; String get brand; String get model; String get registrationNumber; String? get vin; String? get qrCode; String get fuelType; String get status; String? get assignedEmployeeId; String? get assignedEmployeeName; String? get assignedEmployeeNumber; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of Vehicle
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$VehicleCopyWith<Vehicle> get copyWith => _$VehicleCopyWithImpl<Vehicle>(this as Vehicle, _$identity);

  /// Serializes this Vehicle to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Vehicle&&(identical(other.id, id) || other.id == id)&&(identical(other.brand, brand) || other.brand == brand)&&(identical(other.model, model) || other.model == model)&&(identical(other.registrationNumber, registrationNumber) || other.registrationNumber == registrationNumber)&&(identical(other.vin, vin) || other.vin == vin)&&(identical(other.qrCode, qrCode) || other.qrCode == qrCode)&&(identical(other.fuelType, fuelType) || other.fuelType == fuelType)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.assignedEmployeeNumber, assignedEmployeeNumber) || other.assignedEmployeeNumber == assignedEmployeeNumber)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,brand,model,registrationNumber,vin,qrCode,fuelType,status,assignedEmployeeId,assignedEmployeeName,assignedEmployeeNumber,createdAt,updatedAt);

@override
String toString() {
  return 'Vehicle(id: $id, brand: $brand, model: $model, registrationNumber: $registrationNumber, vin: $vin, qrCode: $qrCode, fuelType: $fuelType, status: $status, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, assignedEmployeeNumber: $assignedEmployeeNumber, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $VehicleCopyWith<$Res>  {
  factory $VehicleCopyWith(Vehicle value, $Res Function(Vehicle) _then) = _$VehicleCopyWithImpl;
@useResult
$Res call({
 String id, String brand, String model, String registrationNumber, String? vin, String? qrCode, String fuelType, String status, String? assignedEmployeeId, String? assignedEmployeeName, String? assignedEmployeeNumber, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$VehicleCopyWithImpl<$Res>
    implements $VehicleCopyWith<$Res> {
  _$VehicleCopyWithImpl(this._self, this._then);

  final Vehicle _self;
  final $Res Function(Vehicle) _then;

/// Create a copy of Vehicle
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? brand = null,Object? model = null,Object? registrationNumber = null,Object? vin = freezed,Object? qrCode = freezed,Object? fuelType = null,Object? status = null,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? assignedEmployeeNumber = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,brand: null == brand ? _self.brand : brand // ignore: cast_nullable_to_non_nullable
as String,model: null == model ? _self.model : model // ignore: cast_nullable_to_non_nullable
as String,registrationNumber: null == registrationNumber ? _self.registrationNumber : registrationNumber // ignore: cast_nullable_to_non_nullable
as String,vin: freezed == vin ? _self.vin : vin // ignore: cast_nullable_to_non_nullable
as String?,qrCode: freezed == qrCode ? _self.qrCode : qrCode // ignore: cast_nullable_to_non_nullable
as String?,fuelType: null == fuelType ? _self.fuelType : fuelType // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeNumber: freezed == assignedEmployeeNumber ? _self.assignedEmployeeNumber : assignedEmployeeNumber // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Vehicle].
extension VehiclePatterns on Vehicle {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Vehicle value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Vehicle() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Vehicle value)  $default,){
final _that = this;
switch (_that) {
case _Vehicle():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Vehicle value)?  $default,){
final _that = this;
switch (_that) {
case _Vehicle() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String brand,  String model,  String registrationNumber,  String? vin,  String? qrCode,  String fuelType, String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Vehicle() when $default != null:
return $default(_that.id,_that.brand,_that.model,_that.registrationNumber,_that.vin,_that.qrCode,_that.fuelType,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String brand,  String model,  String registrationNumber,  String? vin,  String? qrCode,  String fuelType, String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _Vehicle():
return $default(_that.id,_that.brand,_that.model,_that.registrationNumber,_that.vin,_that.qrCode,_that.fuelType,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String brand,  String model,  String registrationNumber,  String? vin,  String? qrCode,  String fuelType, String status,  String? assignedEmployeeId,  String? assignedEmployeeName,  String? assignedEmployeeNumber,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _Vehicle() when $default != null:
return $default(_that.id,_that.brand,_that.model,_that.registrationNumber,_that.vin,_that.qrCode,_that.fuelType,_that.status,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.assignedEmployeeNumber,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Vehicle extends Vehicle {
  const _Vehicle({required this.id, required this.brand, required this.model, required this.registrationNumber, this.vin, this.qrCode, required this.fuelType, required this.status, this.assignedEmployeeId, this.assignedEmployeeName, this.assignedEmployeeNumber, required this.createdAt, this.updatedAt}): super._();
  factory _Vehicle.fromJson(Map<String, dynamic> json) => _$VehicleFromJson(json);

@override final  String id;
@override final  String brand;
@override final  String model;
@override final  String registrationNumber;
@override final  String? vin;
@override final  String? qrCode;
@override final  String fuelType;
@override final  String status;
@override final  String? assignedEmployeeId;
@override final  String? assignedEmployeeName;
@override final  String? assignedEmployeeNumber;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of Vehicle
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$VehicleCopyWith<_Vehicle> get copyWith => __$VehicleCopyWithImpl<_Vehicle>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$VehicleToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Vehicle&&(identical(other.id, id) || other.id == id)&&(identical(other.brand, brand) || other.brand == brand)&&(identical(other.model, model) || other.model == model)&&(identical(other.registrationNumber, registrationNumber) || other.registrationNumber == registrationNumber)&&(identical(other.vin, vin) || other.vin == vin)&&(identical(other.qrCode, qrCode) || other.qrCode == qrCode)&&(identical(other.fuelType, fuelType) || other.fuelType == fuelType)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.assignedEmployeeNumber, assignedEmployeeNumber) || other.assignedEmployeeNumber == assignedEmployeeNumber)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,brand,model,registrationNumber,vin,qrCode,fuelType,status,assignedEmployeeId,assignedEmployeeName,assignedEmployeeNumber,createdAt,updatedAt);

@override
String toString() {
  return 'Vehicle(id: $id, brand: $brand, model: $model, registrationNumber: $registrationNumber, vin: $vin, qrCode: $qrCode, fuelType: $fuelType, status: $status, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, assignedEmployeeNumber: $assignedEmployeeNumber, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$VehicleCopyWith<$Res> implements $VehicleCopyWith<$Res> {
  factory _$VehicleCopyWith(_Vehicle value, $Res Function(_Vehicle) _then) = __$VehicleCopyWithImpl;
@override @useResult
$Res call({
 String id, String brand, String model, String registrationNumber, String? vin, String? qrCode, String fuelType, String status, String? assignedEmployeeId, String? assignedEmployeeName, String? assignedEmployeeNumber, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$VehicleCopyWithImpl<$Res>
    implements _$VehicleCopyWith<$Res> {
  __$VehicleCopyWithImpl(this._self, this._then);

  final _Vehicle _self;
  final $Res Function(_Vehicle) _then;

/// Create a copy of Vehicle
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? brand = null,Object? model = null,Object? registrationNumber = null,Object? vin = freezed,Object? qrCode = freezed,Object? fuelType = null,Object? status = null,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? assignedEmployeeNumber = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_Vehicle(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,brand: null == brand ? _self.brand : brand // ignore: cast_nullable_to_non_nullable
as String,model: null == model ? _self.model : model // ignore: cast_nullable_to_non_nullable
as String,registrationNumber: null == registrationNumber ? _self.registrationNumber : registrationNumber // ignore: cast_nullable_to_non_nullable
as String,vin: freezed == vin ? _self.vin : vin // ignore: cast_nullable_to_non_nullable
as String?,qrCode: freezed == qrCode ? _self.qrCode : qrCode // ignore: cast_nullable_to_non_nullable
as String?,fuelType: null == fuelType ? _self.fuelType : fuelType // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeNumber: freezed == assignedEmployeeNumber ? _self.assignedEmployeeNumber : assignedEmployeeNumber // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
