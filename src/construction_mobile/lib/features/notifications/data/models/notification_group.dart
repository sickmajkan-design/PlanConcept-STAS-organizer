import 'package:freezed_annotation/freezed_annotation.dart';

part 'notification_group.freezed.dart';
part 'notification_group.g.dart';

/// Just enough of the API's `NotificationGroupDto` to power the announcement
/// form's audience picker — mobile has no group management screen (that stays
/// desktop-only), only the ability to send to one.
@freezed
abstract class NotificationGroup with _$NotificationGroup {
  const factory NotificationGroup({
    required String id,
    required String name,
  }) = _NotificationGroup;

  factory NotificationGroup.fromJson(Map<String, dynamic> json) =>
      _$NotificationGroupFromJson(json);
}
