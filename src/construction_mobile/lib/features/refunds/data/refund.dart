/// Mirrors the API's `RefundDto`: money a person spent for the firm and asks to get back.
class Refund {
  const Refund({
    required this.id,
    required this.employeeName,
    required this.requestedByUserId,
    required this.amount,
    required this.currency,
    required this.expenseDate,
    required this.description,
    required this.status,
    this.projectName,
    this.reviewNote,
    this.payrollYear,
    this.payrollMonth,
  });

  factory Refund.fromJson(Map<String, dynamic> json) => Refund(
        id: json['id'] as String,
        employeeName: json['employeeName'] as String,
        requestedByUserId: json['requestedByUserId'] as String,
        amount: (json['amount'] as num).toDouble(),
        currency: json['currency'] as String,
        expenseDate: DateTime.parse(json['expenseDate'] as String),
        description: json['description'] as String,
        status: json['status'] as String,
        projectName: json['projectName'] as String?,
        reviewNote: json['reviewNote'] as String?,
        payrollYear: json['payrollYear'] as int?,
        payrollMonth: json['payrollMonth'] as int?,
      );

  final String id;
  final String employeeName;
  final String requestedByUserId;
  final double amount;
  final String currency;
  final DateTime expenseDate;
  final String description;

  /// `Requested`, `Approved`, `Rejected` or `Cancelled`.
  final String status;
  final String? projectName;
  final String? reviewNote;
  final int? payrollYear;
  final int? payrollMonth;
}
