import 'package:dio/dio.dart';
import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/features/absences/data/absence_repository.dart';
import 'package:construction_mobile/features/absences/data/models/absence.dart';
import 'package:construction_mobile/features/absences/presentation/my_absences_controller.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/time_entries/data/models/time_entry.dart';
import 'package:construction_mobile/features/time_entries/data/time_entry_repository.dart';
import 'package:construction_mobile/features/time_entries/presentation/my_time_entries_controller.dart';
import 'package:construction_mobile/features/work_items/data/models/work_item.dart';
import 'package:construction_mobile/features/work_items/data/work_item_repository.dart';
import 'package:construction_mobile/features/work_items/presentation/my_work_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// "My" lists name the employee.
///
/// The API narrows only a Worker to their own rows. A foreman or a manager who asks for the same
/// list gets the whole crew's, and the phone showed those as "my working time", "my leave" and
/// "my tasks" — with no name on the cards to tell whose they were. Each list now says whose it is
/// after; these tests hold the request to that, for the roles the server does not narrow.
const _foreman = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'predradnik@construction.local',
  role: 'Foreman',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
  firstName: 'Petar',
  lastName: 'Predradnik',
);

const _worker = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d1',
  email: 'radnik@construction.local',
  role: 'Worker',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61165',
  firstName: 'Rade',
  lastName: 'Radnik',
);

class _SignedIn extends Notifier<User?> {
  @override
  User? build() => _foreman;

  void set(User? user) => state = user;
}

PagedList<T> _empty<T>() => PagedList<T>(
      items: const [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    );

class _Times extends TimeEntryRepository {
  _Times() : super(Dio());

  String? asked;

  @override
  Future<PagedList<TimeEntry>> fetchMine({
    String? employeeId,
    int pageNumber = 1,
    int pageSize = 20,
    String? sortBy,
    bool sortDescending = true,
  }) async {
    asked = employeeId;
    return _empty<TimeEntry>();
  }
}

class _Absences extends AbsenceRepository {
  _Absences() : super(Dio());

  String? asked;

  @override
  Future<PagedList<Absence>> fetchMine({
    String? employeeId,
    int pageNumber = 1,
    int pageSize = 20,
    String? status,
  }) async {
    asked = employeeId;
    return _empty<Absence>();
  }
}

class _Work extends WorkItemRepository {
  _Work() : super(Dio());

  String? asked;

  @override
  Future<PagedList<WorkItem>> fetchMine({
    String? assignedEmployeeId,
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool openOnly = true,
  }) async {
    asked = assignedEmployeeId;
    return _empty<WorkItem>();
  }
}

void main() {
  test("a foreman's working time, leave and tasks are asked for as their own", () async {
    final times = _Times();
    final absences = _Absences();
    final work = _Work();

    final container = ProviderContainer(
      overrides: [
        currentUserProvider.overrideWithValue(_foreman),
        timeEntryRepositoryProvider.overrideWithValue(times),
        absenceRepositoryProvider.overrideWithValue(absences),
        workItemRepositoryProvider.overrideWithValue(work),
      ],
    );
    addTearDown(container.dispose);

    await container.read(myTimeEntriesControllerProvider.future);
    await container.read(myAbsencesControllerProvider.future);
    await container.read(myWorkControllerProvider.future);

    expect(times.asked, _foreman.employeeId, reason: 'without it a foreman sees the crew as "my time"');
    expect(absences.asked, _foreman.employeeId);
    expect(work.asked, _foreman.employeeId);
  });

  test("the next person to sign in is not shown the previous one's list", () async {
    // The list providers live as long as the app. Without a dependency on who is signed in, a
    // worker signing in after a foreman on the same phone was shown the foreman's list, and no
    // request was made for them at all.
    final times = _Times();
    final signedIn = NotifierProvider<_SignedIn, User?>(_SignedIn.new);

    final container = ProviderContainer(
      overrides: [
        currentUserProvider.overrideWith((ref) => ref.watch(signedIn)),
        timeEntryRepositoryProvider.overrideWithValue(times),
      ],
    );
    addTearDown(container.dispose);

    await container.read(myTimeEntriesControllerProvider.future);
    expect(times.asked, _foreman.employeeId);

    container.read(signedIn.notifier).set(_worker);
    await container.read(myTimeEntriesControllerProvider.future);

    expect(
      times.asked,
      _worker.employeeId,
      reason: 'the list is asked for again, under the new person',
    );
  });
}
