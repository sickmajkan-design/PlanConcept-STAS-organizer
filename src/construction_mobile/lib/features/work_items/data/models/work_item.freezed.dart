// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'work_item.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$WorkItem {

 String get id; String get kind; String get title; String? get description; String? get projectId; String? get projectName; String? get assignedEmployeeId; String? get assignedEmployeeName; String get priority; String get status;/// `YYYY-MM-DD`, or null when nobody set a deadline.
 String? get dueDate; double? get latitude; double? get longitude; String? get createdByName; String? get resolvedByName; DateTime? get resolvedAt; int get attachmentCount; bool get isFinished; DateTime get createdAt; DateTime? get updatedAt;
/// Create a copy of WorkItem
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$WorkItemCopyWith<WorkItem> get copyWith => _$WorkItemCopyWithImpl<WorkItem>(this as WorkItem, _$identity);

  /// Serializes this WorkItem to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is WorkItem&&(identical(other.id, id) || other.id == id)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.priority, priority) || other.priority == priority)&&(identical(other.status, status) || other.status == status)&&(identical(other.dueDate, dueDate) || other.dueDate == dueDate)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.resolvedByName, resolvedByName) || other.resolvedByName == resolvedByName)&&(identical(other.resolvedAt, resolvedAt) || other.resolvedAt == resolvedAt)&&(identical(other.attachmentCount, attachmentCount) || other.attachmentCount == attachmentCount)&&(identical(other.isFinished, isFinished) || other.isFinished == isFinished)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,kind,title,description,projectId,projectName,assignedEmployeeId,assignedEmployeeName,priority,status,dueDate,latitude,longitude,createdByName,resolvedByName,resolvedAt,attachmentCount,isFinished,createdAt,updatedAt]);

@override
String toString() {
  return 'WorkItem(id: $id, kind: $kind, title: $title, description: $description, projectId: $projectId, projectName: $projectName, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, priority: $priority, status: $status, dueDate: $dueDate, latitude: $latitude, longitude: $longitude, createdByName: $createdByName, resolvedByName: $resolvedByName, resolvedAt: $resolvedAt, attachmentCount: $attachmentCount, isFinished: $isFinished, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class $WorkItemCopyWith<$Res>  {
  factory $WorkItemCopyWith(WorkItem value, $Res Function(WorkItem) _then) = _$WorkItemCopyWithImpl;
@useResult
$Res call({
 String id, String kind, String title, String? description, String? projectId, String? projectName, String? assignedEmployeeId, String? assignedEmployeeName, String priority, String status, String? dueDate, double? latitude, double? longitude, String? createdByName, String? resolvedByName, DateTime? resolvedAt, int attachmentCount, bool isFinished, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class _$WorkItemCopyWithImpl<$Res>
    implements $WorkItemCopyWith<$Res> {
  _$WorkItemCopyWithImpl(this._self, this._then);

  final WorkItem _self;
  final $Res Function(WorkItem) _then;

/// Create a copy of WorkItem
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? kind = null,Object? title = null,Object? description = freezed,Object? projectId = freezed,Object? projectName = freezed,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? priority = null,Object? status = null,Object? dueDate = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? createdByName = freezed,Object? resolvedByName = freezed,Object? resolvedAt = freezed,Object? attachmentCount = null,Object? isFinished = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,priority: null == priority ? _self.priority : priority // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,dueDate: freezed == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,resolvedByName: freezed == resolvedByName ? _self.resolvedByName : resolvedByName // ignore: cast_nullable_to_non_nullable
as String?,resolvedAt: freezed == resolvedAt ? _self.resolvedAt : resolvedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,attachmentCount: null == attachmentCount ? _self.attachmentCount : attachmentCount // ignore: cast_nullable_to_non_nullable
as int,isFinished: null == isFinished ? _self.isFinished : isFinished // ignore: cast_nullable_to_non_nullable
as bool,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [WorkItem].
extension WorkItemPatterns on WorkItem {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _WorkItem value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _WorkItem() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _WorkItem value)  $default,){
final _that = this;
switch (_that) {
case _WorkItem():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _WorkItem value)?  $default,){
final _that = this;
switch (_that) {
case _WorkItem() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String kind,  String title,  String? description,  String? projectId,  String? projectName,  String? assignedEmployeeId,  String? assignedEmployeeName,  String priority,  String status,  String? dueDate,  double? latitude,  double? longitude,  String? createdByName,  String? resolvedByName,  DateTime? resolvedAt,  int attachmentCount,  bool isFinished,  DateTime createdAt,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _WorkItem() when $default != null:
return $default(_that.id,_that.kind,_that.title,_that.description,_that.projectId,_that.projectName,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.priority,_that.status,_that.dueDate,_that.latitude,_that.longitude,_that.createdByName,_that.resolvedByName,_that.resolvedAt,_that.attachmentCount,_that.isFinished,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String kind,  String title,  String? description,  String? projectId,  String? projectName,  String? assignedEmployeeId,  String? assignedEmployeeName,  String priority,  String status,  String? dueDate,  double? latitude,  double? longitude,  String? createdByName,  String? resolvedByName,  DateTime? resolvedAt,  int attachmentCount,  bool isFinished,  DateTime createdAt,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _WorkItem():
return $default(_that.id,_that.kind,_that.title,_that.description,_that.projectId,_that.projectName,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.priority,_that.status,_that.dueDate,_that.latitude,_that.longitude,_that.createdByName,_that.resolvedByName,_that.resolvedAt,_that.attachmentCount,_that.isFinished,_that.createdAt,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String kind,  String title,  String? description,  String? projectId,  String? projectName,  String? assignedEmployeeId,  String? assignedEmployeeName,  String priority,  String status,  String? dueDate,  double? latitude,  double? longitude,  String? createdByName,  String? resolvedByName,  DateTime? resolvedAt,  int attachmentCount,  bool isFinished,  DateTime createdAt,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _WorkItem() when $default != null:
return $default(_that.id,_that.kind,_that.title,_that.description,_that.projectId,_that.projectName,_that.assignedEmployeeId,_that.assignedEmployeeName,_that.priority,_that.status,_that.dueDate,_that.latitude,_that.longitude,_that.createdByName,_that.resolvedByName,_that.resolvedAt,_that.attachmentCount,_that.isFinished,_that.createdAt,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _WorkItem extends WorkItem {
  const _WorkItem({required this.id, required this.kind, required this.title, this.description, this.projectId, this.projectName, this.assignedEmployeeId, this.assignedEmployeeName, required this.priority, required this.status, this.dueDate, this.latitude, this.longitude, this.createdByName, this.resolvedByName, this.resolvedAt, this.attachmentCount = 0, this.isFinished = false, required this.createdAt, this.updatedAt}): super._();
  factory _WorkItem.fromJson(Map<String, dynamic> json) => _$WorkItemFromJson(json);

@override final  String id;
@override final  String kind;
@override final  String title;
@override final  String? description;
@override final  String? projectId;
@override final  String? projectName;
@override final  String? assignedEmployeeId;
@override final  String? assignedEmployeeName;
@override final  String priority;
@override final  String status;
/// `YYYY-MM-DD`, or null when nobody set a deadline.
@override final  String? dueDate;
@override final  double? latitude;
@override final  double? longitude;
@override final  String? createdByName;
@override final  String? resolvedByName;
@override final  DateTime? resolvedAt;
@override@JsonKey() final  int attachmentCount;
@override@JsonKey() final  bool isFinished;
@override final  DateTime createdAt;
@override final  DateTime? updatedAt;

/// Create a copy of WorkItem
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$WorkItemCopyWith<_WorkItem> get copyWith => __$WorkItemCopyWithImpl<_WorkItem>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$WorkItemToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _WorkItem&&(identical(other.id, id) || other.id == id)&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.projectId, projectId) || other.projectId == projectId)&&(identical(other.projectName, projectName) || other.projectName == projectName)&&(identical(other.assignedEmployeeId, assignedEmployeeId) || other.assignedEmployeeId == assignedEmployeeId)&&(identical(other.assignedEmployeeName, assignedEmployeeName) || other.assignedEmployeeName == assignedEmployeeName)&&(identical(other.priority, priority) || other.priority == priority)&&(identical(other.status, status) || other.status == status)&&(identical(other.dueDate, dueDate) || other.dueDate == dueDate)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.createdByName, createdByName) || other.createdByName == createdByName)&&(identical(other.resolvedByName, resolvedByName) || other.resolvedByName == resolvedByName)&&(identical(other.resolvedAt, resolvedAt) || other.resolvedAt == resolvedAt)&&(identical(other.attachmentCount, attachmentCount) || other.attachmentCount == attachmentCount)&&(identical(other.isFinished, isFinished) || other.isFinished == isFinished)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,kind,title,description,projectId,projectName,assignedEmployeeId,assignedEmployeeName,priority,status,dueDate,latitude,longitude,createdByName,resolvedByName,resolvedAt,attachmentCount,isFinished,createdAt,updatedAt]);

@override
String toString() {
  return 'WorkItem(id: $id, kind: $kind, title: $title, description: $description, projectId: $projectId, projectName: $projectName, assignedEmployeeId: $assignedEmployeeId, assignedEmployeeName: $assignedEmployeeName, priority: $priority, status: $status, dueDate: $dueDate, latitude: $latitude, longitude: $longitude, createdByName: $createdByName, resolvedByName: $resolvedByName, resolvedAt: $resolvedAt, attachmentCount: $attachmentCount, isFinished: $isFinished, createdAt: $createdAt, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$WorkItemCopyWith<$Res> implements $WorkItemCopyWith<$Res> {
  factory _$WorkItemCopyWith(_WorkItem value, $Res Function(_WorkItem) _then) = __$WorkItemCopyWithImpl;
@override @useResult
$Res call({
 String id, String kind, String title, String? description, String? projectId, String? projectName, String? assignedEmployeeId, String? assignedEmployeeName, String priority, String status, String? dueDate, double? latitude, double? longitude, String? createdByName, String? resolvedByName, DateTime? resolvedAt, int attachmentCount, bool isFinished, DateTime createdAt, DateTime? updatedAt
});




}
/// @nodoc
class __$WorkItemCopyWithImpl<$Res>
    implements _$WorkItemCopyWith<$Res> {
  __$WorkItemCopyWithImpl(this._self, this._then);

  final _WorkItem _self;
  final $Res Function(_WorkItem) _then;

/// Create a copy of WorkItem
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? kind = null,Object? title = null,Object? description = freezed,Object? projectId = freezed,Object? projectName = freezed,Object? assignedEmployeeId = freezed,Object? assignedEmployeeName = freezed,Object? priority = null,Object? status = null,Object? dueDate = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? createdByName = freezed,Object? resolvedByName = freezed,Object? resolvedAt = freezed,Object? attachmentCount = null,Object? isFinished = null,Object? createdAt = null,Object? updatedAt = freezed,}) {
  return _then(_WorkItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,projectId: freezed == projectId ? _self.projectId : projectId // ignore: cast_nullable_to_non_nullable
as String?,projectName: freezed == projectName ? _self.projectName : projectName // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeId: freezed == assignedEmployeeId ? _self.assignedEmployeeId : assignedEmployeeId // ignore: cast_nullable_to_non_nullable
as String?,assignedEmployeeName: freezed == assignedEmployeeName ? _self.assignedEmployeeName : assignedEmployeeName // ignore: cast_nullable_to_non_nullable
as String?,priority: null == priority ? _self.priority : priority // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,dueDate: freezed == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,createdByName: freezed == createdByName ? _self.createdByName : createdByName // ignore: cast_nullable_to_non_nullable
as String?,resolvedByName: freezed == resolvedByName ? _self.resolvedByName : resolvedByName // ignore: cast_nullable_to_non_nullable
as String?,resolvedAt: freezed == resolvedAt ? _self.resolvedAt : resolvedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,attachmentCount: null == attachmentCount ? _self.attachmentCount : attachmentCount // ignore: cast_nullable_to_non_nullable
as int,isFinished: null == isFinished ? _self.isFinished : isFinished // ignore: cast_nullable_to_non_nullable
as bool,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
