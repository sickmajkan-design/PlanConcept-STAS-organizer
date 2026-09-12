// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'tool_rental_rate.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$ToolRentalRate {

 String get id; String get toolId; String get toolName; double get monthlyAmount; String? get provider; DateTime get startDate; DateTime? get endDate; String? get note; String? get setByName; DateTime get createdAt;
/// Create a copy of ToolRentalRate
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ToolRentalRateCopyWith<ToolRentalRate> get copyWith => _$ToolRentalRateCopyWithImpl<ToolRentalRate>(this as ToolRentalRate, _$identity);

  /// Serializes this ToolRentalRate to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ToolRentalRate&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.monthlyAmount, monthlyAmount) || other.monthlyAmount == monthlyAmount)&&(identical(other.provider, provider) || other.provider == provider)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,monthlyAmount,provider,startDate,endDate,note,setByName,createdAt);

@override
String toString() {
  return 'ToolRentalRate(id: $id, toolId: $toolId, toolName: $toolName, monthlyAmount: $monthlyAmount, provider: $provider, startDate: $startDate, endDate: $endDate, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $ToolRentalRateCopyWith<$Res>  {
  factory $ToolRentalRateCopyWith(ToolRentalRate value, $Res Function(ToolRentalRate) _then) = _$ToolRentalRateCopyWithImpl;
@useResult
$Res call({
 String id, String toolId, String toolName, double monthlyAmount, String? provider, DateTime startDate, DateTime? endDate, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class _$ToolRentalRateCopyWithImpl<$Res>
    implements $ToolRentalRateCopyWith<$Res> {
  _$ToolRentalRateCopyWithImpl(this._self, this._then);

  final ToolRentalRate _self;
  final $Res Function(ToolRentalRate) _then;

/// Create a copy of ToolRentalRate
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? monthlyAmount = null,Object? provider = freezed,Object? startDate = null,Object? endDate = freezed,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
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


/// Adds pattern-matching-related methods to [ToolRentalRate].
extension ToolRentalRatePatterns on ToolRentalRate {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ToolRentalRate value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ToolRentalRate() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ToolRentalRate value)  $default,){
final _that = this;
switch (_that) {
case _ToolRentalRate():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ToolRentalRate value)?  $default,){
final _that = this;
switch (_that) {
case _ToolRentalRate() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ToolRentalRate() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _ToolRentalRate():
return $default(_that.id,_that.toolId,_that.toolName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String toolId,  String toolName,  double monthlyAmount,  String? provider,  DateTime startDate,  DateTime? endDate,  String? note,  String? setByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _ToolRentalRate() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.monthlyAmount,_that.provider,_that.startDate,_that.endDate,_that.note,_that.setByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ToolRentalRate implements ToolRentalRate {
  const _ToolRentalRate({required this.id, required this.toolId, required this.toolName, required this.monthlyAmount, this.provider, required this.startDate, this.endDate, this.note, this.setByName, required this.createdAt});
  factory _ToolRentalRate.fromJson(Map<String, dynamic> json) => _$ToolRentalRateFromJson(json);

@override final  String id;
@override final  String toolId;
@override final  String toolName;
@override final  double monthlyAmount;
@override final  String? provider;
@override final  DateTime startDate;
@override final  DateTime? endDate;
@override final  String? note;
@override final  String? setByName;
@override final  DateTime createdAt;

/// Create a copy of ToolRentalRate
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ToolRentalRateCopyWith<_ToolRentalRate> get copyWith => __$ToolRentalRateCopyWithImpl<_ToolRentalRate>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ToolRentalRateToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ToolRentalRate&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.monthlyAmount, monthlyAmount) || other.monthlyAmount == monthlyAmount)&&(identical(other.provider, provider) || other.provider == provider)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,monthlyAmount,provider,startDate,endDate,note,setByName,createdAt);

@override
String toString() {
  return 'ToolRentalRate(id: $id, toolId: $toolId, toolName: $toolName, monthlyAmount: $monthlyAmount, provider: $provider, startDate: $startDate, endDate: $endDate, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$ToolRentalRateCopyWith<$Res> implements $ToolRentalRateCopyWith<$Res> {
  factory _$ToolRentalRateCopyWith(_ToolRentalRate value, $Res Function(_ToolRentalRate) _then) = __$ToolRentalRateCopyWithImpl;
@override @useResult
$Res call({
 String id, String toolId, String toolName, double monthlyAmount, String? provider, DateTime startDate, DateTime? endDate, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class __$ToolRentalRateCopyWithImpl<$Res>
    implements _$ToolRentalRateCopyWith<$Res> {
  __$ToolRentalRateCopyWithImpl(this._self, this._then);

  final _ToolRentalRate _self;
  final $Res Function(_ToolRentalRate) _then;

/// Create a copy of ToolRentalRate
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? monthlyAmount = null,Object? provider = freezed,Object? startDate = null,Object? endDate = freezed,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_ToolRentalRate(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
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
