import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:construction_mobile/core/outbox/outbox_queue.dart';
import 'package:construction_mobile/features/absences/data/absence_repository.dart';
import 'package:construction_mobile/features/absences/data/models/absence.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/outbox/outbox_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _MemoryStore implements OutboxStore {
  String? value;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async => value = written;

  @override
  Future<void> clear() async => value = null;
}

class _FakeAbsences implements AbsenceRepository {
  ApiException? failWith;

  final List<String?> keys = <String?>[];

  @override
  Future<Absence> request({
    required String type,
    required DateTime startDate,
    required DateTime endDate,
    String? reason,
    String? idempotencyKey,
  }) async {
    keys.add(idempotencyKey);

    if (failWith != null) {
      throw failWith!;
    }

    return Absence.fromJson(<String, dynamic>{
      'id': '019fae10-0000-7000-8000-000000000009',
      'employeeId': 'e',
      'employeeName': 'Ana',
      'type': type,
      'status': 'Requested',
      'startDate': startDate.toIso8601String(),
      'dayCount': 3,
      'endDate': endDate.toIso8601String(),
      'createdAt': DateTime.now().toUtc().toIso8601String(),
    });
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

const _ana = User(id: 'ana', email: 'ana@x', role: 'Worker', employeeId: 'e');

Map<String, dynamic> _leave() => <String, dynamic>{
      'type': 'AnnualLeave',
      'startDate': '2030-01-10T00:00:00.000',
      'endDate': '2030-01-12T00:00:00.000',
      'reason': null,
    };

void main() {
  late _FakeAbsences absences;
  late _MemoryStore store;
  late ProviderContainer container;

  setUp(() {
    absences = _FakeAbsences();
    store = _MemoryStore();

    container = ProviderContainer(
      overrides: [
        currentUserProvider.overrideWithValue(_ana),
        absenceRepositoryProvider.overrideWithValue(absences),
        outboxQueueProvider.overrideWithValue(OutboxQueue(store)),
      ],
    );
  });

  tearDown(() => container.dispose());

  OutboxController controller() =>
      container.read(outboxControllerProvider.notifier);

  test('with signal it is sent at once and nothing is kept', () async {
    final result = await controller().submit(OutboxKind.absenceRequest, _leave());

    expect(result.queued, isFalse);
    expect(store.value, isNull);
    expect(absences.keys, hasLength(1));
  });

  test('with no signal it is kept, counted, and sent later under the same key',
      () async {
    absences.failWith = ApiException('offline', kind: ApiFailureKind.offline);

    final result = await controller().submit(OutboxKind.absenceRequest, _leave());

    expect(result.queued, isTrue);
    expect(container.read(outboxControllerProvider).pendingCount, 1);

    absences.failWith = null;
    await controller().flush();

    expect(container.read(outboxControllerProvider).pendingCount, 0);
    expect(absences.keys, hasLength(2));
    expect(absences.keys.last, absences.keys.first);
  });

  test('a refusal while the person is looking is thrown, not queued', () async {
    absences.failWith = ApiException(
      'overlaps',
      statusCode: 409,
      kind: ApiFailureKind.conflict,
    );

    await expectLater(
      controller().submit(OutboxKind.absenceRequest, _leave()),
      throwsA(isA<ApiException>()),
    );

    expect(store.value, isNull);
  });

  test('a refusal that arrives later is dropped and surfaced once', () async {
    absences.failWith = ApiException('offline', kind: ApiFailureKind.offline);
    await controller().submit(OutboxKind.absenceRequest, _leave());

    // Signal returns, and by then the office's answer is no.
    absences.failWith = ApiException(
      'overlaps existing leave',
      statusCode: 409,
      kind: ApiFailureKind.conflict,
    );

    await controller().flush();

    final state = container.read(outboxControllerProvider);

    expect(state.pendingCount, 0, reason: 'retrying a certain refusal forever');
    expect(state.refusal!.kind, OutboxKind.absenceRequest);
    expect(state.refusal!.failure.statusCode, 409);

    controller().refusalShown();

    expect(container.read(outboxControllerProvider).refusal, isNull);
  });

  test('still no signal keeps it and does not lose the count', () async {
    absences.failWith = ApiException('offline', kind: ApiFailureKind.offline);
    await controller().submit(OutboxKind.absenceRequest, _leave());

    await controller().flush();

    expect(container.read(outboxControllerProvider).pendingCount, 1);
    expect(store.value, isNotNull);
  });
}
