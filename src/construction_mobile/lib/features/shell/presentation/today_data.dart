import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/presentation/auth_controller.dart';
import '../../employees/data/employee_repository.dart';
import '../../projects/data/project_repository.dart';
import '../../time_entries/data/time_entry_repository.dart';
import '../../work_items/data/work_item_repository.dart';

enum AttentionKind { notClockedIn, defect }

/// Something on the crew home screen that wants a look.
class AttentionItem {
  const AttentionItem({
    required this.kind,
    required this.title,
    this.projectId,
    this.reportedAt,
  });

  final AttentionKind kind;

  /// The person's name, or the defect's title.
  final String title;

  /// The site a defect is on; where a tap goes.
  final String? projectId;

  /// When a defect was reported.
  final DateTime? reportedAt;
}

/// How the crew stands today, for a foreman or anyone above.
///
/// Worked out on the phone from three lists the API already serves — the
/// people, today's shifts, and open work — because each of them is already
/// narrowed to the caller's own sites. A separate summary endpoint would have
/// to repeat that narrowing and would drift from it.
class CrewToday {
  const CrewToday({
    required this.crewCount,
    required this.onSiteCount,
    required this.notClockedIn,
    required this.outsideSiteCount,
    required this.siteNames,
    required this.attention,
  });

  final int crewCount;
  final int onSiteCount;

  /// Names of the people with no shift recorded today.
  final List<String> notClockedIn;

  /// Shifts running now that were started away from the site.
  final int outsideSiteCount;

  final List<String> siteNames;
  final List<AttentionItem> attention;
}

/// Before this hour nobody is late, so nobody is listed as missing. There is
/// no shift schedule to compare against; a fixed morning is the honest limit.
const crewMissingFromHour = 7;

/// The moment "today" is judged at; a provider so a test can pick the hour.
final crewClockProvider = Provider<DateTime Function()>((ref) => DateTime.now);

final crewTodayProvider = FutureProvider.autoDispose<CrewToday>((ref) async {
  final user = ref.watch(currentUserProvider);
  final now = ref.read(crewClockProvider)();

  // Nothing to show for a person who does not oversee anybody.
  if (user == null || !user.canViewDirectory) {
    return const CrewToday(
      crewCount: 0,
      onSiteCount: 0,
      notClockedIn: [],
      outsideSiteCount: 0,
      siteNames: [],
      attention: [],
    );
  }

  final employees = await ref
      .read(employeeRepositoryProvider)
      .fetchEmployees(pageSize: 100, status: 'Active');
  final entries = await ref.read(timeEntryRepositoryProvider).fetchTeamToday();

  // Each of the two below is a nicety; the card stands without it.
  var siteNames = <String>[];
  try {
    final projects = await ref
        .read(projectRepositoryProvider)
        .fetchProjects(pageSize: 20, status: 'Active');
    siteNames = projects.items.map((p) => p.name).toList();
  } catch (_) {}

  var defects = <AttentionItem>[];
  try {
    final work = await ref.read(workItemRepositoryProvider).fetchMine(pageSize: 20);
    final since = now.toUtc().subtract(const Duration(days: 3));

    defects = work.items
        .where((w) => w.kind == 'Defect' && w.createdAt.toUtc().isAfter(since))
        .map((w) => AttentionItem(
              kind: AttentionKind.defect,
              title: w.title,
              projectId: w.projectId,
              reportedAt: w.createdAt,
            ))
        .toList()
      ..sort((a, b) => b.reportedAt!.compareTo(a.reportedAt!));
  } catch (_) {}

  final crew = employees.items;
  final hasShiftToday = entries.map((e) => e.employeeId).toSet();
  final running = entries.where((e) => e.isRunning).toList();
  final runningIds = running.map((e) => e.employeeId).toSet();
  final crewIds = crew.map((e) => e.id).toSet();

  final missing = now.hour < crewMissingFromHour
      ? <String>[]
      : crew
          // Not the person looking: they know whether they have clocked in.
          .where((e) => e.id != user.employeeId && !hasShiftToday.contains(e.id))
          .map((e) => e.fullName)
          .toList();

  return CrewToday(
    crewCount: crew.length,
    onSiteCount: runningIds.intersection(crewIds).length,
    notClockedIn: missing,
    outsideSiteCount: running.where((e) => e.locationCorrect == false).length,
    siteNames: siteNames,
    attention: [
      for (final name in missing.take(3))
        AttentionItem(kind: AttentionKind.notClockedIn, title: name),
      ...defects.take(3),
    ],
  );
});
