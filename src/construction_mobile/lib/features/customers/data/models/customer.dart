import 'package:freezed_annotation/freezed_annotation.dart';

part 'customer.freezed.dart';
part 'customer.g.dart';

/// Just enough of the API's `CustomerDto` to power the Project form's picker
/// — this is not a Customer-management feature, mobile has none of that yet.
@freezed
abstract class Customer with _$Customer {
  const factory Customer({
    required String id,
    required String name,
  }) = _Customer;

  factory Customer.fromJson(Map<String, dynamic> json) =>
      _$CustomerFromJson(json);
}
