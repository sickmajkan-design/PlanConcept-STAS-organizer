import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/paged_list.dart';
import '../network/api_exception.dart';
import 'paged_state.dart';

/// Shared behaviour for every searchable, infinitely scrolling list in the
/// app: debounced search, page appending, and refresh. Subclasses only supply
/// [loadPage].
abstract class PagedListNotifier<T> extends AsyncNotifier<PagedState<T>> {
  static const int pageSize = 20;
  static const Duration searchDebounce = Duration(milliseconds: 350);

  Timer? _debounceTimer;
  String _searchTerm = '';

  /// The search term currently applied to the loaded results.
  String get searchTerm => _searchTerm;

  /// Fetches one page from the API. Implemented per feature.
  Future<PagedList<T>> loadPage({
    required int pageNumber,
    required String search,
  });

  @override
  Future<PagedState<T>> build() async {
    ref.onDispose(() => _debounceTimer?.cancel());

    final page = await loadPage(pageNumber: 1, search: _searchTerm);
    return PagedState<T>.fromPage(page);
  }

  /// Applies a search term after a short pause, so a fast typist triggers one
  /// request instead of one per keystroke.
  void search(String term) {
    final trimmed = term.trim();

    _debounceTimer?.cancel();
    _debounceTimer = Timer(searchDebounce, () {
      if (trimmed == _searchTerm) {
        return;
      }

      _searchTerm = trimmed;
      // Riverpod keeps the previous rows visible while the reload runs.
      ref.invalidateSelf();
    });
  }

  /// Reloads from page 1, keeping the current search term.
  Future<void> refresh() async {
    _debounceTimer?.cancel();
    ref.invalidateSelf();
    await future;
  }

  /// Appends the next page. Safe to call repeatedly from a scroll listener —
  /// it ignores calls while a page is already in flight or none is left.
  Future<void> loadMore() async {
    final current = state.value;

    if (current == null ||
        state.isLoading ||
        current.isLoadingMore ||
        !current.hasMore) {
      return;
    }

    state = AsyncData(current.appending());

    try {
      final page = await loadPage(
        pageNumber: current.lastLoadedPage + 1,
        search: _searchTerm,
      );

      state = AsyncData((state.value ?? current).appended(page));
    } on ApiException catch (exception) {
      state = AsyncData((state.value ?? current).failedToAppend(exception.message));
    }
  }
}
