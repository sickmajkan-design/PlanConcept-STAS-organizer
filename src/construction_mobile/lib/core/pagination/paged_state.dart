import '../models/paged_list.dart';

/// Accumulated state of an infinitely scrolling list.
class PagedState<T> {
  const PagedState({
    required this.items,
    required this.totalCount,
    required this.lastLoadedPage,
    required this.hasMore,
    this.isLoadingMore = false,
    this.loadMoreError,
  });

  final List<T> items;
  final int totalCount;
  final int lastLoadedPage;
  final bool hasMore;

  /// True while an additional page is being appended, so the list can show a
  /// footer spinner without replacing the rows already on screen.
  final bool isLoadingMore;

  /// Set when appending a page failed; the list shows a retry footer and the
  /// rows already loaded stay usable.
  final String? loadMoreError;

  bool get isEmpty => items.isEmpty;

  factory PagedState.fromPage(PagedList<T> page) => PagedState<T>(
        items: page.items,
        totalCount: page.totalCount,
        lastLoadedPage: page.pageNumber,
        hasMore: page.hasNextPage,
      );

  PagedState<T> appending() => copyWith(isLoadingMore: true, clearError: true);

  PagedState<T> appended(PagedList<T> page) => PagedState<T>(
        items: [...items, ...page.items],
        totalCount: page.totalCount,
        lastLoadedPage: page.pageNumber,
        hasMore: page.hasNextPage,
      );

  PagedState<T> failedToAppend(String message) =>
      copyWith(isLoadingMore: false, loadMoreError: message);

  PagedState<T> copyWith({
    List<T>? items,
    int? totalCount,
    int? lastLoadedPage,
    bool? hasMore,
    bool? isLoadingMore,
    String? loadMoreError,
    bool clearError = false,
  }) {
    return PagedState<T>(
      items: items ?? this.items,
      totalCount: totalCount ?? this.totalCount,
      lastLoadedPage: lastLoadedPage ?? this.lastLoadedPage,
      hasMore: hasMore ?? this.hasMore,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      loadMoreError: clearError ? null : (loadMoreError ?? this.loadMoreError),
    );
  }
}
