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

  /// Starts a shift.
  ///
  /// [occurredAt] is for a shift that began with no signal: the handset's own
  /// moment, sent when the network comes back, because the server's clock read
  /// on arrival would say whenever the phone found a bar rather than when the
  /// worker started. [idempotencyKey] rides along with it so a reply lost on
  /// the way back cannot open the shift twice.
  Future<TimeEntry> clockIn({
    String? projectId,
    String workType = 'Regular',
    String? note,
    double? latitude,
    double? longitude,
    DateTime? occurredAt,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/timeentries/clock-in',
      TimeEntry.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'workType': workType,
        'projectId': ?projectId,
        'note': ?note,
        'occurredAt': ?occurredAt?.toUtc().toIso8601String(),
        // Sent only as a pair: the API rejects half a position, and a
        // basement with no fix must still be able to start a shift.
        if (latitude != null && longitude != null) ...<String, dynamic>{
          'latitude': latitude,
          'longitude': longitude,
        },
      },
    );
  }

  /// Ends the running shift. See [clockIn] for [occurredAt] and
  /// [idempotencyKey] — the end of a shift is the more common of the two to
  /// happen without signal, because that is when people are furthest inside a
  /// building and in the most hurry to leave.
  Future<TimeEntry> clockOut({
    int breakMinutes = 0,
    String? note,
    double? latitude,
    double? longitude,
    DateTime? occurredAt,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/timeentries/clock-out',
      TimeEntry.fromJson,
      idempotencyKey: idempotencyKey,
      data: <String, dynamic>{
        'breakMinutes': breakMinutes,
        'note': ?note,
        'occurredAt': ?occurredAt?.toUtc().toIso8601String(),
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
