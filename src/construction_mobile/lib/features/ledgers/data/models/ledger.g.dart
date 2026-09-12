// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ledger.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_LedgerSummary _$LedgerSummaryFromJson(Map<String, dynamic> json) =>
    _LedgerSummary(
      id: json['id'] as String,
      name: json['name'] as String,
      year: (json['year'] as num).toInt(),
      month: (json['month'] as num).toInt(),
      note: json['note'] as String?,
      createdByName: json['createdByName'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$LedgerSummaryToJson(_LedgerSummary instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'year': instance.year,
      'month': instance.month,
      'note': ?instance.note,
      'createdByName': ?instance.createdByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };

_LedgerColumn _$LedgerColumnFromJson(Map<String, dynamic> json) =>
    _LedgerColumn(
      id: json['id'] as String,
      name: json['name'] as String,
      dataType: json['dataType'] as String,
      sourceMetric: json['sourceMetric'] as String?,
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$LedgerColumnToJson(_LedgerColumn instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'dataType': instance.dataType,
      'sourceMetric': ?instance.sourceMetric,
      'sortOrder': instance.sortOrder,
    };

_LedgerSection _$LedgerSectionFromJson(Map<String, dynamic> json) =>
    _LedgerSection(
      id: json['id'] as String,
      name: json['name'] as String,
      projectId: json['projectId'] as String?,
      projectName: json['projectName'] as String?,
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
      rowCount: (json['rowCount'] as num?)?.toInt() ?? 0,
      rows:
          (json['rows'] as List<dynamic>?)
              ?.map((e) => LedgerRow.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <LedgerRow>[],
    );

Map<String, dynamic> _$LedgerSectionToJson(_LedgerSection instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'projectId': ?instance.projectId,
      'projectName': ?instance.projectName,
      'sortOrder': instance.sortOrder,
      'rowCount': instance.rowCount,
      'rows': instance.rows.map((e) => e.toJson()).toList(),
    };

_LedgerRow _$LedgerRowFromJson(Map<String, dynamic> json) => _LedgerRow(
  id: json['id'] as String,
  label: json['label'] as String,
  employeeId: json['employeeId'] as String?,
  employeeName: json['employeeName'] as String?,
  vehicleId: json['vehicleId'] as String?,
  vehicleName: json['vehicleName'] as String?,
  toolId: json['toolId'] as String?,
  toolName: json['toolName'] as String?,
  materialId: json['materialId'] as String?,
  materialName: json['materialName'] as String?,
  promotedGeneralExpenseId: json['promotedGeneralExpenseId'] as String?,
  promotedAccommodationRateId: json['promotedAccommodationRateId'] as String?,
  colorTag: json['colorTag'] as String?,
  sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
  cells:
      (json['cells'] as List<dynamic>?)
          ?.map((e) => LedgerCell.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <LedgerCell>[],
);

Map<String, dynamic> _$LedgerRowToJson(_LedgerRow instance) =>
    <String, dynamic>{
      'id': instance.id,
      'label': instance.label,
      'employeeId': ?instance.employeeId,
      'employeeName': ?instance.employeeName,
      'vehicleId': ?instance.vehicleId,
      'vehicleName': ?instance.vehicleName,
      'toolId': ?instance.toolId,
      'toolName': ?instance.toolName,
      'materialId': ?instance.materialId,
      'materialName': ?instance.materialName,
      'promotedGeneralExpenseId': ?instance.promotedGeneralExpenseId,
      'promotedAccommodationRateId': ?instance.promotedAccommodationRateId,
      'colorTag': ?instance.colorTag,
      'sortOrder': instance.sortOrder,
      'cells': instance.cells.map((e) => e.toJson()).toList(),
    };

_LedgerCell _$LedgerCellFromJson(Map<String, dynamic> json) => _LedgerCell(
  id: json['id'] as String?,
  columnId: json['columnId'] as String,
  value: json['value'] as String?,
  colorTag: json['colorTag'] as String?,
  isComputed: json['isComputed'] as bool? ?? false,
);

Map<String, dynamic> _$LedgerCellToJson(_LedgerCell instance) =>
    <String, dynamic>{
      'id': ?instance.id,
      'columnId': instance.columnId,
      'value': ?instance.value,
      'colorTag': ?instance.colorTag,
      'isComputed': instance.isComputed,
    };

_LedgerDetail _$LedgerDetailFromJson(Map<String, dynamic> json) =>
    _LedgerDetail(
      id: json['id'] as String,
      name: json['name'] as String,
      year: (json['year'] as num).toInt(),
      month: (json['month'] as num).toInt(),
      note: json['note'] as String?,
      createdByName: json['createdByName'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
      columns:
          (json['columns'] as List<dynamic>?)
              ?.map((e) => LedgerColumn.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <LedgerColumn>[],
      sections:
          (json['sections'] as List<dynamic>?)
              ?.map((e) => LedgerSection.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <LedgerSection>[],
    );

Map<String, dynamic> _$LedgerDetailToJson(_LedgerDetail instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'year': instance.year,
      'month': instance.month,
      'note': ?instance.note,
      'createdByName': ?instance.createdByName,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': ?instance.updatedAt?.toIso8601String(),
      'columns': instance.columns.map((e) => e.toJson()).toList(),
      'sections': instance.sections.map((e) => e.toJson()).toList(),
    };

_LedgerSummaryBox _$LedgerSummaryBoxFromJson(Map<String, dynamic> json) =>
    _LedgerSummaryBox(
      id: json['id'] as String,
      label: json['label'] as String,
      sourceColumnId: json['sourceColumnId'] as String?,
      sourceColumnName: json['sourceColumnName'] as String?,
      manualValue: (json['manualValue'] as num?)?.toDouble(),
      sign: (json['sign'] as num?)?.toInt() ?? 1,
      color: json['color'] as String?,
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
      value: (json['value'] as num?)?.toDouble() ?? 0,
    );

Map<String, dynamic> _$LedgerSummaryBoxToJson(_LedgerSummaryBox instance) =>
    <String, dynamic>{
      'id': instance.id,
      'label': instance.label,
      'sourceColumnId': ?instance.sourceColumnId,
      'sourceColumnName': ?instance.sourceColumnName,
      'manualValue': ?instance.manualValue,
      'sign': instance.sign,
      'color': ?instance.color,
      'sortOrder': instance.sortOrder,
      'value': instance.value,
    };

_LedgerSummaryPanel _$LedgerSummaryPanelFromJson(Map<String, dynamic> json) =>
    _LedgerSummaryPanel(
      boxes:
          (json['boxes'] as List<dynamic>?)
              ?.map((e) => LedgerSummaryBox.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <LedgerSummaryBox>[],
      netTotal: (json['netTotal'] as num?)?.toDouble() ?? 0,
    );

Map<String, dynamic> _$LedgerSummaryPanelToJson(_LedgerSummaryPanel instance) =>
    <String, dynamic>{
      'boxes': instance.boxes.map((e) => e.toJson()).toList(),
      'netTotal': instance.netTotal,
    };
