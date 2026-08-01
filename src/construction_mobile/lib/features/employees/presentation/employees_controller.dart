import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/filtered_paged_list_notifier.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/employee_repository.dart';
import '../data/models/employee.dart';

/// Employee statuses offered as filter chips, mirroring the API's enum.
const employeeStatusFilters = <String>[
  'Active',
  'OnLeave',
  'Suspended',
  'Terminated',
];

class EmployeesController extends FilteredPagedListNotifier<Employee> {
  @override
  Future<PagedList<Employee>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(employeeRepositoryProvider).fetchEmployees(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
          status: filter,
        );
  }
}

final employeesControllerProvider =
    AsyncNotifierProvider<EmployeesController, PagedState<Employee>>(
  EmployeesController.new,
);

/// Detail of a single employee, keyed by id.
final employeeDetailProvider =
    FutureProvider.autoDispose.family<EmployeeDetail, String>((ref, id) {
  return ref.watch(employeeRepositoryProvider).fetchEmployee(id);
});
