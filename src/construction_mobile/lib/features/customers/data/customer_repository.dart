import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/customer.dart';

class CustomerRepository extends ApiRepository {
  const CustomerRepository(super.dio);

  /// Every customer, for a picker — the list is realistically small enough
  /// company-wide that a single generously-sized page beats building search
  /// into a dropdown nobody will scroll far in.
  Future<List<Customer>> fetchAll() async {
    final page = await getPaged(
      '/api/v1/customers',
      Customer.fromJson,
      query: pagedQuery(pageNumber: 1, pageSize: 200),
    );

    return page.items;
  }
}

final customerRepositoryProvider = Provider<CustomerRepository>((ref) {
  return CustomerRepository(ref.watch(apiClientProvider));
});

final allCustomersProvider = FutureProvider.autoDispose<List<Customer>>((ref) {
  return ref.watch(customerRepositoryProvider).fetchAll();
});
