import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/time_entry.dart';

class TimeEntryRepository extends ApiRepository {
  const TimeEntryRepository(super.dio);

  /// The caller's own entries. The API narrows a Worker to their own rows, so
  /// no employee filter is sent — asking for someone else's would return this
  /// employee's anyway.
  Future<PagedList<TimeEntry>> fetchMine({
    int pageNumber = 1,
    int pageSize = 20,
    String? sortBy,
    bool sortDescending = true,
  }) {
    return getPaged(
      '/api/v1/timeentries',
      TimeEntry.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: sortBy,
        sortDescending: sortDescending,
      ),
    );
  }

  /// The running shift, or null when off shift.
  ///
  /// The endpoint answers 204 rather than 404 for "not clocked in", because
  /// being off shift is the ordinary state and not a missing resource. Dio
  /// hands that back as a null body.
  Future<TimeEntry?> fetchCurrent() {
    return guard(() async {
      final response =
          await dio.get<Map<String, dynamic>>('/api/v1/timeentries/current');

      final data = response.data;

      return data == null ? null : TimeEntry.fromJson(data);
    });
  }

  Future<TimeEntry> clockIn({
    String? projectId,
    String workType = 'Regular',
    String? note,
    double? latitude,
    double? longitude,
  }) {
    return postJson(
      '/api/v1/timeentries/clock-in',
      TimeEntry.fromJson,
      data: <String, dynamic>{
        'workType': workType,
        'projectId': ?projectId,
        'note': ?note,
        // Sent only as a pair: the API rejects half a position, and a
        // basement with no fix must still be able to start a shift.
        if (latitude != null && longitude != null) ...<String, dynamic>{
          'latitude': latitude,
          'longitude': longitude,
        },
      },
    );
  }

  Future<TimeEntry> clockOut({
    int breakMinutes = 0,
    String? note,
    double? latitude,
    double? longitude,
  }) {
    return postJson(
      '/api/v1/timeentries/clock-out',
      TimeEntry.fromJson,
      data: <String, dynamic>{
        'breakMinutes': breakMinutes,
        'note': ?note,
        if (latitude != null && longitude != null) ...<String, dynamic>{
          'latitude': latitude,
          'longitude': longitude,
        },
      },
    );
  }
}

final timeEntryRepositoryProvider = Provider<TimeEntryRepository>((ref) {
  return TimeEntryRepository(ref.watch(apiClientProvider));
});
