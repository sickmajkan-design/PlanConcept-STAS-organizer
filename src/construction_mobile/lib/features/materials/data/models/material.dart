import 'package:freezed_annotation/freezed_annotation.dart';

part 'material.freezed.dart';
part 'material.g.dart';

/// Mirrors the API's `MaterialDto`, used for both the list and the detail
/// screen — the API serves the same shape from both endpoints.
///
/// Named `MaterialItem` rather than `Material` because the latter collides
/// with Flutter's own `Material` widget, which every screen in this app
/// imports.
@freezed
abstract class MaterialItem with _$MaterialItem {
  const factory MaterialItem({
    required String id,
    required String name,
    required String unit,
    required double quantity,
    String? warehouse,
    String? projectId,
    String? projectName,
    required DateTime lastUpdated,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _MaterialItem;

  const MaterialItem._();

  factory MaterialItem.fromJson(Map<String, dynamic> json) =>
      _$MaterialItemFromJson(json);

  bool get isAssignedToProject => projectId != null;
}
