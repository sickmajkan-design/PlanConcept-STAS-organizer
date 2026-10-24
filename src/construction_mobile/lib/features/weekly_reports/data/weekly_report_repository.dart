import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/weekly_site_report.dart';

/// A person who can be put in a report: name and position only.
class ReportWorker {
  const ReportWorker({required this.id, required this.fullName, this.position});

  factory ReportWorker.fromJson(Map<String, dynamic> json) => ReportWorker(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        position: json['position'] as String?,
      );

  final String id;
  final String fullName;
  final String? position;
}

/// One picture of the signed sheet, on the phone.
class ReportPicture {
  const ReportPicture({required this.path, required this.name});

  final String path;
  final String name;
}

/// One worker's hours for the report's week; [hoursSecondMonth] is the part that falls in the
/// later month when the week runs over a month's end.
class ReportLine {
  const ReportLine({
    required this.employeeId,
    required this.hours,
    this.hoursSecondMonth = 0,
  });

  final String employeeId;
  final double hours;
  final double hoursSecondMonth;

  Map<String, dynamic> toJson() => {
        'employeeId': employeeId,
        'hours': hours,
        'hoursSecondMonth': hoursSecondMonth,
      };
}

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

  /// Sends a report: the pictures of the signed sheet (a week can be written on several)
  /// and the workers with their hours. Without workers the total goes as [quantity].
  /// People to put in a report: the crew of the caller's own sites, or anyone by name once two
  /// letters are typed (a foreman can have somebody with him who was never entered in the planner).
  Future<List<ReportWorker>> fetchReportWorkers({String? search}) {
    return guard(() async {
      final response = await dio.get<List<dynamic>>(
        '/api/v1/weeklysitereports/workers',
        queryParameters: {
          if (search != null && search.isNotEmpty) 'search': search,
        },
      );

      return (response.data ?? const [])
          .cast<Map<String, dynamic>>()
          .map(ReportWorker.fromJson)
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
    required List<ReportPicture> pictures,
    List<ReportLine> lines = const [],
  }) {
    return guard(() async {
      final form = FormData.fromMap(<String, dynamic>{
        'projectId': projectId,
        'isoYear': isoYear,
        'isoWeek': isoWeek,
        'type': type,
        'quantity': ?quantity,
        'note': ?note,
        if (lines.isNotEmpty)
          'lines': jsonEncode([for (final line in lines) line.toJson()]),
      });

      for (final picture in pictures) {
        form.files.add(
          MapEntry(
            'files',
            await MultipartFile.fromFile(picture.path, filename: picture.name),
          ),
        );
      }

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
