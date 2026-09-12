import 'package:freezed_annotation/freezed_annotation.dart';

part 'ledger.freezed.dart';
part 'ledger.g.dart';

/// Mirrors the API's `LedgerSummaryDto` — one row in the ledger list, no
/// columns/sections/rows, just enough to pick one.
@freezed
abstract class LedgerSummary with _$LedgerSummary {
  const factory LedgerSummary({
    required String id,
    required String name,
    required int year,
    required int month,
    String? note,
    String? createdByName,
    required DateTime createdAt,
  }) = _LedgerSummary;

  factory LedgerSummary.fromJson(Map<String, dynamic> json) =>
      _$LedgerSummaryFromJson(json);
}

/// Mirrors the API's `LedgerColumnDto`.
@freezed
abstract class LedgerColumn with _$LedgerColumn {
  const factory LedgerColumn({
    required String id,
    required String name,
    required String dataType,
    String? sourceMetric,
    @Default(0) int sortOrder,
  }) = _LedgerColumn;

  factory LedgerColumn.fromJson(Map<String, dynamic> json) =>
      _$LedgerColumnFromJson(json);
}

/// Mirrors the API's `LedgerSectionDto` — a shell (no rows) when it comes
/// from the ledger detail fetch, populated once its own rows are fetched.
@freezed
abstract class LedgerSection with _$LedgerSection {
  const factory LedgerSection({
    required String id,
    required String name,
    String? projectId,
    String? projectName,
    @Default(0) int sortOrder,
    @Default(0) int rowCount,
    @Default(<LedgerRow>[]) List<LedgerRow> rows,
  }) = _LedgerSection;

  factory LedgerSection.fromJson(Map<String, dynamic> json) =>
      _$LedgerSectionFromJson(json);
}

/// Mirrors the API's `LedgerRowDto`.
@freezed
abstract class LedgerRow with _$LedgerRow {
  const factory LedgerRow({
    required String id,
    required String label,
    String? employeeId,
    String? employeeName,
    String? vehicleId,
    String? vehicleName,
    String? toolId,
    String? toolName,
    String? materialId,
    String? materialName,
    String? promotedGeneralExpenseId,
    String? promotedAccommodationRateId,
    String? colorTag,
    @Default(0) int sortOrder,
    @Default(<LedgerCell>[]) List<LedgerCell> cells,
  }) = _LedgerRow;

  const LedgerRow._();

  /// The linked real record's name, whichever kind this row points at.
  String? get linkedName =>
      employeeName ?? vehicleName ?? toolName ?? materialName;

  factory LedgerRow.fromJson(Map<String, dynamic> json) =>
      _$LedgerRowFromJson(json);
}

/// Mirrors the API's `LedgerCellDto`.
@freezed
abstract class LedgerCell with _$LedgerCell {
  const factory LedgerCell({
    String? id,
    required String columnId,
    String? value,
    String? colorTag,
    @Default(false) bool isComputed,
  }) = _LedgerCell;

  factory LedgerCell.fromJson(Map<String, dynamic> json) =>
      _$LedgerCellFromJson(json);
}

/// Mirrors the API's `LedgerDetailDto` — the ledger shell: columns and
/// section headers (with row counts), but no rows or cells until a section
/// is opened.
@freezed
abstract class LedgerDetail with _$LedgerDetail {
  const factory LedgerDetail({
    required String id,
    required String name,
    required int year,
    required int month,
    String? note,
    String? createdByName,
    required DateTime createdAt,
    DateTime? updatedAt,
    @Default(<LedgerColumn>[]) List<LedgerColumn> columns,
    @Default(<LedgerSection>[]) List<LedgerSection> sections,
  }) = _LedgerDetail;

  factory LedgerDetail.fromJson(Map<String, dynamic> json) =>
      _$LedgerDetailFromJson(json);
}

/// Mirrors the API's `LedgerSummaryBoxDto`.
@freezed
abstract class LedgerSummaryBox with _$LedgerSummaryBox {
  const factory LedgerSummaryBox({
    required String id,
    required String label,
    String? sourceColumnId,
    String? sourceColumnName,
    double? manualValue,
    @Default(1) int sign,
    String? color,
    @Default(0) int sortOrder,
    @Default(0) double value,
  }) = _LedgerSummaryBox;

  factory LedgerSummaryBox.fromJson(Map<String, dynamic> json) =>
      _$LedgerSummaryBoxFromJson(json);
}

/// Mirrors the API's `LedgerSummaryPanelDto`.
@freezed
abstract class LedgerSummaryPanel with _$LedgerSummaryPanel {
  const factory LedgerSummaryPanel({
    @Default(<LedgerSummaryBox>[]) List<LedgerSummaryBox> boxes,
    @Default(0) double netTotal,
  }) = _LedgerSummaryPanel;

  factory LedgerSummaryPanel.fromJson(Map<String, dynamic> json) =>
      _$LedgerSummaryPanelFromJson(json);
}
