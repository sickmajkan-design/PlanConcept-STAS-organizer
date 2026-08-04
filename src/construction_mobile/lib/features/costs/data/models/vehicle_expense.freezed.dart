// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'vehicle_expense.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$VehicleExpense {

 String get id; String get vehicleId; String get vehicleName; String get kind; double get amount;/// `YYYY-MM-DD`.
 String get occurredOn;/// Only ever set on a fill-up.
 double? get litres; double? get pricePerLitre; int? get odometerKm; String? get supplier; String? get note; String? get recordedByName; DateTime get createdAt;
/// Create a copy of VehicleExpense
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$VehicleExpenseCopyWith<VehicleExpense> get copyWith => _$VehicleExpenseCopyWithImpl<VehicleExpense>(this as VehicleExpense, _$identity);

  /// Serializes this VehicleExpense to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is VehicleExpense&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.amount, amount) || other.amount == amount)&&(identical(other.occurredOn, occurredOn) || other.occurredOn == occurredOn)&&(identical(other.litres, litres) || other.litres == litres)&&(identical(other.pricePerLitre, pricePerLitre) || other.pricePerLitre == pricePerLitre)&&(identical(other.odometerKm, odometerKm) || other.odometerKm == odometerKm)&&(identical(other.supplier, supplier) || other.supplier == supplier)&&(identical(other.note, note) || other.note == note)&&(identical(other.recordedByName, recordedByName) || other.recordedByName == recordedByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,vehicleName,kind,amount,occurredOn,litres,pricePerLitre,odometerKm,supplier,note,recordedByName,createdAt);

@override
String toString() {
  return 'VehicleExpense(id: $id, vehicleId: $vehicleId, vehicleName: $vehicleName, kind: $kind, amount: $amount, occurredOn: $occurredOn, litres: $litres, pricePerLitre: $pricePerLitre, odometerKm: $odometerKm, supplier: $supplier, note: $note, recordedByName: $recordedByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $VehicleExpenseCopyWith<$Res>  {
  factory $VehicleExpenseCopyWith(VehicleExpense value, $Res Function(VehicleExpense) _then) = _$VehicleExpenseCopyWithImpl;
@useResult
$Res call({
 String id, String vehicleId, String vehicleName, String kind, double amount, String occurredOn, double? litres, double? pricePerLitre, int? odometerKm, String? supplier, String? note, String? recordedByName, DateTime createdAt
});




}
/// @nodoc
class _$VehicleExpenseCopyWithImpl<$Res>
    implements $VehicleExpenseCopyWith<$Res> {
  _$VehicleExpenseCopyWithImpl(this._self, this._then);

  final VehicleExpense _self;
  final $Res Function(VehicleExpense) _then;

/// Create a copy of VehicleExpense
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? vehicleId = null,Object? vehicleName = null,Object? kind = null,Object? amount = null,Object? occurredOn = null,Object? litres = freezed,Object? pricePerLitre = freezed,Object? odometerKm = freezed,Object? supplier = freezed,Object? note = freezed,Object? recordedByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,vehicleName: null == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,occurredOn: null == occurredOn ? _self.occurredOn : occurredOn // ignore: cast_nullable_to_non_nullable
as String,litres: freezed == litres ? _self.litres : litres // ignore: cast_nullable_to_non_nullable
as double?,pricePerLitre: freezed == pricePerLitre ? _self.pricePerLitre : pricePerLitre // ignore: cast_nullable_to_non_nullable
as double?,odometerKm: freezed == odometerKm ? _self.odometerKm : odometerKm // ignore: cast_nullable_to_non_nullable
as int?,supplier: freezed == supplier ? _self.supplier : supplier // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,recordedByName: freezed == recordedByName ? _self.recordedByName : recordedByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [VehicleExpense].
extension VehicleExpensePatterns on VehicleExpense {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _VehicleExpense value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _VehicleExpense() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _VehicleExpense value)  $default,){
final _that = this;
switch (_that) {
case _VehicleExpense():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _VehicleExpense value)?  $default,){
final _that = this;
switch (_that) {
case _VehicleExpense() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String vehicleName,  String kind,  double amount,  String occurredOn,  double? litres,  double? pricePerLitre,  int? odometerKm,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _VehicleExpense() when $default != null:
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.kind,_that.amount,_that.occurredOn,_that.litres,_that.pricePerLitre,_that.odometerKm,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String vehicleId,  String vehicleName,  String kind,  double amount,  String occurredOn,  double? litres,  double? pricePerLitre,  int? odometerKm,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _VehicleExpense():
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.kind,_that.amount,_that.occurredOn,_that.litres,_that.pricePerLitre,_that.odometerKm,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String vehicleId,  String vehicleName,  String kind,  double amount,  String occurredOn,  double? litres,  double? pricePerLitre,  int? odometerKm,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _VehicleExpense() when $default != null:
return $default(_that.id,_that.vehicleId,_that.vehicleName,_that.kind,_that.amount,_that.occurredOn,_that.litres,_that.pricePerLitre,_that.odometerKm,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _VehicleExpense extends VehicleExpense {
  const _VehicleExpense({required this.id, required this.vehicleId, required this.vehicleName, required this.kind, required this.amount, required this.occurredOn, this.litres, this.pricePerLitre, this.odometerKm, this.supplier, this.note, this.recordedByName, required this.createdAt}): super._();
  factory _VehicleExpense.fromJson(Map<String, dynamic> json) => _$VehicleExpenseFromJson(json);

@override final  String id;
@override final  String vehicleId;
@override final  String vehicleName;
@override final  String kind;
@override final  double amount;
/// `YYYY-MM-DD`.
@override final  String occurredOn;
/// Only ever set on a fill-up.
@override final  double? litres;
@override final  double? pricePerLitre;
@override final  int? odometerKm;
@override final  String? supplier;
@override final  String? note;
@override final  String? recordedByName;
@override final  DateTime createdAt;

/// Create a copy of VehicleExpense
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$VehicleExpenseCopyWith<_VehicleExpense> get copyWith => __$VehicleExpenseCopyWithImpl<_VehicleExpense>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$VehicleExpenseToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _VehicleExpense&&(identical(other.id, id) || other.id == id)&&(identical(other.vehicleId, vehicleId) || other.vehicleId == vehicleId)&&(identical(other.vehicleName, vehicleName) || other.vehicleName == vehicleName)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.amount, amount) || other.amount == amount)&&(identical(other.occurredOn, occurredOn) || other.occurredOn == occurredOn)&&(identical(other.litres, litres) || other.litres == litres)&&(identical(other.pricePerLitre, pricePerLitre) || other.pricePerLitre == pricePerLitre)&&(identical(other.odometerKm, odometerKm) || other.odometerKm == odometerKm)&&(identical(other.supplier, supplier) || other.supplier == supplier)&&(identical(other.note, note) || other.note == note)&&(identical(other.recordedByName, recordedByName) || other.recordedByName == recordedByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,vehicleId,vehicleName,kind,amount,occurredOn,litres,pricePerLitre,odometerKm,supplier,note,recordedByName,createdAt);

@override
String toString() {
  return 'VehicleExpense(id: $id, vehicleId: $vehicleId, vehicleName: $vehicleName, kind: $kind, amount: $amount, occurredOn: $occurredOn, litres: $litres, pricePerLitre: $pricePerLitre, odometerKm: $odometerKm, supplier: $supplier, note: $note, recordedByName: $recordedByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$VehicleExpenseCopyWith<$Res> implements $VehicleExpenseCopyWith<$Res> {
  factory _$VehicleExpenseCopyWith(_VehicleExpense value, $Res Function(_VehicleExpense) _then) = __$VehicleExpenseCopyWithImpl;
@override @useResult
$Res call({
 String id, String vehicleId, String vehicleName, String kind, double amount, String occurredOn, double? litres, double? pricePerLitre, int? odometerKm, String? supplier, String? note, String? recordedByName, DateTime createdAt
});




}
/// @nodoc
class __$VehicleExpenseCopyWithImpl<$Res>
    implements _$VehicleExpenseCopyWith<$Res> {
  __$VehicleExpenseCopyWithImpl(this._self, this._then);

  final _VehicleExpense _self;
  final $Res Function(_VehicleExpense) _then;

/// Create a copy of VehicleExpense
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? vehicleId = null,Object? vehicleName = null,Object? kind = null,Object? amount = null,Object? occurredOn = null,Object? litres = freezed,Object? pricePerLitre = freezed,Object? odometerKm = freezed,Object? supplier = freezed,Object? note = freezed,Object? recordedByName = freezed,Object? createdAt = null,}) {
  return _then(_VehicleExpense(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,vehicleId: null == vehicleId ? _self.vehicleId : vehicleId // ignore: cast_nullable_to_non_nullable
as String,vehicleName: null == vehicleName ? _self.vehicleName : vehicleName // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,occurredOn: null == occurredOn ? _self.occurredOn : occurredOn // ignore: cast_nullable_to_non_nullable
as String,litres: freezed == litres ? _self.litres : litres // ignore: cast_nullable_to_non_nullable
as double?,pricePerLitre: freezed == pricePerLitre ? _self.pricePerLitre : pricePerLitre // ignore: cast_nullable_to_non_nullable
as double?,odometerKm: freezed == odometerKm ? _self.odometerKm : odometerKm // ignore: cast_nullable_to_non_nullable
as int?,supplier: freezed == supplier ? _self.supplier : supplier // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,recordedByName: freezed == recordedByName ? _self.recordedByName : recordedByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
