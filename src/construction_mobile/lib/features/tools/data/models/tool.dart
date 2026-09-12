import 'package:freezed_annotation/freezed_annotation.dart';

part 'tool.freezed.dart';
part 'tool.g.dart';

/// Mirrors the API's `ToolDto`, used for the list, the detail screen and the
/// QR lookup — all three endpoints serve the same shape.
@freezed
abstract class Tool with _$Tool {
  const factory Tool({
    required String id,
    required String name,
    String? category,
    String? serialNumber,
    String? qrCode,
    required String status,

    /// `"Owned"` or `"Rented"`.
    @Default('Owned') String ownershipType,

    /// Set when a rental/lease rate is currently in force. Null for an owned
    /// tool, or one with no rate on file.
    double? currentRentalMonthlyAmount,
    String? currentRentalProvider,
    String? assignedEmployeeId,
    String? assignedEmployeeName,
    String? assignedEmployeeNumber,
    String? assignedProjectId,
    String? assignedProjectName,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _Tool;

  const Tool._();

  factory Tool.fromJson(Map<String, dynamic> json) => _$ToolFromJson(json);

  bool get isAssignedToEmployee => assignedEmployeeId != null;

  bool get isAssignedToProject => assignedProjectId != null;

  bool get isRented => ownershipType == 'Rented';
}
