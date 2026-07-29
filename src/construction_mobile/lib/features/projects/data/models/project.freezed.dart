// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'project.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Project {

 String get id; String get name; String? get description; String? get client; String? get address; double? get latitude; double? get longitude; DateTime? get startDate; DateTime? get endDate; String get status; int get employeeCount; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of Project
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ProjectCopyWith<Project> get copyWith => _$ProjectCopyWithImpl<Project>(this as Project, _$identity);

  /// Serializes this Project to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Project&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.client, client) || other.client == client)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.status, status) || other.status == status)&&(identical(other.employeeCount, employeeCount) || other.employeeCount == employeeCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,description,client,address,latitude,longitude,startDate,endDate,status,employeeCount,createdAt,updatedAt);

@override
String toString() {
  return 'Project(id: $id, name: $name, description: $description, client: $client, address: $address, latitude: $latitude, longitude: $longitude, startDate: $startDate, endDate: $endDate, status: $status, employeeCount: $employeeCount, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $ProjectCopyWith<$Res>  {
  factory $ProjectCopyWith(Project value, $Res Function(Project) _then) = _$ProjectCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? description, String? client, String? address, double? latitude, double? longitude, DateTime? startDate, DateTime? endDate, String status, int employeeCount, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$ProjectCopyWithImpl<$Res>
    implements $ProjectCopyWith<$Res> {
  _$ProjectCopyWithImpl(this._self, this._then);

  final Project _self;
  final $Res Function(Project) _then;

/// Create a copy of Project
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? description = freezed,Object? client = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? startDate = freezed,Object? endDate = freezed,Object? status = null,Object? employeeCount = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,client: freezed == client ? _self.client : client // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,startDate: freezed == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime?,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,employeeCount: null == employeeCount ? _self.employeeCount : employeeCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Project].
extension ProjectPatterns on Project {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Project value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Project() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Project value)  $default,){
final _that = this;
switch (_that) {
case _Project():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Project value)?  $default,){
final _that = this;
switch (_that) {
case _Project() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Project() when $default != null:
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _Project():
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _Project() when $default != null:
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Project extends Project {
  const _Project({required this.id, required this.name, this.description, this.client, this.address, this.latitude, this.longitude, this.startDate, this.endDate, required this.status, this.employeeCount = 0, required this.createdAt, this.updatedAt}): super._();
  factory _Project.fromJson(Map<String, dynamic> json) => _$ProjectFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? description;
@override final  String? client;
@override final  String? address;
@override final  double? latitude;
@override final  double? longitude;
@override final  DateTime? startDate;
@override final  DateTime? endDate;
@override final  String status;
@override@JsonKey() final  int employeeCount;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of Project
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ProjectCopyWith<_Project> get copyWith => __$ProjectCopyWithImpl<_Project>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ProjectToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Project&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.client, client) || other.client == client)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.status, status) || other.status == status)&&(identical(other.employeeCount, employeeCount) || other.employeeCount == employeeCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,description,client,address,latitude,longitude,startDate,endDate,status,employeeCount,createdAt,updatedAt);

@override
String toString() {
  return 'Project(id: $id, name: $name, description: $description, client: $client, address: $address, latitude: $latitude, longitude: $longitude, startDate: $startDate, endDate: $endDate, status: $status, employeeCount: $employeeCount, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$ProjectCopyWith<$Res> implements $ProjectCopyWith<$Res> {
  factory _$ProjectCopyWith(_Project value, $Res Function(_Project) _then) = __$ProjectCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? description, String? client, String? address, double? latitude, double? longitude, DateTime? startDate, DateTime? endDate, String status, int employeeCount, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$ProjectCopyWithImpl<$Res>
    implements _$ProjectCopyWith<$Res> {
  __$ProjectCopyWithImpl(this._self, this._then);

  final _Project _self;
  final $Res Function(_Project) _then;

/// Create a copy of Project
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? description = freezed,Object? client = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? startDate = freezed,Object? endDate = freezed,Object? status = null,Object? employeeCount = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_Project(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,client: freezed == client ? _self.client : client // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,startDate: freezed == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime?,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,employeeCount: null == employeeCount ? _self.employeeCount : employeeCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$ProjectDetail {

 String get id; String get name; String? get description; String? get client; String? get address; double? get latitude; double? get longitude; DateTime? get startDate; DateTime? get endDate; String get status; int get employeeCount; DateTime get createdAt; DateTime? get updatedAt; List<ProjectEmployee> get employees;
/// Create a copy of ProjectDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ProjectDetailCopyWith<ProjectDetail> get copyWith => _$ProjectDetailCopyWithImpl<ProjectDetail>(this as ProjectDetail, _$identity);

  /// Serializes this ProjectDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ProjectDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.client, client) || other.client == client)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.status, status) || other.status == status)&&(identical(other.employeeCount, employeeCount) || other.employeeCount == employeeCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&const DeepCollectionEquality().equals(other.employees, employees));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,description,client,address,latitude,longitude,startDate,endDate,status,employeeCount,createdAt,updatedAt,const DeepCollectionEquality().hash(employees));

@override
String toString() {
  return 'ProjectDetail(id: $id, name: $name, description: $description, client: $client, address: $address, latitude: $latitude, longitude: $longitude, startDate: $startDate, endDate: $endDate, status: $status, employeeCount: $employeeCount, createdAt: $createdAt, updatedAt: $updatedAt, employees: $employees)';
}


}

/// @nodoc
abstract mixin class $ProjectDetailCopyWith<$Res>  {
  factory $ProjectDetailCopyWith(ProjectDetail value, $Res Function(ProjectDetail) _then) = _$ProjectDetailCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? description, String? client, String? address, double? latitude, double? longitude, DateTime? startDate, DateTime? endDate, String status, int employeeCount, DateTime createdAt, DateTime? updatedAt, List<ProjectEmployee> employees
});




}
/// @nodoc
class _$ProjectDetailCopyWithImpl<$Res>
    implements $ProjectDetailCopyWith<$Res> {
  _$ProjectDetailCopyWithImpl(this._self, this._then);

  final ProjectDetail _self;
  final $Res Function(ProjectDetail) _then;

/// Create a copy of ProjectDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? description = freezed,Object? client = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? startDate = freezed,Object? endDate = freezed,Object? status = null,Object? employeeCount = null,Object? createdAt = null,Object? updatedAt = freezed,Object? employees = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,client: freezed == client ? _self.client : client // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,startDate: freezed == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime?,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,employeeCount: null == employeeCount ? _self.employeeCount : employeeCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,employees: null == employees ? _self.employees : employees // ignore: cast_nullable_to_non_nullable
as List<ProjectEmployee>,
  ));
}

}


/// Adds pattern-matching-related methods to [ProjectDetail].
extension ProjectDetailPatterns on ProjectDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ProjectDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ProjectDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ProjectDetail value)  $default,){
final _that = this;
switch (_that) {
case _ProjectDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ProjectDetail value)?  $default,){
final _that = this;
switch (_that) {
case _ProjectDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt,  List<ProjectEmployee> employees)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ProjectDetail() when $default != null:
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt,_that.employees);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt,  List<ProjectEmployee> employees)  $default,) {final _that = this;
switch (_that) {
case _ProjectDetail():
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt,_that.employees);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? description,  String? client,  String? address,  double? latitude,  double? longitude,  DateTime? startDate,  DateTime? endDate,  String status,  int employeeCount,  DateTime createdAt,  DateTime? updatedAt,  List<ProjectEmployee> employees)?  $default,) {final _that = this;
switch (_that) {
case _ProjectDetail() when $default != null:
return $default(_that.id,_that.name,_that.description,_that.client,_that.address,_that.latitude,_that.longitude,_that.startDate,_that.endDate,_that.status,_that.employeeCount,_that.createdAt,_that.updatedAt,_that.employees);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ProjectDetail extends ProjectDetail {
  const _ProjectDetail({required this.id, required this.name, this.description, this.client, this.address, this.latitude, this.longitude, this.startDate, this.endDate, required this.status, this.employeeCount = 0, required this.createdAt, this.updatedAt, final  List<ProjectEmployee> employees = const <ProjectEmployee>[]}): _employees = employees,super._();
  factory _ProjectDetail.fromJson(Map<String, dynamic> json) => _$ProjectDetailFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? description;
@override final  String? client;
@override final  String? address;
@override final  double? latitude;
@override final  double? longitude;
@override final  DateTime? startDate;
@override final  DateTime? endDate;
@override final  String status;
@override@JsonKey() final  int employeeCount;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;
 final  List<ProjectEmployee> _employees;
@override@JsonKey() List<ProjectEmployee> get employees {
  if (_employees is EqualUnmodifiableListView) return _employees;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_employees);
}


/// Create a copy of ProjectDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ProjectDetailCopyWith<_ProjectDetail> get copyWith => __$ProjectDetailCopyWithImpl<_ProjectDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ProjectDetailToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ProjectDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.client, client) || other.client == client)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.status, status) || other.status == status)&&(identical(other.employeeCount, employeeCount) || other.employeeCount == employeeCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt)&&const DeepCollectionEquality().equals(other._employees, _employees));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,description,client,address,latitude,longitude,startDate,endDate,status,employeeCount,createdAt,updatedAt,const DeepCollectionEquality().hash(_employees));

@override
String toString() {
  return 'ProjectDetail(id: $id, name: $name, description: $description, client: $client, address: $address, latitude: $latitude, longitude: $longitude, startDate: $startDate, endDate: $endDate, status: $status, employeeCount: $employeeCount, createdAt: $createdAt, updatedAt: $updatedAt, employees: $employees)';
}


}

/// @nodoc
abstract mixin class _$ProjectDetailCopyWith<$Res> implements $ProjectDetailCopyWith<$Res> {
  factory _$ProjectDetailCopyWith(_ProjectDetail value, $Res Function(_ProjectDetail) _then) = __$ProjectDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? description, String? client, String? address, double? latitude, double? longitude, DateTime? startDate, DateTime? endDate, String status, int employeeCount, DateTime createdAt, DateTime? updatedAt, List<ProjectEmployee> employees
});




}
/// @nodoc
class __$ProjectDetailCopyWithImpl<$Res>
    implements _$ProjectDetailCopyWith<$Res> {
  __$ProjectDetailCopyWithImpl(this._self, this._then);

  final _ProjectDetail _self;
  final $Res Function(_ProjectDetail) _then;

/// Create a copy of ProjectDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? description = freezed,Object? client = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? startDate = freezed,Object? endDate = freezed,Object? status = null,Object? employeeCount = null,Object? createdAt = null,Object? updatedAt = freezed,Object? employees = null,}) {
  return _then(_ProjectDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,client: freezed == client ? _self.client : client // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,startDate: freezed == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime?,endDate: freezed == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,employeeCount: null == employeeCount ? _self.employeeCount : employeeCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,employees: null == employees ? _self._employees : employees // ignore: cast_nullable_to_non_nullable
as List<ProjectEmployee>,
  ));
}


}


/// @nodoc
mixin _$ProjectEmployee {

 String get employeeId; String get employeeNumber; String get fullName; String get position; String get status; DateTime get assignedAt;
/// Create a copy of ProjectEmployee
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ProjectEmployeeCopyWith<ProjectEmployee> get copyWith => _$ProjectEmployeeCopyWithImpl<ProjectEmployee>(this as ProjectEmployee, _$identity);

  /// Serializes this ProjectEmployee to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ProjectEmployee&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedAt, assignedAt) || other.assignedAt == assignedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,employeeId,employeeNumber,fullName,position,status,assignedAt);

@override
String toString() {
  return 'ProjectEmployee(employeeId: $employeeId, employeeNumber: $employeeNumber, fullName: $fullName, position: $position, status: $status, assignedAt: $assignedAt)';
}


}

/// @nodoc
abstract mixin class $ProjectEmployeeCopyWith<$Res>  {
  factory $ProjectEmployeeCopyWith(ProjectEmployee value, $Res Function(ProjectEmployee) _then) = _$ProjectEmployeeCopyWithImpl;
@useResult
$Res call({
 String employeeId, String employeeNumber, String fullName, String position, String status, DateTime assignedAt
});




}
/// @nodoc
class _$ProjectEmployeeCopyWithImpl<$Res>
    implements $ProjectEmployeeCopyWith<$Res> {
  _$ProjectEmployeeCopyWithImpl(this._self, this._then);

  final ProjectEmployee _self;
  final $Res Function(ProjectEmployee) _then;

/// Create a copy of ProjectEmployee
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? employeeId = null,Object? employeeNumber = null,Object? fullName = null,Object? position = null,Object? status = null,Object? assignedAt = null,}) {
  return _then(_self.copyWith(
employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedAt: null == assignedAt ? _self.assignedAt : assignedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [ProjectEmployee].
extension ProjectEmployeePatterns on ProjectEmployee {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ProjectEmployee value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ProjectEmployee() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ProjectEmployee value)  $default,){
final _that = this;
switch (_that) {
case _ProjectEmployee():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ProjectEmployee value)?  $default,){
final _that = this;
switch (_that) {
case _ProjectEmployee() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String employeeId,  String employeeNumber,  String fullName,  String position,  String status,  DateTime assignedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ProjectEmployee() when $default != null:
return $default(_that.employeeId,_that.employeeNumber,_that.fullName,_that.position,_that.status,_that.assignedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String employeeId,  String employeeNumber,  String fullName,  String position,  String status,  DateTime assignedAt)  $default,) {final _that = this;
switch (_that) {
case _ProjectEmployee():
return $default(_that.employeeId,_that.employeeNumber,_that.fullName,_that.position,_that.status,_that.assignedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String employeeId,  String employeeNumber,  String fullName,  String position,  String status,  DateTime assignedAt)?  $default,) {final _that = this;
switch (_that) {
case _ProjectEmployee() when $default != null:
return $default(_that.employeeId,_that.employeeNumber,_that.fullName,_that.position,_that.status,_that.assignedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ProjectEmployee implements ProjectEmployee {
  const _ProjectEmployee({required this.employeeId, required this.employeeNumber, required this.fullName, required this.position, required this.status, required this.assignedAt});
  factory _ProjectEmployee.fromJson(Map<String, dynamic> json) => _$ProjectEmployeeFromJson(json);

@override final  String employeeId;
@override final  String employeeNumber;
@override final  String fullName;
@override final  String position;
@override final  String status;
@override final  DateTime assignedAt;

/// Create a copy of ProjectEmployee
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ProjectEmployeeCopyWith<_ProjectEmployee> get copyWith => __$ProjectEmployeeCopyWithImpl<_ProjectEmployee>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ProjectEmployeeToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ProjectEmployee&&(identical(other.employeeId, employeeId) || other.employeeId == employeeId)&&(identical(other.employeeNumber, employeeNumber) || other.employeeNumber == employeeNumber)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.position, position) || other.position == position)&&(identical(other.status, status) || other.status == status)&&(identical(other.assignedAt, assignedAt) || other.assignedAt == assignedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,employeeId,employeeNumber,fullName,position,status,assignedAt);

@override
String toString() {
  return 'ProjectEmployee(employeeId: $employeeId, employeeNumber: $employeeNumber, fullName: $fullName, position: $position, status: $status, assignedAt: $assignedAt)';
}


}

/// @nodoc
abstract mixin class _$ProjectEmployeeCopyWith<$Res> implements $ProjectEmployeeCopyWith<$Res> {
  factory _$ProjectEmployeeCopyWith(_ProjectEmployee value, $Res Function(_ProjectEmployee) _then) = __$ProjectEmployeeCopyWithImpl;
@override @useResult
$Res call({
 String employeeId, String employeeNumber, String fullName, String position, String status, DateTime assignedAt
});




}
/// @nodoc
class __$ProjectEmployeeCopyWithImpl<$Res>
    implements _$ProjectEmployeeCopyWith<$Res> {
  __$ProjectEmployeeCopyWithImpl(this._self, this._then);

  final _ProjectEmployee _self;
  final $Res Function(_ProjectEmployee) _then;

/// Create a copy of ProjectEmployee
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? employeeId = null,Object? employeeNumber = null,Object? fullName = null,Object? position = null,Object? status = null,Object? assignedAt = null,}) {
  return _then(_ProjectEmployee(
employeeId: null == employeeId ? _self.employeeId : employeeId // ignore: cast_nullable_to_non_nullable
as String,employeeNumber: null == employeeNumber ? _self.employeeNumber : employeeNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,position: null == position ? _self.position : position // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,assignedAt: null == assignedAt ? _self.assignedAt : assignedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
