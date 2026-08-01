import 'paged_list_notifier.dart';

/// A [PagedListNotifier] with one filter applied alongside the search term.
///
/// Every list in the app offers the same interaction: a row of chips where at
/// most one is selected, and selecting one reloads from page 1. Only the set
/// of chips differs — statuses for employees, projects, tools and vehicles, a
/// single toggle for materials — so the state and the reload live here and
/// subclasses supply only [loadPage].
///
/// The filter is held as a nullable [String] because that is what the chip row
/// ([ListSearchHeader]) hands back: the selected label, or null for "all".
/// Subclasses translate it into whatever the repository expects.
abstract class FilteredPagedListNotifier<T> extends PagedListNotifier<T> {
  String? _filter;

  /// The selected chip, or null when no filter is applied.
  String? get filter => _filter;

  /// Selects [filter] and reloads. Re-selecting the current value is ignored,
  /// so tapping the same chip twice does not fire a redundant request.
  void applyFilter(String? filter) {
    if (_filter == filter) {
      return;
    }

    _filter = filter;
    ref.invalidateSelf();
  }
}
