import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../pagination/paged_state.dart';
import '../utils/formatting.dart';
import 'failure_view.dart';

/// Renders a [PagedState] with pull-to-refresh, infinite scroll and footer
/// states. Shared by every list screen so paging behaviour stays identical.
class PagedListView<T> extends StatefulWidget {
  const PagedListView({
    super.key,
    required this.state,
    required this.onRefresh,
    required this.onLoadMore,
    required this.itemBuilder,
    required this.emptyMessage,
    this.emptyIcon = Icons.inbox,
    this.header,
    this.padding = const EdgeInsets.fromLTRB(16, 8, 16, 24),
  });

  final AsyncValue<PagedState<T>> state;
  final Future<void> Function() onRefresh;
  final VoidCallback onLoadMore;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final String emptyMessage;
  final IconData emptyIcon;

  /// Optional pinned content above the rows (search field, filter chips).
  final Widget? header;
  final EdgeInsets padding;

  @override
  State<PagedListView<T>> createState() => _PagedListViewState<T>();
}

class _PagedListViewState<T> extends State<PagedListView<T>> {
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) {
      return;
    }

    final position = _scrollController.position;

    // Fetch the next page a little before the user reaches the bottom.
    if (position.pixels >= position.maxScrollExtent - 400) {
      widget.onLoadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.state;
    final data = state.value;

    // First load, nothing to show yet.
    if (data == null) {
      if (state.hasError) {
        return Column(
          children: [
            if (widget.header != null) widget.header!,
            Expanded(
              child: FailureView(
                error: state.error!,
                onRetry: widget.onRefresh,
              ),
            ),
          ],
        );
      }

      return Column(
        children: [
          if (widget.header != null) widget.header!,
          const Expanded(child: Center(child: CircularProgressIndicator())),
        ],
      );
    }

    return Column(
      children: [
        if (widget.header != null) widget.header!,
        // Thin progress line while a search or refresh reloads the list,
        // keeping the current rows on screen.
        SizedBox(
          height: 2,
          child: state.isLoading ? const LinearProgressIndicator() : null,
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: widget.onRefresh,
            child: data.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.5,
                        child: EmptyView(
                          message: widget.emptyMessage,
                          icon: widget.emptyIcon,
                        ),
                      ),
                    ],
                  )
                : ListView.separated(
                    controller: _scrollController,
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: widget.padding,
                    itemCount: data.items.length + 1,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (context, index) {
                      if (index < data.items.length) {
                        return widget.itemBuilder(context, data.items[index]);
                      }

                      return _Footer(
                        state: data,
                        onRetry: widget.onLoadMore,
                      );
                    },
                  ),
          ),
        ),
      ],
    );
  }
}

class _Footer<T> extends StatelessWidget {
  const _Footer({required this.state, required this.onRetry});

  final PagedState<T> state;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (state.loadMoreError != null) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 16),
        child: Column(
          children: [
            Text(
              state.loadMoreError!,
              textAlign: TextAlign.center,
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            OutlinedButton(
              onPressed: onRetry,
              child: const Text('Load more'),
            ),
          ],
        ),
      );
    }

    if (state.isLoadingMore) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: Center(
          child: SizedBox(
            width: 24,
            height: 24,
            child: CircularProgressIndicator(strokeWidth: 2.5),
          ),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.only(top: 16, bottom: 8),
      child: Center(
        child: Text(
          state.items.length >= state.totalCount
              ? '${state.totalCount} total'
              : '${state.items.length} of ${state.totalCount}',
          style: theme.textTheme.bodySmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ),
    );
  }
}

/// Search field plus optional filter chips, used as the [PagedListView]
/// header on list screens.
class ListSearchHeader extends StatelessWidget {
  const ListSearchHeader({
    super.key,
    required this.hintText,
    required this.onSearchChanged,
    this.filters = const [],
    this.selectedFilter,
    this.onFilterSelected,
  });

  final String hintText;
  final ValueChanged<String> onSearchChanged;
  final List<String> filters;
  final String? selectedFilter;
  final ValueChanged<String?>? onFilterSelected;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            onChanged: onSearchChanged,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: hintText,
              prefixIcon: const Icon(Icons.search),
              isDense: true,
            ),
          ),
          if (filters.isNotEmpty) ...[
            const SizedBox(height: 8),
            SizedBox(
              height: 40,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: filters.length,
                separatorBuilder: (_, _) => const SizedBox(width: 8),
                itemBuilder: (context, index) {
                  final filter = filters[index];
                  final selected = filter == selectedFilter;

                  return FilterChip(
                    label: Text(humanizeEnum(filter)),
                    selected: selected,
                    onSelected: (_) =>
                        onFilterSelected?.call(selected ? null : filter),
                  );
                },
              ),
            ),
          ],
        ],
      ),
    );
  }
}
