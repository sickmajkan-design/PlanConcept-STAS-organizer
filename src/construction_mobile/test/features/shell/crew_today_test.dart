import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/employees/data/employee_repository.dart';
import 'package:construction_mobile/features/employees/data/models/employee.dart';
import 'package:construction_mobile/features/projects/data/models/project.dart';
import 'package:construction_mobile/features/projects/data/project_repository.dart';
import 'package:construction_mobile/features/shell/presentation/today_data.dart';
import 'package:construction_mobile/features/time_entries/data/models/time_entry.dart';
import 'package:construction_mobile/features/time_entries/data/time_entry_repository.dart';
import 'package:construction_mobile/features/work_items/data/models/work_item.dart';
import 'package:construction_mobile/features/work_items/data/work_item_repository.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

PagedList<T> _page<T>(List<T> items) => PagedList<T>(
      items: items,
      pageNumber: 1,
      pageSize: 100,
      totalCount: items.length,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    );

Employee _person(String id, String name) => Employee(
      id: id,
      employeeNumber: id,
      firstName: name,
      lastName: 'X',
      fullName: '$name X',
      employmentDate: DateTime(2024),
      position: 'Zidar',
      status: 'Active',
      createdAt: DateTime(2024),
    );

TimeEntry _shift(String employeeId, {bool running = true, bool? atSite}) =>
    TimeEntry(
      id: 's-$employeeId',
      employeeId: employeeId,
      employeeName: employeeId,
      startedAt: DateTime.now().toUtc(),
      endedAt: running ? null : DateTime.now().toUtc(),
      breakMinutes: 0,
      workType: 'Regular',
      status: 'Pending',
      createdAt: DateTime.now().toUtc(),
      locationCorrect: atSite,
    );

class _Employees implements EmployeeRepository {
  _Employees(this.people);

  final List<Employee> people;

  @override
  Future<PagedList<Employee>> fetchEmployees({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? projectId,
    String? sortBy,
    bool sortDescending = false,
  }) async =>
      _page(people);

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Time implements TimeEntryRepository {
  _Time(this.entries);

  final List<TimeEntry> entries;

  @override
  Future<List<TimeEntry>> fetchTeamToday() async => entries;

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Projects implements ProjectRepository {
  @override
  Future<PagedList<Project>> fetchProjects({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? employeeId,
    String? sortBy,
    bool sortDescending = false,
  }) async =>
      _page([
        Project(
          id: 'p1',
          name: 'Zgrada A',
          status: 'Active',
          createdAt: DateTime(2024),
        ),
      ]);

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Work implements WorkItemRepository {
  _Work(this.items);

  final List<WorkItem> items;

  @override
  Future<PagedList<WorkItem>> fetchMine({
    String? assignedEmployeeId,
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
    bool openOnly = true,
  }) async =>
      _page(items);

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

WorkItem _item(String kind, String title, DateTime createdAt) => WorkItem(
      id: title,
      kind: kind,
      title: title,
      priority: 'Normal',
      status: 'Open',
      createdAt: createdAt,
    );

const _marko = User(
  id: 'u-marko',
  email: 'marko@x',
  role: 'Foreman',
  employeeId: 'marko',
);

ProviderContainer _container({
  required List<Employee> people,
  required List<TimeEntry> entries,
  List<WorkItem> work = const [],
  int hour = 9,
  User user = _marko,
}) {
  return ProviderContainer(
    overrides: [
      currentUserProvider.overrideWithValue(user),
      employeeRepositoryProvider.overrideWithValue(_Employees(people)),
      timeEntryRepositoryProvider.overrideWithValue(_Time(entries)),
      projectRepositoryProvider.overrideWithValue(_Projects()),
      workItemRepositoryProvider.overrideWithValue(_Work(work)),
      crewClockProvider.overrideWithValue(() => DateTime.now().copyWith(hour: hour)),
    ],
  );
}

void main() {
  final crew = [
    _person('marko', 'Marko'),
    _person('ana', 'Ana'),
    _person('ivan', 'Ivan'),
    _person('luka', 'Luka'),
  ];

  test('counts who is on site, who has not clocked in, and who is away from it',
      () async {
    final container = _container(
      people: crew,
      entries: [
        _shift('marko'),
        _shift('ana', atSite: false),
        _shift('luka', running: false),
      ],
    );
    addTearDown(container.dispose);

    final today = await container.read(crewTodayProvider.future);

    expect(today.crewCount, 4);
    expect(today.onSiteCount, 2, reason: 'Luka finished, so he is not on site');
    expect(today.outsideSiteCount, 1);
    expect(today.notClockedIn, ['Ivan X']);
    expect(today.siteNames, ['Zgrada A']);
  });

  test('the person looking is not listed as missing', () async {
    final container = _container(people: crew, entries: const []);
    addTearDown(container.dispose);

    final today = await container.read(crewTodayProvider.future);

    expect(today.notClockedIn, isNot(contains('Marko X')));
    expect(today.notClockedIn, hasLength(3));
  });

  test('nobody is missing before the working day has started', () async {
    final container = _container(people: crew, entries: const [], hour: 6);
    addTearDown(container.dispose);

    final today = await container.read(crewTodayProvider.future);

    expect(today.notClockedIn, isEmpty);
    expect(today.attention, isEmpty);
  });

  test('a recent defect is listed, an old one and a task are not', () async {
    final container = _container(
      people: crew,
      entries: [for (final p in crew) _shift(p.id)],
      work: [
        _item('Defect', 'Fresh crack', DateTime.now().toUtc()),
        _item(
          'Defect',
          'Old crack',
          DateTime.now().toUtc().subtract(const Duration(days: 30)),
        ),
        _item('Task', 'Sweep', DateTime.now().toUtc()),
      ],
    );
    addTearDown(container.dispose);

    final today = await container.read(crewTodayProvider.future);

    expect(today.attention.map((a) => a.title), ['Fresh crack']);
  });

  test('a worker gets nothing: they oversee nobody', () async {
    final container = _container(
      people: crew,
      entries: const [],
      user: const User(id: 'u', email: 'w@x', role: 'Worker', employeeId: 'ivan'),
    );
    addTearDown(container.dispose);

    final today = await container.read(crewTodayProvider.future);

    expect(today.crewCount, 0);
    expect(today.attention, isEmpty);
  });
}
