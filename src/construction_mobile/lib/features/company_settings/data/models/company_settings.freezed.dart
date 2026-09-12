// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'company_settings.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$CompanySettings {

 String? get name; String? get address; String? get taxId; String? get registrationNumber; String? get vatNumber; String? get phone; String? get email; String? get weeklyReportsForwardEmail; bool get hasLogo; DateTime? get updatedAt;
/// Create a copy of CompanySettings
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CompanySettingsCopyWith<CompanySettings> get copyWith => _$CompanySettingsCopyWithImpl<CompanySettings>(this as CompanySettings, _$identity);

  /// Serializes this CompanySettings to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CompanySettings&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.taxId, taxId) || other.taxId == taxId)&&(identical(other.registrationNumber, registrationNumber) || other.registrationNumber == registrationNumber)&&(identical(other.vatNumber, vatNumber) || other.vatNumber == vatNumber)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.weeklyReportsForwardEmail, weeklyReportsForwardEmail) || other.weeklyReportsForwardEmail == weeklyReportsForwardEmail)&&(identical(other.hasLogo, hasLogo) || other.hasLogo == hasLogo)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,name,address,taxId,registrationNumber,vatNumber,phone,email,weeklyReportsForwardEmail,hasLogo,updatedAt);

@override
String toString() {
  return 'CompanySettings(name: $name, address: $address, taxId: $taxId, registrationNumber: $registrationNumber, vatNumber: $vatNumber, phone: $phone, email: $email, weeklyReportsForwardEmail: $weeklyReportsForwardEmail, hasLogo: $hasLogo, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $CompanySettingsCopyWith<$Res>  {
  factory $CompanySettingsCopyWith(CompanySettings value, $Res Function(CompanySettings) _then) = _$CompanySettingsCopyWithImpl;
@useResult
$Res call({
 String? name, String? address, String? taxId, String? registrationNumber, String? vatNumber, String? phone, String? email, String? weeklyReportsForwardEmail, bool hasLogo, DateTime? updatedAt
});




}
/// @nodoc
class _$CompanySettingsCopyWithImpl<$Res>
    implements $CompanySettingsCopyWith<$Res> {
  _$CompanySettingsCopyWithImpl(this._self, this._then);

  final CompanySettings _self;
  final $Res Function(CompanySettings) _then;

/// Create a copy of CompanySettings
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? name = freezed,Object? address = freezed,Object? taxId = freezed,Object? registrationNumber = freezed,Object? vatNumber = freezed,Object? phone = freezed,Object? email = freezed,Object? weeklyReportsForwardEmail = freezed,Object? hasLogo = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
name: freezed == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,taxId: freezed == taxId ? _self.taxId : taxId // ignore: cast_nullable_to_non_nullable
as String?,registrationNumber: freezed == registrationNumber ? _self.registrationNumber : registrationNumber // ignore: cast_nullable_to_non_nullable
as String?,vatNumber: freezed == vatNumber ? _self.vatNumber : vatNumber // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,weeklyReportsForwardEmail: freezed == weeklyReportsForwardEmail ? _self.weeklyReportsForwardEmail : weeklyReportsForwardEmail // ignore: cast_nullable_to_non_nullable
as String?,hasLogo: null == hasLogo ? _self.hasLogo : hasLogo // ignore: cast_nullable_to_non_nullable
as bool,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [CompanySettings].
extension CompanySettingsPatterns on CompanySettings {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CompanySettings value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CompanySettings() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CompanySettings value)  $default,){
final _that = this;
switch (_that) {
case _CompanySettings():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CompanySettings value)?  $default,){
final _that = this;
switch (_that) {
case _CompanySettings() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String? name,  String? address,  String? taxId,  String? registrationNumber,  String? vatNumber,  String? phone,  String? email,  String? weeklyReportsForwardEmail,  bool hasLogo,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CompanySettings() when $default != null:
return $default(_that.name,_that.address,_that.taxId,_that.registrationNumber,_that.vatNumber,_that.phone,_that.email,_that.weeklyReportsForwardEmail,_that.hasLogo,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String? name,  String? address,  String? taxId,  String? registrationNumber,  String? vatNumber,  String? phone,  String? email,  String? weeklyReportsForwardEmail,  bool hasLogo,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _CompanySettings():
return $default(_that.name,_that.address,_that.taxId,_that.registrationNumber,_that.vatNumber,_that.phone,_that.email,_that.weeklyReportsForwardEmail,_that.hasLogo,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String? name,  String? address,  String? taxId,  String? registrationNumber,  String? vatNumber,  String? phone,  String? email,  String? weeklyReportsForwardEmail,  bool hasLogo,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _CompanySettings() when $default != null:
return $default(_that.name,_that.address,_that.taxId,_that.registrationNumber,_that.vatNumber,_that.phone,_that.email,_that.weeklyReportsForwardEmail,_that.hasLogo,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CompanySettings implements CompanySettings {
  const _CompanySettings({this.name, this.address, this.taxId, this.registrationNumber, this.vatNumber, this.phone, this.email, this.weeklyReportsForwardEmail, this.hasLogo = false, this.updatedAt});
  factory _CompanySettings.fromJson(Map<String, dynamic> json) => _$CompanySettingsFromJson(json);

@override final  String? name;
@override final  String? address;
@override final  String? taxId;
@override final  String? registrationNumber;
@override final  String? vatNumber;
@override final  String? phone;
@override final  String? email;
@override final  String? weeklyReportsForwardEmail;
@override@JsonKey() final  bool hasLogo;
@override final  DateTime? updatedAt;

/// Create a copy of CompanySettings
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CompanySettingsCopyWith<_CompanySettings> get copyWith => __$CompanySettingsCopyWithImpl<_CompanySettings>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CompanySettingsToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _CompanySettings&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.taxId, taxId) || other.taxId == taxId)&&(identical(other.registrationNumber, registrationNumber) || other.registrationNumber == registrationNumber)&&(identical(other.vatNumber, vatNumber) || other.vatNumber == vatNumber)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.weeklyReportsForwardEmail, weeklyReportsForwardEmail) || other.weeklyReportsForwardEmail == weeklyReportsForwardEmail)&&(identical(other.hasLogo, hasLogo) || other.hasLogo == hasLogo)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,name,address,taxId,registrationNumber,vatNumber,phone,email,weeklyReportsForwardEmail,hasLogo,updatedAt);

@override
String toString() {
  return 'CompanySettings(name: $name, address: $address, taxId: $taxId, registrationNumber: $registrationNumber, vatNumber: $vatNumber, phone: $phone, email: $email, weeklyReportsForwardEmail: $weeklyReportsForwardEmail, hasLogo: $hasLogo, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$CompanySettingsCopyWith<$Res> implements $CompanySettingsCopyWith<$Res> {
  factory _$CompanySettingsCopyWith(_CompanySettings value, $Res Function(_CompanySettings) _then) = __$CompanySettingsCopyWithImpl;
@override @useResult
$Res call({
 String? name, String? address, String? taxId, String? registrationNumber, String? vatNumber, String? phone, String? email, String? weeklyReportsForwardEmail, bool hasLogo, DateTime? updatedAt
});




}
/// @nodoc
class __$CompanySettingsCopyWithImpl<$Res>
    implements _$CompanySettingsCopyWith<$Res> {
  __$CompanySettingsCopyWithImpl(this._self, this._then);

  final _CompanySettings _self;
  final $Res Function(_CompanySettings) _then;

/// Create a copy of CompanySettings
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? name = freezed,Object? address = freezed,Object? taxId = freezed,Object? registrationNumber = freezed,Object? vatNumber = freezed,Object? phone = freezed,Object? email = freezed,Object? weeklyReportsForwardEmail = freezed,Object? hasLogo = null,Object? updatedAt = freezed,}) {
  return _then(_CompanySettings(
name: freezed == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,taxId: freezed == taxId ? _self.taxId : taxId // ignore: cast_nullable_to_non_nullable
as String?,registrationNumber: freezed == registrationNumber ? _self.registrationNumber : registrationNumber // ignore: cast_nullable_to_non_nullable
as String?,vatNumber: freezed == vatNumber ? _self.vatNumber : vatNumber // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,weeklyReportsForwardEmail: freezed == weeklyReportsForwardEmail ? _self.weeklyReportsForwardEmail : weeklyReportsForwardEmail // ignore: cast_nullable_to_non_nullable
as String?,hasLogo: null == hasLogo ? _self.hasLogo : hasLogo // ignore: cast_nullable_to_non_nullable
as bool,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
