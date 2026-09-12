// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'tool_expense.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$ToolExpense {

 String get id; String get toolId; String get toolName; String get kind; double get amount;/// `YYYY-MM-DD`.
 String get occurredOn; String? get supplier; String? get note; String? get recordedByName; DateTime get createdAt;
/// Create a copy of ToolExpense
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ToolExpenseCopyWith<ToolExpense> get copyWith => _$ToolExpenseCopyWithImpl<ToolExpense>(this as ToolExpense, _$identity);

  /// Serializes this ToolExpense to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ToolExpense&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.amount, amount) || other.amount == amount)&&(identical(other.occurredOn, occurredOn) || other.occurredOn == occurredOn)&&(identical(other.supplier, supplier) || other.supplier == supplier)&&(identical(other.note, note) || other.note == note)&&(identical(other.recordedByName, recordedByName) || other.recordedByName == recordedByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,kind,amount,occurredOn,supplier,note,recordedByName,createdAt);

@override
String toString() {
  return 'ToolExpense(id: $id, toolId: $toolId, toolName: $toolName, kind: $kind, amount: $amount, occurredOn: $occurredOn, supplier: $supplier, note: $note, recordedByName: $recordedByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $ToolExpenseCopyWith<$Res>  {
  factory $ToolExpenseCopyWith(ToolExpense value, $Res Function(ToolExpense) _then) = _$ToolExpenseCopyWithImpl;
@useResult
$Res call({
 String id, String toolId, String toolName, String kind, double amount, String occurredOn, String? supplier, String? note, String? recordedByName, DateTime createdAt
});




}
/// @nodoc
class _$ToolExpenseCopyWithImpl<$Res>
    implements $ToolExpenseCopyWith<$Res> {
  _$ToolExpenseCopyWithImpl(this._self, this._then);

  final ToolExpense _self;
  final $Res Function(ToolExpense) _then;

/// Create a copy of ToolExpense
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? kind = null,Object? amount = null,Object? occurredOn = null,Object? supplier = freezed,Object? note = freezed,Object? recordedByName = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,occurredOn: null == occurredOn ? _self.occurredOn : occurredOn // ignore: cast_nullable_to_non_nullable
as String,supplier: freezed == supplier ? _self.supplier : supplier // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,recordedByName: freezed == recordedByName ? _self.recordedByName : recordedByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [ToolExpense].
extension ToolExpensePatterns on ToolExpense {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ToolExpense value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ToolExpense() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ToolExpense value)  $default,){
final _that = this;
switch (_that) {
case _ToolExpense():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ToolExpense value)?  $default,){
final _that = this;
switch (_that) {
case _ToolExpense() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  String kind,  double amount,  String occurredOn,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ToolExpense() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.kind,_that.amount,_that.occurredOn,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String toolId,  String toolName,  String kind,  double amount,  String occurredOn,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _ToolExpense():
return $default(_that.id,_that.toolId,_that.toolName,_that.kind,_that.amount,_that.occurredOn,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String toolId,  String toolName,  String kind,  double amount,  String occurredOn,  String? supplier,  String? note,  String? recordedByName,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _ToolExpense() when $default != null:
return $default(_that.id,_that.toolId,_that.toolName,_that.kind,_that.amount,_that.occurredOn,_that.supplier,_that.note,_that.recordedByName,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ToolExpense extends ToolExpense {
  const _ToolExpense({required this.id, required this.toolId, required this.toolName, required this.kind, required this.amount, required this.occurredOn, this.supplier, this.note, this.recordedByName, required this.createdAt}): super._();
  factory _ToolExpense.fromJson(Map<String, dynamic> json) => _$ToolExpenseFromJson(json);

@override final  String id;
@override final  String toolId;
@override final  String toolName;
@override final  String kind;
@override final  double amount;
/// `YYYY-MM-DD`.
@override final  String occurredOn;
@override final  String? supplier;
@override final  String? note;
@override final  String? recordedByName;
@override final  DateTime createdAt;

/// Create a copy of ToolExpense
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ToolExpenseCopyWith<_ToolExpense> get copyWith => __$ToolExpenseCopyWithImpl<_ToolExpense>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ToolExpenseToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ToolExpense&&(identical(other.id, id) || other.id == id)&&(identical(other.toolId, toolId) || other.toolId == toolId)&&(identical(other.toolName, toolName) || other.toolName == toolName)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.amount, amount) || other.amount == amount)&&(identical(other.occurredOn, occurredOn) || other.occurredOn == occurredOn)&&(identical(other.supplier, supplier) || other.supplier == supplier)&&(identical(other.note, note) || other.note == note)&&(identical(other.recordedByName, recordedByName) || other.recordedByName == recordedByName)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,toolId,toolName,kind,amount,occurredOn,supplier,note,recordedByName,createdAt);

@override
String toString() {
  return 'ToolExpense(id: $id, toolId: $toolId, toolName: $toolName, kind: $kind, amount: $amount, occurredOn: $occurredOn, supplier: $supplier, note: $note, recordedByName: $recordedByName, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$ToolExpenseCopyWith<$Res> implements $ToolExpenseCopyWith<$Res> {
  factory _$ToolExpenseCopyWith(_ToolExpense value, $Res Function(_ToolExpense) _then) = __$ToolExpenseCopyWithImpl;
@override @useResult
$Res call({
 String id, String toolId, String toolName, String kind, double amount, String occurredOn, String? supplier, String? note, String? recordedByName, DateTime createdAt
});




}
/// @nodoc
class __$ToolExpenseCopyWithImpl<$Res>
    implements _$ToolExpenseCopyWith<$Res> {
  __$ToolExpenseCopyWithImpl(this._self, this._then);

  final _ToolExpense _self;
  final $Res Function(_ToolExpense) _then;

/// Create a copy of ToolExpense
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? toolId = null,Object? toolName = null,Object? kind = null,Object? amount = null,Object? occurredOn = null,Object? supplier = freezed,Object? note = freezed,Object? recordedByName = freezed,Object? createdAt = null,}) {
  return _then(_ToolExpense(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,toolId: null == toolId ? _self.toolId : toolId // ignore: cast_nullable_to_non_nullable
as String,toolName: null == toolName ? _self.toolName : toolName // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,occurredOn: null == occurredOn ? _self.occurredOn : occurredOn // ignore: cast_nullable_to_non_nullable
as String,supplier: freezed == supplier ? _self.supplier : supplier // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,recordedByName: freezed == recordedByName ? _self.recordedByName : recordedByName // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
