import 'package:flutter_riverpod/flutter_riverpod.dart';

/// When the data currently on screen was saved, or null while it is live.
///
/// One value for the whole app rather than one per screen. A phone either has
/// a signal or it does not, and a foreman who has been told once that he is
/// looking at this morning's copy does not need telling again on the next tab.
///
/// It holds the *oldest* moment served since the connection went, because that
/// is the honest one: a screen assembled from a list saved at 07:14 and a
/// count saved at 11:02 is only as current as its oldest part.
class OfflineDataNotifier extends Notifier<DateTime?> {
  @override
  DateTime? build() => null;

  /// A request was answered from the cache, with a copy stored at [savedAt].
  void servedFromCache(DateTime savedAt) {
    final current = state;

    if (current == null || savedAt.isBefore(current)) {
      state = savedAt;
    }
  }

  /// A request reached the server. Whatever the answer was, the phone is on
  /// the network and the notice no longer applies.
  void servedLive() {
    if (state != null) {
      state = null;
    }
  }
}

final offlineDataProvider = NotifierProvider<OfflineDataNotifier, DateTime?>(
  OfflineDataNotifier.new,
);
