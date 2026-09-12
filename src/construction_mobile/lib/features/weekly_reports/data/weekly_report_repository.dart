import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/weekly_site_report.dart';

class WeeklyReportRepository extends ApiRepository {
  const WeeklyReportRepository(super.dio);

  /// Mirrors the API's AttachmentRules, so a file is refused before it is sent.
  static const maxSizeBytes = 20 * 1024 * 1024;

  /// What the caller has submitted themselves — the mobile app's own history,
  /// distinct from the office's whole inbox which stays Admin-and-above.
  Future<PagedList<WeeklySiteReport>> fetchMine({
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    return getPaged(
      '/api/v1/weeklysitereports/mine',
      WeeklySiteReport.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'createdAt',
        sortDescending: true,
      ),
    );
  }

  /// Sites the caller may currently submit a report for — a Foreman/Worker's
  /// own active postings, resolved server-side.
  Future<List<ReportableProject>> fetchReportableProjects() {
    return guard(() async {
      final response = await dio
          .get<List<dynamic>>('/api/v1/weeklysitereports/reportable-projects');

      return (response.data ?? const [])
          .cast<Map<String, dynamic>>()
          .map(ReportableProject.fromJson)
          .toList();
    });
  }

  Future<WeeklySiteReport> submit({
    required String projectId,
    required int isoYear,
    required int isoWeek,
    required String type,
    double? quantity,
    String? note,
    required String filePath,
    required String fileName,
  }) {
    return guard(() async {
      final form = FormData.fromMap(<String, dynamic>{
        'projectId': projectId,
        'isoYear': isoYear,
        'isoWeek': isoWeek,
        'type': type,
        'quantity': ?quantity,
        'note': ?note,
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });

      final response = await dio.post<Map<String, dynamic>>(
        '/api/v1/weeklysitereports',
        data: form,
      );

      return WeeklySiteReport.fromJson(response.data!);
    });
  }
}

final weeklyReportRepositoryProvider = Provider<WeeklyReportRepository>((ref) {
  return WeeklyReportRepository(ref.watch(apiClientProvider));
});

final reportableProjectsProvider =
    FutureProvider.autoDispose<List<ReportableProject>>((ref) {
  return ref.watch(weeklyReportRepositoryProvider).fetchReportableProjects();
});
