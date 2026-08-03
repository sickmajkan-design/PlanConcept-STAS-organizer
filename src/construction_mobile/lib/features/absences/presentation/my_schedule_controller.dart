import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/absence_repository.dart';
import '../data/models/schedule.dart';

/// How far ahead the phone looks. Two weeks is what fits on the screen and
/// what a worker is actually asking about; the board on the admin panel is
/// where a quarter gets planned.
const scheduleWindowDays = 14;

/// Where the signed-in employee is posted, and when they are away.
class MyScheduleController extends AsyncNotifier<Schedule> {
  @override
  Future<Schedule> build() {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    return ref.read(absenceRepositoryProvider).fetchSchedule(
          from: today,
          to: today.add(const Duration(days: scheduleWindowDays - 1)),
        );
  }

  Future<void> refresh() async {
    ref.invalidateSelf();
    await future;
  }
}

final myScheduleControllerProvider =
    AsyncNotifierProvider<MyScheduleController, Schedule>(
  MyScheduleController.new,
);
