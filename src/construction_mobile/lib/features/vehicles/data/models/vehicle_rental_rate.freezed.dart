// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'vehicle_rental_rate.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$VehicleRentalRate {

 String get id; String get vehicleId; String get vehicleName; double get monthlyAmount; String? get provider; DateTime get startDate; DateTime? get endDate; String? get note; String? get setByName; DateTime get createdAt;
/// Create a copy of VehicleRentalRate
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$VehicleRentalRateCopyWith<VehicleRentalRate> get copyWith => _$VehicleRentalRateCopyWithImpl<VehicleRentalRate>(this as VehicleRentalRate, _$identity);

  /// Serializes this VehicleRentalRate to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is VehicleRentalRate&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.monthlyAmount, monthlyAmount) || other.monthlyAmount == monthlyAmount)&&(identical(other.provider, provider) || other.provider == provider)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,vehicleName,monthlyAmount,provider,startDate,endDate,note,setByName,createdAt);

@override
String toString() {
  return 'VehicleRentalRate(id: $id, vehicleId: $vehicleId, vehicleName: $vehicleName, monthlyAmount: $monthlyAmount, provider: $provider, startDate: $startDate, endDate: $endDate, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $VehicleRentalRateCopyWith<$Res>  {
  factory $VehicleRentalRateCopyWith(VehicleRentalRate value, $Res Function(VehicleRentalRate) _then) = _$VehicleRentalRateCopyWithImpl;
@useResult
$Res call({
 String id, String vehicleId, String vehicleName, double monthlyAmount, String? provider, DateTime startDate, DateTime? endDate, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class _$VehicleRentalRateCopyWithImpl<$Res>
    implements $VehicleRentalRateCopyWith<$Res> {
  _$VehicleRentalRateCopyWithImpl(this._self, this._then);

  final VehicleRentalRate _self;
  final $Res Function(VehicleRentalRate) _then;

/// Create a copy of VehicleRentalRate
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? vehicleId = null,Object? vehicleName = null,Object? monthlyAmount = null,Object? provider = freezed,Object? startDate = null,Object? endDate = freezed,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,vehicleName: null == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String,monthlyAmount: null == monthlyAmount ? _self.monthlyAmount : monthlyAmount // ignore: cast_nullable_to_non_nullable
as double,provider: freezed == provider ? _self.provider : provider // ignore: cast_nullable_to_non_nullable
as String?,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,setByName: freezed == setByName ? _self.setByName : setByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [VehicleRentalRate].
extension VehicleRentalRatePatterns on VehicleRentalRate {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _VehicleRentalRate value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _VehicleRentalRate() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _VehicleRentalRate value)  $default,){
final _that = this;
switch (_that) {
case _VehicleRentalRate():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _VehicleRentalRate value)?  $default,){
final _that = this;
switch (_that) {
case _VehicleRentalRate() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String vehicleName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _VehicleRentalRate() when $default != null:
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String vehicleName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _VehicleRentalRate():
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String vehicleId,  String vehicleName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _VehicleRentalRate() when $default != null:
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _VehicleRentalRate implements VehicleRentalRate {
  const _VehicleRentalRate({required this.id, required this.vehicleId, required this.vehicleName, required this.monthlyAmount, this.provider, required this.startDate, this.endDate, this.note, this.setByName, required this.createdAt});
  factory _VehicleRentalRate.fromJson(Map<String, dynamic> json) => _$VehicleRentalRateFromJson(json);

@override final  String id;
@override final  String vehicleId;
@override final  String vehicleName;
@override final  double monthlyAmount;
@override final  String? provider;
@override final  DateTime startDate;
@override final  DateTime? endDate;
@override final  String? note;
@override final  String? setByName;
@override final  DateTime createdAt;

/// Create a copy of VehicleRentalRate
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$VehicleRentalRateCopyWith<_VehicleRentalRate> get copyWith => __$VehicleRentalRateCopyWithImpl<_VehicleRentalRate>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$VehicleRentalRateToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _VehicleRentalRate&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.monthlyAmount, monthlyAmount) || other.monthlyAmount == monthlyAmount)&&(identical(other.provider, provider) || other.provider == provider)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,vehicleName,monthlyAmount,provider,startDate,endDate,note,setByName,createdAt);

@override
String toString() {
  return 'VehicleRentalRate(id: $id, vehicleId: $vehicleId, vehicleName: $vehicleName, monthlyAmount: $monthlyAmount, provider: $provider, startDate: $startDate, endDate: $endDate, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$VehicleRentalRateCopyWith<$Res> implements $VehicleRentalRateCopyWith<$Res> {
  factory _$VehicleRentalRateCopyWith(_VehicleRentalRate value, $Res Function(_VehicleRentalRate) _then) = __$VehicleRentalRateCopyWithImpl;
@override @useResult
$Res call({
 String id, String vehicleId, String vehicleName, double monthlyAmount, String? provider, DateTime startDate, DateTime? endDate, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class __$VehicleRentalRateCopyWithImpl<$Res>
    implements _$VehicleRentalRateCopyWith<$Res> {
  __$VehicleRentalRateCopyWithImpl(this._self, this._then);

  final _VehicleRentalRate _self;
  final $Res Function(_VehicleRentalRate) _then;

/// Create a copy of VehicleRentalRate
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? vehicleId = null,Object? vehicleName = null,Object? monthlyAmount = null,Object? provider = freezed,Object? startDate = null,Object? endDate = freezed,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_VehicleRentalRate(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,vehicleName: null == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String,monthlyAmount: null == monthlyAmount ? _self.monthlyAmount : monthlyAmount // ignore: cast_nullable_to_non_nullable
as double,provider: freezed == provider ? _self.provider : provider // ignore: cast_nullable_to_non_nullable
as String?,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,setByName: freezed == setByName ? _self.setByName : setByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
