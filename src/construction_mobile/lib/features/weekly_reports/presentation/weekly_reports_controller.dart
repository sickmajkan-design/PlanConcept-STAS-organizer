import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/models/weekly_site_report.dart';
import '../data/weekly_report_repository.dart';

/// What the signed-in foreman has submitted themselves, newest first.
class WeeklyReportsController extends PagedListNotifier<WeeklySiteReport> {
  @override
  Future<PagedList<WeeklySiteReport>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    // The endpoint has no text search; the base class still supplies one and
    // sending it would filter on a parameter the API ignores.
    return ref.read(weeklyReportRepositoryProvider).fetchMine(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
        );
  }
}

final weeklyReportsControllerProvider =
    AsyncNotifierProvider<WeeklyReportsController, PagedState<WeeklySiteReport>>(
  WeeklyReportsController.new,
);
