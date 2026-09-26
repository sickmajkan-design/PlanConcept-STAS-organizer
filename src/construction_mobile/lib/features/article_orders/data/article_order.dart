/// One line of a request for articles.
class ArticleOrderItem {
  const ArticleOrderItem({
    required this.id,
    required this.name,
    required this.quantity,
    this.unit,
    this.note,
  });

  factory ArticleOrderItem.fromJson(Map<String, dynamic> json) => ArticleOrderItem(
        id: json['id'] as String,
        name: json['name'] as String,
        quantity: (json['quantity'] as num).toDouble(),
        unit: json['unit'] as String?,
        note: json['note'] as String?,
      );

  final String id;
  final String name;
  final double quantity;
  final String? unit;
  final String? note;

  /// "1 par Radne cipele (broj 43)", the quantity without a trailing ".0".
  String get summary {
    final amount = quantity == quantity.roundToDouble() ? quantity.round().toString() : quantity.toString();
    final parts = <String>[amount, if ((unit ?? '').isNotEmpty) unit!, name];
    final text = parts.join(' ');

    return (note ?? '').isEmpty ? text : '$text ($note)';
  }
}

/// Mirrors the API's `ArticleOrderDto`: articles a person needs for the job,
/// from asked for to in their hands.
class ArticleOrder {
  const ArticleOrder({
    required this.id,
    required this.status,
    required this.urgent,
    required this.requestedByUserId,
    required this.requestedByName,
    required this.createdAt,
    required this.items,
    this.note,
    this.reviewNote,
    this.projectName,
  });

  factory ArticleOrder.fromJson(Map<String, dynamic> json) => ArticleOrder(
        id: json['id'] as String,
        status: json['status'] as String,
        urgent: json['urgent'] as bool? ?? false,
        requestedByUserId: json['requestedByUserId'] as String,
        requestedByName: json['requestedByName'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
        note: json['note'] as String?,
        reviewNote: json['reviewNote'] as String?,
        projectName: json['projectName'] as String?,
        items: [
          for (final item in (json['items'] as List<dynamic>? ?? const <dynamic>[]))
            ArticleOrderItem.fromJson(item as Map<String, dynamic>),
        ],
      );

  final String id;

  /// `Requested`, `Ordered`, `InDelivery`, `Delivered`, `Rejected` or `Cancelled`.
  final String status;
  final bool urgent;
  final String requestedByUserId;
  final String requestedByName;
  final DateTime createdAt;
  final String? note;
  final String? reviewNote;
  final String? projectName;
  final List<ArticleOrderItem> items;
}
