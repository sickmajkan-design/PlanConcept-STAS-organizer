import 'package:construction_mobile/core/models/paged_list.dart';
import 'package:construction_mobile/core/pagination/paged_state.dart';
import 'package:flutter_test/flutter_test.dart';

PagedList<String> _page(
  List<String> items, {
  int pageNumber = 1,
  int totalCount = 5,
  bool hasNextPage = true,
}) {
  return PagedList<String>(
    items: items,
    pageNumber: pageNumber,
    pageSize: 2,
    totalCount: totalCount,
    totalPages: 3,
    hasNextPage: hasNextPage,
    hasPreviousPage: pageNumber > 1,
  );
}

void main() {
  group('PagedList.fromJson', () {
    test('parses the API envelope', () {
      final page = PagedList<String>.fromJson(
        {
          'items': [
            {'name': 'first'},
            {'name': 'second'},
          ],
          'pageNumber': 2,
          'pageSize': 2,
          'totalCount': 5,
          'totalPages': 3,
          'hasNextPage': true,
          'hasPreviousPage': true,
        },
        (item) => item['name'] as String,
      );

      expect(page.items, ['first', 'second']);
      expect(page.pageNumber, 2);
      expect(page.totalCount, 5);
      expect(page.hasNextPage, isTrue);
    });

    test('tolerates an empty result', () {
      final page = PagedList<String>.fromJson(
        {'items': <dynamic>[], 'totalCount': 0},
        (item) => item['name'] as String,
      );

      expect(page.items, isEmpty);
      expect(page.totalCount, 0);
      expect(page.hasNextPage, isFalse);
    });
  });

  group('PagedState', () {
    test('starts from the first page', () {
      final state = PagedState<String>.fromPage(_page(['a', 'b']));

      expect(state.items, ['a', 'b']);
      expect(state.lastLoadedPage, 1);
      expect(state.hasMore, isTrue);
      expect(state.isLoadingMore, isFalse);
    });

    test('appends the next page without losing loaded rows', () {
      final state = PagedState<String>.fromPage(_page(['a', 'b']))
          .appending()
          .appended(_page(['c', 'd'], pageNumber: 2));

      expect(state.items, ['a', 'b', 'c', 'd']);
      expect(state.lastLoadedPage, 2);
      // Appending clears the in-flight marker.
      expect(state.isLoadingMore, isFalse);
    });

    test('marks the end of the list', () {
      final state = PagedState<String>.fromPage(_page(['a', 'b']))
          .appended(_page(['e'], pageNumber: 3, hasNextPage: false));

      expect(state.hasMore, isFalse);
    });

    test('keeps loaded rows when appending fails', () {
      final state = PagedState<String>.fromPage(_page(['a', 'b']))
          .appending()
          .failedToAppend('No connection to the server.');

      expect(state.items, ['a', 'b']);
      expect(state.isLoadingMore, isFalse);
      expect(state.loadMoreError, 'No connection to the server.');
      // The user can still retry, so the list is not marked as finished.
      expect(state.hasMore, isTrue);
    });

    test('clears a previous error when retrying', () {
      final state = PagedState<String>.fromPage(_page(['a', 'b']))
          .failedToAppend('boom')
          .appending();

      expect(state.loadMoreError, isNull);
      expect(state.isLoadingMore, isTrue);
    });
  });
}
