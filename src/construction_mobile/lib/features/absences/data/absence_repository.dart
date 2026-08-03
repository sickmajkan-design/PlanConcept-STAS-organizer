import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/absence.dart';
import 'models/schedule.dart';

class AbsenceRepository extends ApiRepository {
  const AbsenceRepository(super.dio);

  /// The caller's own leave. The API narrows a Worker to their own rows, so no
  /// employee filter is sent.
  Future<PagedList<Absence>> fetchMine({
    int pageNumber = 1,
    int pageSize = 20,
    String? status,
  }) {
    return getPaged(
      '/api/absences',
      Absence.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'startDate',
        sortDescending: true,
        filters: {'status': status},
      ),
    );
  }

  /// Where the caller is posted over a window, and when they are away.
  ///
  /// The same endpoint the admin board uses; the server sends a worker their
  /// own line only.
  Future<Schedule> fetchSchedule({
    required DateTime from,
    required DateTime to,
  }) {
    return getJson(
      '/api/schedule',
      Schedule.fromJson,
      query: <String, dynamic>{
        'from': _asDate(from),
        'to': _asDate(to),
      },
    );
  }

  /// Asks for time off. A worker may only ask for their own, so no employee is
  /// sent and the server takes it from the token.
  Future<Absence> request({
    required String type,
    required DateTime startDate,
    required DateTime endDate,
    String? reason,
  }) {
    return postJson(
      '/api/absences',
      Absence.fromJson,
      data: <String, dynamic>{
        'type': type,
        'startDate': _asDate(startDate),
        'endDate': _asDate(endDate),
        'reason': ?reason,
      },
    );
  }

  /// Takes back an unanswered request.
  Future<void> withdraw(String id) => deleteVoid('/api/absences/$id');

  /// The API speaks `DateOnly`; sending an instant would put a time and a zone
  /// on a value that has neither.
  static String _asDate(DateTime value) {
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '${value.year}-$month-$day';
  }
}

final absenceRepositoryProvider = Provider<AbsenceRepository>((ref) {
  return AbsenceRepository(ref.watch(apiClientProvider));
});
