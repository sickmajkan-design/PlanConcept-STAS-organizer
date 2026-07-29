// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'employee.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Employee {

 String get id; String get employeeNumber; String get firstName; String get lastName; String get fullName; String? get phone; String? get email; String? get address; DateTime? get dateOfBirth; DateTime get employmentDate; String get position; String get status; String? get photoUrl; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of Employee
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$EmployeeCopyWith<Employee> get copyWith => _$EmployeeCopyWithImpl<Employee>(this as Employee, _$identity);

  /// Serializes this Employee to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Employee&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.firstName, firstName) || other.firstName == firstName)&&(identical(other.lastName, lastName) || other.lastName == lastName)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.address, address) || other.address == address)&&(identical(other.dateOfBirth, dateOfBirth) || other.dateOfBirth == dateOfBirth)&&(identical(other.employmentDate, employmentDate) || other.employmentDate == employmentDate)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeNumber,firstName,lastName,fullName,phone,email,address,dateOfBirth,employmentDate,position,status,photoUrl,createdAt,updatedAt);

@override
String toString() {
  return 'Employee(id: $id, employeeNumber: $employeeNumber, firstName: $firstName, lastName: $lastName, fullName: $fullName, phone: $phone, email: $email, address: $address, dateOfBirth: $dateOfBirth, employmentDate: $employmentDate, position: $position, status: $status, photoUrl: $photoUrl, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $EmployeeCopyWith<$Res>  {
  factory $EmployeeCopyWith(Employee value, $Res Function(Employee) _then) = _$EmployeeCopyWithImpl;
@useResult
$Res call({
 String id, String employeeNumber, String firstName, String lastName, String fullName, String? phone, String? email, String? address, DateTime? dateOfBirth, DateTime employmentDate, String position, String status, String? photoUrl, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$EmployeeCopyWithImpl<$Res>
    implements $EmployeeCopyWith<$Res> {
  _$EmployeeCopyWithImpl(this._self, this._then);

  final Employee _self;
  final $Res Function(Employee) _then;

/// Create a copy of Employee
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? employeeNumber = null,Object? firstName = null,Object? lastName = null,Object? fullName = null,Object? phone = freezed,Object? email = freezed,Object? address = freezed,Object? dateOfBirth = freezed,Object? employmentDate = null,Object? position = null,Object? status = null,Object? photoUrl = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,firstName: null == firstName ? _self.firstName : firstName // ignore: cast_nullable_to_non_nullable
as String,lastName: null == lastName ? _self.lastName : lastName // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as DateTime?,employmentDate: null == employmentDate ? _self.employmentDate : employmentDate // ignore: cast_nullable_to_non_nullable
as DateTime,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Employee].
extension EmployeePatterns on Employee {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Employee value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Employee() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Employee value)  $default,){
final _that = this;
switch (_that) {
case _Employee():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Employee value)?  $default,){
final _that = this;
switch (_that) {
case _Employee() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Employee() when $default != null:
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _Employee():
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _Employee() when $default != null:
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Employee extends Employee {
  const _Employee({required this.id, required this.employeeNumber, required this.firstName, required this.lastName, required this.fullName, this.phone, this.email, this.address, this.dateOfBirth, required this.employmentDate, required this.position, required this.status, this.photoUrl, required this.createdAt, this.updatedAt}): super._();
  factory _Employee.fromJson(Map<String, dynamic> json) => _$EmployeeFromJson(json);

@override final  String id;
@override final  String employeeNumber;
@override final  String firstName;
@override final  String lastName;
@override final  String fullName;
@override final  String? phone;
@override final  String? email;
@override final  String? address;
@override final  DateTime? dateOfBirth;
@override final  DateTime employmentDate;
@override final  String position;
@override final  String status;
@override final  String? photoUrl;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of Employee
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$EmployeeCopyWith<_Employee> get copyWith => __$EmployeeCopyWithImpl<_Employee>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$EmployeeToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Employee&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.firstName, firstName) || other.firstName == firstName)&&(identical(other.lastName, lastName) || other.lastName == lastName)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.address, address) || other.address == address)&&(identical(other.dateOfBirth, dateOfBirth) || other.dateOfBirth == dateOfBirth)&&(identical(other.employmentDate, employmentDate) || other.employmentDate == employmentDate)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeNumber,firstName,lastName,fullName,phone,email,address,dateOfBirth,employmentDate,position,status,photoUrl,createdAt,updatedAt);

@override
String toString() {
  return 'Employee(id: $id, employeeNumber: $employeeNumber, firstName: $firstName, lastName: $lastName, fullName: $fullName, phone: $phone, email: $email, address: $address, dateOfBirth: $dateOfBirth, employmentDate: $employmentDate, position: $position, status: $status, photoUrl: $photoUrl, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$EmployeeCopyWith<$Res> implements $EmployeeCopyWith<$Res> {
  factory _$EmployeeCopyWith(_Employee value, $Res Function(_Employee) _then) = __$EmployeeCopyWithImpl;
@override @useResult
$Res call({
 String id, String employeeNumber, String firstName, String lastName, String fullName, String? phone, String? email, String? address, DateTime? dateOfBirth, DateTime employmentDate, String position, String status, String? photoUrl, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$EmployeeCopyWithImpl<$Res>
    implements _$EmployeeCopyWith<$Res> {
  __$EmployeeCopyWithImpl(this._self, this._then);

  final _Employee _self;
  final $Res Function(_Employee) _then;

/// Create a copy of Employee
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? employeeNumber = null,Object? firstName = null,Object? lastName = null,Object? fullName = null,Object? phone = freezed,Object? email = freezed,Object? address = freezed,Object? dateOfBirth = freezed,Object? employmentDate = null,Object? position = null,Object? status = null,Object? photoUrl = freezed,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_Employee(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,firstName: null == firstName ? _self.firstName : firstName // ignore: cast_nullable_to_non_nullable
as String,lastName: null == lastName ? _self.lastName : lastName // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as DateTime?,employmentDate: null == employmentDate ? _self.employmentDate : employmentDate // ignore: cast_nullable_to_non_nullable
as DateTime,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$EmployeeDetail {

 String get id; String get employeeNumber; String get firstName; String get lastName; String get fullName; String? get phone; String? get email; String? get address; DateTime? get dateOfBirth; DateTime get employmentDate; String get position; String get status; String? get photoUrl; DateTime get createdAt; DateTime? get updatedAt; bool get hasUserAccount; List<EmployeeProjectAssignment> get projects;
/// Create a copy of EmployeeDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$EmployeeDetailCopyWith<EmployeeDetail> get copyWith => _$EmployeeDetailCopyWithImpl<EmployeeDetail>(this as EmployeeDetail, _$identity);

  /// Serializes this EmployeeDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is EmployeeDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.firstName, firstName) || other.firstName == firstName)&&(identical(other.lastName, lastName) || other.lastName == lastName)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.address, address) || other.address == address)&&(identical(other.dateOfBirth, dateOfBirth) || other.dateOfBirth == dateOfBirth)&&(identical(other.employmentDate, employmentDate) || other.employmentDate == employmentDate)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&(identical(other.hasUserAccount, hasUserAccount) || other.hasUserAccount == hasUserAccount)&&const DeepCollectionEquality().equals(other.projects, projects));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeNumber,firstName,lastName,fullName,phone,email,address,dateOfBirth,employmentDate,position,status,photoUrl,createdAt,updatedAt,hasUserAccount,const DeepCollectionEquality().hash(projects));

@override
String toString() {
  return 'EmployeeDetail(id: $id, employeeNumber: $employeeNumber, firstName: $firstName, lastName: $lastName, fullName: $fullName, phone: $phone, email: $email, address: $address, dateOfBirth: $dateOfBirth, employmentDate: $employmentDate, position: $position, status: $status, photoUrl: $photoUrl, createdAt: $createdAt, updatedAt: $updatedAt, hasUserAccount: $hasUserAccount, projects: $projects)';
}


}

/// @nodoc
abstract mixin class $EmployeeDetailCopyWith<$Res>  {
  factory $EmployeeDetailCopyWith(EmployeeDetail value, $Res Function(EmployeeDetail) _then) = _$EmployeeDetailCopyWithImpl;
@useResult
$Res call({
 String id, String employeeNumber, String firstName, String lastName, String fullName, String? phone, String? email, String? address, DateTime? dateOfBirth, DateTime employmentDate, String position, String status, String? photoUrl, DateTime createdAt, DateTime? updatedAt, bool hasUserAccount, List<EmployeeProjectAssignment> projects
});




}
/// @nodoc
class _$EmployeeDetailCopyWithImpl<$Res>
    implements $EmployeeDetailCopyWith<$Res> {
  _$EmployeeDetailCopyWithImpl(this._self, this._then);

  final EmployeeDetail _self;
  final $Res Function(EmployeeDetail) _then;

/// Create a copy of EmployeeDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? employeeNumber = null,Object? firstName = null,Object? lastName = null,Object? fullName = null,Object? phone = freezed,Object? email = freezed,Object? address = freezed,Object? dateOfBirth = freezed,Object? employmentDate = null,Object? position = null,Object? status = null,Object? photoUrl = freezed,Object? createdAt = null,Object? updatedAt = freezed,Object? hasUserAccount = null,Object? projects = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,firstName: null == firstName ? _self.firstName : firstName // ignore: cast_nullable_to_non_nullable
as String,lastName: null == lastName ? _self.lastName : lastName // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as DateTime?,employmentDate: null == employmentDate ? _self.employmentDate : employmentDate // ignore: cast_nullable_to_non_nullable
as DateTime,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,hasUserAccount: null == hasUserAccount ? _self.hasUserAccount : hasUserAccount // ignore: cast_nullable_to_non_nullable
as bool,projects: null == projects ? _self.projects : projects // ignore: cast_nullable_to_non_nullable
as List<EmployeeProjectAssignment>,
  ));
}

}


/// Adds pattern-matching-related methods to [EmployeeDetail].
extension EmployeeDetailPatterns on EmployeeDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _EmployeeDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _EmployeeDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _EmployeeDetail value)  $default,){
final _that = this;
switch (_that) {
case _EmployeeDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _EmployeeDetail value)?  $default,){
final _that = this;
switch (_that) {
case _EmployeeDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt,  bool hasUserAccount,  List<EmployeeProjectAssignment> projects)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _EmployeeDetail() when $default != null:
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt,_that.hasUserAccount,_that.projects);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt,  bool hasUserAccount,  List<EmployeeProjectAssignment> projects)  $default,) {final _that = this;
switch (_that) {
case _EmployeeDetail():
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt,_that.hasUserAccount,_that.projects);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String employeeNumber,  String firstName,  String lastName,  String fullName,  String? phone,  String? email,  String? address,  DateTime? dateOfBirth,  DateTime employmentDate,  String position,  String status,  String? photoUrl,  DateTime createdAt,  DateTime? updatedAt,  bool hasUserAccount,  List<EmployeeProjectAssignment> projects)?  $default,) {final _that = this;
switch (_that) {
case _EmployeeDetail() when $default != null:
return $default(_that.id,_that.employeeNumber,_that.firstName,_that.lastName,_that.fullName,_that.phone,_that.email,_that.address,_that.dateOfBirth,_that.employmentDate,_that.position,_that.status,_that.photoUrl,_that.createdAt,_that.updatedAt,_that.hasUserAccount,_that.projects);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _EmployeeDetail extends EmployeeDetail {
  const _EmployeeDetail({required this.id, required this.employeeNumber, required this.firstName, required this.lastName, required this.fullName, this.phone, this.email, this.address, this.dateOfBirth, required this.employmentDate, required this.position, required this.status, this.photoUrl, required this.createdAt, this.updatedAt, this.hasUserAccount = false, final  List<EmployeeProjectAssignment> projects = const <EmployeeProjectAssignment>[]}): _projects = projects,super._();
  factory _EmployeeDetail.fromJson(Map<String, dynamic> json) => _$EmployeeDetailFromJson(json);

@override final  String id;
@override final  String employeeNumber;
@override final  String firstName;
@override final  String lastName;
@override final  String fullName;
@override final  String? phone;
@override final  String? email;
@override final  String? address;
@override final  DateTime? dateOfBirth;
@override final  DateTime employmentDate;
@override final  String position;
@override final  String status;
@override final  String? photoUrl;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;
@override@JsonKey() final  bool hasUserAccount;
 final  List<EmployeeProjectAssignment> _projects;
@override@JsonKey() List<EmployeeProjectAssignment> get projects {
  if (_projects is EqualUnmodifiableListView) return _projects;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_projects);
}


/// Create a copy of EmployeeDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$EmployeeDetailCopyWith<_EmployeeDetail> get copyWith => __$EmployeeDetailCopyWithImpl<_EmployeeDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$EmployeeDetailToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _EmployeeDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.firstName, firstName) || other.firstName == firstName)&&(identical(other.lastName, lastName) || other.lastName == lastName)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.email, email) || other.email == email)&&(identical(other.address, address) || other.address == address)&&(identical(other.dateOfBirth, dateOfBirth) || other.dateOfBirth == dateOfBirth)&&(identical(other.employmentDate, employmentDate) || other.employmentDate == employmentDate)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&(identical(other.hasUserAccount, hasUserAccount) || other.hasUserAccount == hasUserAccount)&&const DeepCollectionEquality().equals(other._projects, _projects));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,employeeNumber,firstName,lastName,fullName,phone,email,address,dateOfBirth,employmentDate,position,status,photoUrl,createdAt,updatedAt,hasUserAccount,const DeepCollectionEquality().hash(_projects));

@override
String toString() {
  return 'EmployeeDetail(id: $id, employeeNumber: $employeeNumber, firstName: $firstName, lastName: $lastName, fullName: $fullName, phone: $phone, email: $email, address: $address, dateOfBirth: $dateOfBirth, employmentDate: $employmentDate, position: $position, status: $status, photoUrl: $photoUrl, createdAt: $createdAt, updatedAt: $updatedAt, hasUserAccount: $hasUserAccount, projects: $projects)';
}


}

/// @nodoc
abstract mixin class _$EmployeeDetailCopyWith<$Res> implements $EmployeeDetailCopyWith<$Res> {
  factory _$EmployeeDetailCopyWith(_EmployeeDetail value, $Res Function(_EmployeeDetail) _then) = __$EmployeeDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String employeeNumber, String firstName, String lastName, String fullName, String? phone, String? email, String? address, DateTime? dateOfBirth, DateTime employmentDate, String position, String status, String? photoUrl, DateTime createdAt, DateTime? updatedAt, bool hasUserAccount, List<EmployeeProjectAssignment> projects
});




}
/// @nodoc
class __$EmployeeDetailCopyWithImpl<$Res>
    implements _$EmployeeDetailCopyWith<$Res> {
  __$EmployeeDetailCopyWithImpl(this._self, this._then);

  final _EmployeeDetail _self;
  final $Res Function(_EmployeeDetail) _then;

/// Create a copy of EmployeeDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? employeeNumber = null,Object? firstName = null,Object? lastName = null,Object? fullName = null,Object? phone = freezed,Object? email = freezed,Object? address = freezed,Object? dateOfBirth = freezed,Object? employmentDate = null,Object? position = null,Object? status = null,Object? photoUrl = freezed,Object? createdAt = null,Object? updatedAt = freezed,Object? hasUserAccount = null,Object? projects = null,}) {
  return _then(_EmployeeDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,firstName: null == firstName ? _self.firstName : firstName // ignore: cast_nullable_to_non_nullable
as String,lastName: null == lastName ? _self.lastName : lastName // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as DateTime?,employmentDate: null == employmentDate ? _self.employmentDate : employmentDate // ignore: cast_nullable_to_non_nullable
as DateTime,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,hasUserAccount: null == hasUserAccount ? _self.hasUserAccount : hasUserAccount // ignore: cast_nullable_to_non_nullable
as bool,projects: null == projects ? _self._projects : projects // ignore: cast_nullable_to_non_nullable
as List<EmployeeProjectAssignment>,
  ));
}


}


/// @nodoc
mixin _$EmployeeProjectAssignment {

 String get projectId; String get projectName; String get projectStatus; DateTime get assignedAt;
/// Create a copy of EmployeeProjectAssignment
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$EmployeeProjectAssignmentCopyWith<EmployeeProjectAssignment> get copyWith => _$EmployeeProjectAssignmentCopyWithImpl<EmployeeProjectAssignment>(this as EmployeeProjectAssignment, _$identity);

  /// Serializes this EmployeeProjectAssignment to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is EmployeeProjectAssignment&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.projectStatus, projectStatus) || other.projectStatus == projectStatus)&&(identical(other.assignedAt, assignedAt) || other.assignedAt == assignedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,projectId,projectName,projectStatus,assignedAt);

@override
String toString() {
  return 'EmployeeProjectAssignment(projectId: $projectId, projectName: $projectName, projectStatus: $projectStatus, assignedAt: $assignedAt)';
}


}

/// @nodoc
abstract mixin class $EmployeeProjectAssignmentCopyWith<$Res>  {
  factory $EmployeeProjectAssignmentCopyWith(EmployeeProjectAssignment value, $Res Function(EmployeeProjectAssignment) _then) = _$EmployeeProjectAssignmentCopyWithImpl;
@useResult
$Res call({
 String projectId, String projectName, String projectStatus, DateTime assignedAt
});




}
/// @nodoc
class _$EmployeeProjectAssignmentCopyWithImpl<$Res>
    implements $EmployeeProjectAssignmentCopyWith<$Res> {
  _$EmployeeProjectAssignmentCopyWithImpl(this._self, this._then);

  final EmployeeProjectAssignment _self;
  final $Res Function(EmployeeProjectAssignment) _then;

/// Create a copy of EmployeeProjectAssignment
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? projectId = null,Object? projectName = null,Object? projectStatus = null,Object? assignedAt = null,}) {
  return _then(_self.copyWith(
projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,projectStatus: null == projectStatus ? _self.projectStatus : projectStatus // ignore: cast_nullable_to_non_nullable
as String,assignedAt: null == assignedAt ? _self.assignedAt : assignedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [EmployeeProjectAssignment].
extension EmployeeProjectAssignmentPatterns on EmployeeProjectAssignment {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _EmployeeProjectAssignment value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _EmployeeProjectAssignment() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _EmployeeProjectAssignment value)  $default,){
final _that = this;
switch (_that) {
case _EmployeeProjectAssignment():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _EmployeeProjectAssignment value)?  $default,){
final _that = this;
switch (_that) {
case _EmployeeProjectAssignment() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String projectId,  String projectName,  String projectStatus,  DateTime assignedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _EmployeeProjectAssignment() when $default != null:
return $default(_that.projectId,_that.projectName,_that.projectStatus,_that.assignedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String projectId,  String projectName,  String projectStatus,  DateTime assignedAt)  $default,) {final _that = this;
switch (_that) {
case _EmployeeProjectAssignment():
return $default(_that.projectId,_that.projectName,_that.projectStatus,_that.assignedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String projectId,  String projectName,  String projectStatus,  DateTime assignedAt)?  $default,) {final _that = this;
switch (_that) {
case _EmployeeProjectAssignment() when $default != null:
return $default(_that.projectId,_that.projectName,_that.projectStatus,_that.assignedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _EmployeeProjectAssignment implements EmployeeProjectAssignment {
  const _EmployeeProjectAssignment({required this.projectId, required this.projectName, required this.projectStatus, required this.assignedAt});
  factory _EmployeeProjectAssignment.fromJson(Map<String, dynamic> json) => _$EmployeeProjectAssignmentFromJson(json);

@override final  String projectId;
@override final  String projectName;
@override final  String projectStatus;
@override final  DateTime assignedAt;

/// Create a copy of EmployeeProjectAssignment
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$EmployeeProjectAssignmentCopyWith<_EmployeeProjectAssignment> get copyWith => __$EmployeeProjectAssignmentCopyWithImpl<_EmployeeProjectAssignment>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$EmployeeProjectAssignmentToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _EmployeeProjectAssignment&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.projectStatus, projectStatus) || other.projectStatus == projectStatus)&&(identical(other.assignedAt, assignedAt) || other.assignedAt == assignedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,projectId,projectName,projectStatus,assignedAt);

@override
String toString() {
  return 'EmployeeProjectAssignment(projectId: $projectId, projectName: $projectName, projectStatus: $projectStatus, assignedAt: $assignedAt)';
}


}

/// @nodoc
abstract mixin class _$EmployeeProjectAssignmentCopyWith<$Res> implements $EmployeeProjectAssignmentCopyWith<$Res> {
  factory _$EmployeeProjectAssignmentCopyWith(_EmployeeProjectAssignment value, $Res Function(_EmployeeProjectAssignment) _then) = __$EmployeeProjectAssignmentCopyWithImpl;
@override @useResult
$Res call({
 String projectId, String projectName, String projectStatus, DateTime assignedAt
});




}
/// @nodoc
class __$EmployeeProjectAssignmentCopyWithImpl<$Res>
    implements _$EmployeeProjectAssignmentCopyWith<$Res> {
  __$EmployeeProjectAssignmentCopyWithImpl(this._self, this._then);

  final _EmployeeProjectAssignment _self;
  final $Res Function(_EmployeeProjectAssignment) _then;

/// Create a copy of EmployeeProjectAssignment
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? projectId = null,Object? projectName = null,Object? projectStatus = null,Object? assignedAt = null,}) {
  return _then(_EmployeeProjectAssignment(
projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,projectStatus: null == projectStatus ? _self.projectStatus : projectStatus // ignore: cast_nullable_to_non_nullable
as String,assignedAt: null == assignedAt ? _self.assignedAt : assignedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
