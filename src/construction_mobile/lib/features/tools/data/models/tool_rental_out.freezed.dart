// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'tool_rental_out.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$ToolRentalOut {

 String get id; String get toolId; String get toolName; String? get customerId; String get renterDisplayName; String get renterName; double get dailyRate; DateTime get startDate; DateTime? get endDate; bool get isOpen; String? get note; String? get setByName; DateTime get createdAt;
/// Create a copy of ToolRentalOut
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ToolRentalOutCopyWith<ToolRentalOut> get copyWith => _$ToolRentalOutCopyWithImpl<ToolRentalOut>(this as ToolRentalOut, _$identity);

  /// Serializes this ToolRentalOut to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ToolRentalOut&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.customerId, customerId) || other.customerId == customerId)&&(identical(other.renterDisplayName, renterDisplayName) || other.renterDisplayName == renterDisplayName)&&(identical(other.renterName, renterName) || other.renterName == renterName)&&(identical(other.dailyRate, dailyRate) || other.dailyRate == dailyRate)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.isOpen, isOpen) || other.isOpen == isOpen)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,customerId,renterDisplayName,renterName,dailyRate,startDate,endDate,isOpen,note,setByName,createdAt);

@override
String toString() {
  return 'ToolRentalOut(id: $id, toolId: $toolId, toolName: $toolName, customerId: $customerId, renterDisplayName: $renterDisplayName, renterName: $renterName, dailyRate: $dailyRate, startDate: $startDate, endDate: $endDate, isOpen: $isOpen, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $ToolRentalOutCopyWith<$Res>  {
  factory $ToolRentalOutCopyWith(ToolRentalOut value, $Res Function(ToolRentalOut) _then) = _$ToolRentalOutCopyWithImpl;
@useResult
$Res call({
 String id, String toolId, String toolName, String? customerId, String renterDisplayName, String renterName, double dailyRate, DateTime startDate, DateTime? endDate, bool isOpen, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class _$ToolRentalOutCopyWithImpl<$Res>
    implements $ToolRentalOutCopyWith<$Res> {
  _$ToolRentalOutCopyWithImpl(this._self, this._then);

  final ToolRentalOut _self;
  final $Res Function(ToolRentalOut) _then;

/// Create a copy of ToolRentalOut
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? customerId = freezed,Object? renterDisplayName = null,Object? renterName = null,Object? dailyRate = null,Object? startDate = null,Object? endDate = freezed,Object? isOpen = null,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String,customerId: freezed == customerId ? _self.customerId : customerId // ignore: cast_nullable_to_non_nullable
as String?,renterDisplayName: null == renterDisplayName ? _self.renterDisplayName : renterDisplayName // ignore: cast_nullable_to_non_nullable
as String,renterName: null == renterName ? _self.renterName : renterName // ignore: cast_nullable_to_non_nullable
as String,dailyRate: null == dailyRate ? _self.dailyRate : dailyRate // ignore: cast_nullable_to_non_nullable
as double,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,isOpen: null == isOpen ? _self.isOpen : isOpen // ignore: cast_nullable_to_non_nullable
as bool,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,setByName: freezed == setByName ? _self.setByName : setByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [ToolRentalOut].
extension ToolRentalOutPatterns on ToolRentalOut {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ToolRentalOut value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ToolRentalOut() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ToolRentalOut value)  $default,){
final _that = this;
switch (_that) {
case _ToolRentalOut():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ToolRentalOut value)?  $default,){
final _that = this;
switch (_that) {
case _ToolRentalOut() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  String? customerId,  String renterDisplayName,  String renterName,  double dailyRate,  DateTime startDate,  DateTime? endDate,  bool isOpen,  String? note,  String? setByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ToolRentalOut() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.customerId,_that.renterDisplayName,_that.renterName,_that.dailyRate,_that.startDate,_that.endDate,_that.isOpen,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  String? customerId,  String renterDisplayName,  String renterName,  double dailyRate,  DateTime startDate,  DateTime? endDate,  bool isOpen,  String? note,  String? setByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _ToolRentalOut():
return $default(_that.id,_that.toolId,_that.toolName,_that.customerId,_that.renterDisplayName,_that.renterName,_that.dailyRate,_that.startDate,_that.endDate,_that.isOpen,_that.note,_that.setByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String toolId,  String toolName,  String? customerId,  String renterDisplayName,  String renterName,  double dailyRate,  DateTime startDate,  DateTime? endDate,  bool isOpen,  String? note,  String? setByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _ToolRentalOut() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.customerId,_that.renterDisplayName,_that.renterName,_that.dailyRate,_that.startDate,_that.endDate,_that.isOpen,_that.note,_that.setByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ToolRentalOut implements ToolRentalOut {
  const _ToolRentalOut({required this.id, required this.toolId, required this.toolName, this.customerId, required this.renterDisplayName, required this.renterName, required this.dailyRate, required this.startDate, this.endDate, required this.isOpen, this.note, this.setByName, required this.createdAt});
  factory _ToolRentalOut.fromJson(Map<String, dynamic> json) => _$ToolRentalOutFromJson(json);

@override final  String id;
@override final  String toolId;
@override final  String toolName;
@override final  String? customerId;
@override final  String renterDisplayName;
@override final  String renterName;
@override final  double dailyRate;
@override final  DateTime startDate;
@override final  DateTime? endDate;
@override final  bool isOpen;
@override final  String? note;
@override final  String? setByName;
@override final  DateTime createdAt;

/// Create a copy of ToolRentalOut
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ToolRentalOutCopyWith<_ToolRentalOut> get copyWith => __$ToolRentalOutCopyWithImpl<_ToolRentalOut>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ToolRentalOutToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ToolRentalOut&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.customerId, customerId) || other.customerId == customerId)&&(identical(other.renterDisplayName, renterDisplayName) || other.renterDisplayName == renterDisplayName)&&(identical(other.renterName, renterName) || other.renterName == renterName)&&(identical(other.dailyRate, dailyRate) || other.dailyRate == dailyRate)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.isOpen, isOpen) || other.isOpen == isOpen)&&(identical(other.note, note) || other.note == note)&&(identical(other.setByName, setByName) || other.setByName == setByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,customerId,renterDisplayName,renterName,dailyRate,startDate,endDate,isOpen,note,setByName,createdAt);

@override
String toString() {
  return 'ToolRentalOut(id: $id, toolId: $toolId, toolName: $toolName, customerId: $customerId, renterDisplayName: $renterDisplayName, renterName: $renterName, dailyRate: $dailyRate, startDate: $startDate, endDate: $endDate, isOpen: $isOpen, note: $note, setByName: $setByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$ToolRentalOutCopyWith<$Res> implements $ToolRentalOutCopyWith<$Res> {
  factory _$ToolRentalOutCopyWith(_ToolRentalOut value, $Res Function(_ToolRentalOut) _then) = __$ToolRentalOutCopyWithImpl;
@override @useResult
$Res call({
 String id, String toolId, String toolName, String? customerId, String renterDisplayName, String renterName, double dailyRate, DateTime startDate, DateTime? endDate, bool isOpen, String? note, String? setByName, DateTime createdAt
});




}
/// @nodoc
class __$ToolRentalOutCopyWithImpl<$Res>
    implements _$ToolRentalOutCopyWith<$Res> {
  __$ToolRentalOutCopyWithImpl(this._self, this._then);

  final _ToolRentalOut _self;
  final $Res Function(_ToolRentalOut) _then;

/// Create a copy of ToolRentalOut
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? customerId = freezed,Object? renterDisplayName = null,Object? renterName = null,Object? dailyRate = null,Object? startDate = null,Object? endDate = freezed,Object? isOpen = null,Object? note = freezed,Object? setByName = freezed,Object? createdAt = null,}) {
  return _then(_ToolRentalOut(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String,customerId: freezed == customerId ? _self.customerId : customerId // ignore: cast_nullable_to_non_nullable
as String?,renterDisplayName: null == renterDisplayName ? _self.renterDisplayName : renterDisplayName // ignore: cast_nullable_to_non_nullable
as String,renterName: null == renterName ? _self.renterName : renterName // ignore: cast_nullable_to_non_nullable
as String,dailyRate: null == dailyRate ? _self.dailyRate : dailyRate // ignore: cast_nullable_to_non_nullable
as double,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,isOpen: null == isOpen ? _self.isOpen : isOpen // ignore: cast_nullable_to_non_nullable
as bool,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,setByName: freezed == setByName ? _self.setByName : setByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
