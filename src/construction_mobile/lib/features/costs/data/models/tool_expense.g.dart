// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'tool_expense.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_ToolExpense _$ToolExpenseFromJson(Map<String, dynamic> json) => _ToolExpense(
  id: json['id'] as String,
  toolId: json['toolId'] as String,
  toolName: json['toolName'] as String,
  kind: json['kind'] as String,
  amount: (json['amount'] as num).toDouble(),
  occurredOn: json['occurredOn'] as String,
  supplier: json['supplier'] as String?,
  note: json['note'] as String?,
  recordedByName: json['recordedByName'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$ToolExpenseToJson(_ToolExpense instance) =>
    <String, dynamic>{
      'id': instance.id,
      'toolId': instance.toolId,
      'toolName': instance.toolName,
      'kind': instance.kind,
      'amount': instance.amount,
      'occurredOn': instance.occurredOn,
      'supplier': ?instance.supplier,
      'note': ?instance.note,
      'recordedByName': ?instance.recordedByName,
      'createdAt': instance.createdAt.toIso8601String(),
    };
