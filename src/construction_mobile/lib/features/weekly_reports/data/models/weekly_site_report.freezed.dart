// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'weekly_site_report.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$WeeklySiteReport {

 String get id; String get projectId; String get projectName; String get submittedByEmployeeId; String get submittedByEmployeeName; int get isoYear; int get isoWeek; String get type; double? get quantity; String? get note; String get fileName; String get status; DateTime? get processedAt; String? get processedByEmail; DateTime get createdAt;
/// Create a copy of WeeklySiteReport
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$WeeklySiteReportCopyWith<WeeklySiteReport> get copyWith => _$WeeklySiteReportCopyWithImpl<WeeklySiteReport>(this as WeeklySiteReport, _$identity);

  /// Serializes this WeeklySiteReport to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is WeeklySiteReport&&(identical(other.id, id) || other.id == id)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.submittedByEmployeeId, submittedByEmployeeId) || other.submittedByEmployeeId == submittedByEmployeeId)&&(identical(other.submittedByEmployeeName, submittedByEmployeeName) || other.submittedByEmployeeName == submittedByEmployeeName)&&(identical(other.isoYear, isoYear) || other.isoYear == isoYear)&&(identical(other.isoWeek, isoWeek) || other.isoWeek == isoWeek)&&(identical(other.type, type) || other.type == type)&&(identical(other.quantity, quantity) || other.quantity == quantity)&&(identical(other.note, note) || other.note == note)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.status, status) || other.status == status)&&(identical(other.processedAt, processedAt) || other.processedAt == processedAt)&&(identical(other.processedByEmail, processedByEmail) || other.processedByEmail == processedByEmail)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,projectId,projectName,submittedByEmployeeId,submittedByEmployeeName,isoYear,isoWeek,type,quantity,note,fileName,status,processedAt,processedByEmail,createdAt);

@override
String toString() {
  return 'WeeklySiteReport(id: $id, projectId: $projectId, projectName: $projectName, submittedByEmployeeId: $submittedByEmployeeId, submittedByEmployeeName: $submittedByEmployeeName, isoYear: $isoYear, isoWeek: $isoWeek, type: $type, quantity: $quantity, note: $note, fileName: $fileName, status: $status, processedAt: $processedAt, processedByEmail: $processedByEmail, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $WeeklySiteReportCopyWith<$Res>  {
  factory $WeeklySiteReportCopyWith(WeeklySiteReport value, $Res Function(WeeklySiteReport) _then) = _$WeeklySiteReportCopyWithImpl;
@useResult
$Res call({
 String id, String projectId, String projectName, String submittedByEmployeeId, String submittedByEmployeeName, int isoYear, int isoWeek, String type, double? quantity, String? note, String fileName, String status, DateTime? processedAt, String? processedByEmail, DateTime createdAt
});




}
/// @nodoc
class _$WeeklySiteReportCopyWithImpl<$Res>
    implements $WeeklySiteReportCopyWith<$Res> {
  _$WeeklySiteReportCopyWithImpl(this._self, this._then);

  final WeeklySiteReport _self;
  final $Res Function(WeeklySiteReport) _then;

/// Create a copy of WeeklySiteReport
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? projectId = null,Object? projectName = null,Object? submittedByEmployeeId = null,Object? submittedByEmployeeName = null,Object? isoYear = null,Object? isoWeek = null,Object? type = null,Object? quantity = freezed,Object? note = freezed,Object? fileName = null,Object? status = null,Object? processedAt = freezed,Object? processedByEmail = freezed,Object? createdAt = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,submittedByEmployeeId: null == submittedByEmployeeId ? _self.submittedByEmployeeId : submittedByEmployeeId // ignore: cast_nullable_to_non_nullable
as String,submittedByEmployeeName: null == submittedByEmployeeName ? _self.submittedByEmployeeName : submittedByEmployeeName // ignore: cast_nullable_to_non_nullable
as String,isoYear: null == isoYear ? _self.isoYear : isoYear // ignore: cast_nullable_to_non_nullable
as int,isoWeek: null == isoWeek ? _self.isoWeek : isoWeek // ignore: cast_nullable_to_non_nullable
as int,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,quantity: freezed == quantity ? _self.quantity : quantity // ignore: cast_nullable_to_non_nullable
as double?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,processedAt: freezed == processedAt ? _self.processedAt : processedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,processedByEmail: freezed == processedByEmail ? _self.processedByEmail : processedByEmail // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [WeeklySiteReport].
extension WeeklySiteReportPatterns on WeeklySiteReport {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _WeeklySiteReport value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _WeeklySiteReport() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _WeeklySiteReport value)  $default,){
final _that = this;
switch (_that) {
case _WeeklySiteReport():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _WeeklySiteReport value)?  $default,){
final _that = this;
switch (_that) {
case _WeeklySiteReport() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String projectId,  String projectName,  String submittedByEmployeeId,  String submittedByEmployeeName,  int isoYear,  int isoWeek,  String type,  double? quantity,  String? note,  String fileName,  String status,  DateTime? processedAt,  String? processedByEmail,  DateTime createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _WeeklySiteReport() when $default != null:
return $default(_that.id,_that.projectId,_that.projectName,_that.submittedByEmployeeId,_that.submittedByEmployeeName,_that.isoYear,_that.isoWeek,_that.type,_that.quantity,_that.note,_that.fileName,_that.status,_that.processedAt,_that.processedByEmail,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String projectId,  String projectName,  String submittedByEmployeeId,  String submittedByEmployeeName,  int isoYear,  int isoWeek,  String type,  double? quantity,  String? note,  String fileName,  String status,  DateTime? processedAt,  String? processedByEmail,  DateTime createdAt)  $default,) {final _that = this;
switch (_that) {
case _WeeklySiteReport():
return $default(_that.id,_that.projectId,_that.projectName,_that.submittedByEmployeeId,_that.submittedByEmployeeName,_that.isoYear,_that.isoWeek,_that.type,_that.quantity,_that.note,_that.fileName,_that.status,_that.processedAt,_that.processedByEmail,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String projectId,  String projectName,  String submittedByEmployeeId,  String submittedByEmployeeName,  int isoYear,  int isoWeek,  String type,  double? quantity,  String? note,  String fileName,  String status,  DateTime? processedAt,  String? processedByEmail,  DateTime createdAt)?  $default,) {final _that = this;
switch (_that) {
case _WeeklySiteReport() when $default != null:
return $default(_that.id,_that.projectId,_that.projectName,_that.submittedByEmployeeId,_that.submittedByEmployeeName,_that.isoYear,_that.isoWeek,_that.type,_that.quantity,_that.note,_that.fileName,_that.status,_that.processedAt,_that.processedByEmail,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _WeeklySiteReport implements WeeklySiteReport {
  const _WeeklySiteReport({required this.id, required this.projectId, required this.projectName, required this.submittedByEmployeeId, required this.submittedByEmployeeName, required this.isoYear, required this.isoWeek, required this.type, this.quantity, this.note, required this.fileName, required this.status, this.processedAt, this.processedByEmail, required this.createdAt});
  factory _WeeklySiteReport.fromJson(Map<String, dynamic> json) => _$WeeklySiteReportFromJson(json);

@override final  String id;
@override final  String projectId;
@override final  String projectName;
@override final  String submittedByEmployeeId;
@override final  String submittedByEmployeeName;
@override final  int isoYear;
@override final  int isoWeek;
@override final  String type;
@override final  double? quantity;
@override final  String? note;
@override final  String fileName;
@override final  String status;
@override final  DateTime? processedAt;
@override final  String? processedByEmail;
@override final  DateTime createdAt;

/// Create a copy of WeeklySiteReport
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$WeeklySiteReportCopyWith<_WeeklySiteReport> get copyWith => __$WeeklySiteReportCopyWithImpl<_WeeklySiteReport>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$WeeklySiteReportToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _WeeklySiteReport&&(identical(other.id, id) || other.id == id)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.submittedByEmployeeId, submittedByEmployeeId) || other.submittedByEmployeeId == submittedByEmployeeId)&&(identical(other.submittedByEmployeeName, submittedByEmployeeName) || other.submittedByEmployeeName == submittedByEmployeeName)&&(identical(other.isoYear, isoYear) || other.isoYear == isoYear)&&(identical(other.isoWeek, isoWeek) || other.isoWeek == isoWeek)&&(identical(other.type, type) || other.type == type)&&(identical(other.quantity, quantity) || other.quantity == quantity)&&(identical(other.note, note) || other.note == note)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.status, status) || other.status == status)&&(identical(other.processedAt, processedAt) || other.processedAt == processedAt)&&(identical(other.processedByEmail, processedByEmail) || other.processedByEmail == processedByEmail)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,projectId,projectName,submittedByEmployeeId,submittedByEmployeeName,isoYear,isoWeek,type,quantity,note,fileName,status,processedAt,processedByEmail,createdAt);

@override
String toString() {
  return 'WeeklySiteReport(id: $id, projectId: $projectId, projectName: $projectName, submittedByEmployeeId: $submittedByEmployeeId, submittedByEmployeeName: $submittedByEmployeeName, isoYear: $isoYear, isoWeek: $isoWeek, type: $type, quantity: $quantity, note: $note, fileName: $fileName, status: $status, processedAt: $processedAt, processedByEmail: $processedByEmail, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$WeeklySiteReportCopyWith<$Res> implements $WeeklySiteReportCopyWith<$Res> {
  factory _$WeeklySiteReportCopyWith(_WeeklySiteReport value, $Res Function(_WeeklySiteReport) _then) = __$WeeklySiteReportCopyWithImpl;
@override @useResult
$Res call({
 String id, String projectId, String projectName, String submittedByEmployeeId, String submittedByEmployeeName, int isoYear, int isoWeek, String type, double? quantity, String? note, String fileName, String status, DateTime? processedAt, String? processedByEmail, DateTime createdAt
});




}
/// @nodoc
class __$WeeklySiteReportCopyWithImpl<$Res>
    implements _$WeeklySiteReportCopyWith<$Res> {
  __$WeeklySiteReportCopyWithImpl(this._self, this._then);

  final _WeeklySiteReport _self;
  final $Res Function(_WeeklySiteReport) _then;

/// Create a copy of WeeklySiteReport
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? projectId = null,Object? projectName = null,Object? submittedByEmployeeId = null,Object? submittedByEmployeeName = null,Object? isoYear = null,Object? isoWeek = null,Object? type = null,Object? quantity = freezed,Object? note = freezed,Object? fileName = null,Object? status = null,Object? processedAt = freezed,Object? processedByEmail = freezed,Object? createdAt = null,}) {
  return _then(_WeeklySiteReport(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,projectId: null == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String,projectName: null == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String,submittedByEmployeeId: null == submittedByEmployeeId ? _self.submittedByEmployeeId : submittedByEmployeeId // ignore: cast_nullable_to_non_nullable
as String,submittedByEmployeeName: null == submittedByEmployeeName ? _self.submittedByEmployeeName : submittedByEmployeeName // ignore: cast_nullable_to_non_nullable
as String,isoYear: null == isoYear ? _self.isoYear : isoYear // ignore: cast_nullable_to_non_nullable
as int,isoWeek: null == isoWeek ? _self.isoWeek : isoWeek // ignore: cast_nullable_to_non_nullable
as int,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,quantity: freezed == quantity ? _self.quantity : quantity // ignore: cast_nullable_to_non_nullable
as double?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,processedAt: freezed == processedAt ? _self.processedAt : processedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,processedByEmail: freezed == processedByEmail ? _self.processedByEmail : processedByEmail // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}


/// @nodoc
mixin _$ReportableProject {

 String get id; String get name;
/// Create a copy of ReportableProject
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ReportableProjectCopyWith<ReportableProject> get copyWith => _$ReportableProjectCopyWithImpl<ReportableProject>(this as ReportableProject, _$identity);

  /// Serializes this ReportableProject to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ReportableProject&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name);

@override
String toString() {
  return 'ReportableProject(id: $id, name: $name)';
}


}

/// @nodoc
abstract mixin class $ReportableProjectCopyWith<$Res>  {
  factory $ReportableProjectCopyWith(ReportableProject value, $Res Function(ReportableProject) _then) = _$ReportableProjectCopyWithImpl;
@useResult
$Res call({
 String id, String name
});




}
/// @nodoc
class _$ReportableProjectCopyWithImpl<$Res>
    implements $ReportableProjectCopyWith<$Res> {
  _$ReportableProjectCopyWithImpl(this._self, this._then);

  final ReportableProject _self;
  final $Res Function(ReportableProject) _then;

/// Create a copy of ReportableProject
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [ReportableProject].
extension ReportableProjectPatterns on ReportableProject {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ReportableProject value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ReportableProject() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ReportableProject value)  $default,){
final _that = this;
switch (_that) {
case _ReportableProject():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ReportableProject value)?  $default,){
final _that = this;
switch (_that) {
case _ReportableProject() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ReportableProject() when $default != null:
return $default(_that.id,_that.name);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name)  $default,) {final _that = this;
switch (_that) {
case _ReportableProject():
return $default(_that.id,_that.name);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name)?  $default,) {final _that = this;
switch (_that) {
case _ReportableProject() when $default != null:
return $default(_that.id,_that.name);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ReportableProject implements ReportableProject {
  const _ReportableProject({required this.id, required this.name});
  factory _ReportableProject.fromJson(Map<String, dynamic> json) => _$ReportableProjectFromJson(json);

@override final  String id;
@override final  String name;

/// Create a copy of ReportableProject
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ReportableProjectCopyWith<_ReportableProject> get copyWith => __$ReportableProjectCopyWithImpl<_ReportableProject>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ReportableProjectToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ReportableProject&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name);

@override
String toString() {
  return 'ReportableProject(id: $id, name: $name)';
}


}

/// @nodoc
abstract mixin class _$ReportableProjectCopyWith<$Res> implements $ReportableProjectCopyWith<$Res> {
  factory _$ReportableProjectCopyWith(_ReportableProject value, $Res Function(_ReportableProject) _then) = __$ReportableProjectCopyWithImpl;
@override @useResult
$Res call({
 String id, String name
});




}
/// @nodoc
class __$ReportableProjectCopyWithImpl<$Res>
    implements _$ReportableProjectCopyWith<$Res> {
  __$ReportableProjectCopyWithImpl(this._self, this._then);

  final _ReportableProject _self;
  final $Res Function(_ReportableProject) _then;

/// Create a copy of ReportableProject
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,}) {
  return _then(_ReportableProject(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on
