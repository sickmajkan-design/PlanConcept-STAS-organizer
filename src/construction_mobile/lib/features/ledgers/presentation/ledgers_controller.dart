import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/pagination/paged_list_notifier.dart';
import '../../../core/pagination/paged_state.dart';
import '../data/ledger_repository.dart';
import '../data/models/ledger.dart';

class LedgersController extends PagedListNotifier<LedgerSummary> {
  @override
  Future<PagedList<LedgerSummary>> loadPage({
    required int pageNumber,
    required String search,
  }) {
    return ref.read(ledgerRepositoryProvider).fetchLedgers(
          pageNumber: pageNumber,
          pageSize: PagedListNotifier.pageSize,
          search: search,
        );
  }
}

final ledgersControllerProvider =
    AsyncNotifierProvider<LedgersController, PagedState<LedgerSummary>>(
  LedgersController.new,
);

final ledgerDetailProvider =
    FutureProvider.autoDispose.family<LedgerDetail, String>((ref, id) {
  return ref.watch(ledgerRepositoryProvider).fetchLedger(id);
});

final ledgerSummaryPanelProvider =
    FutureProvider.autoDispose.family<LedgerSummaryPanel, String>((ref, ledgerId) {
  return ref.watch(ledgerRepositoryProvider).fetchSummary(ledgerId);
});

/// A section's rows and cells, fetched only once it is opened.
final ledgerSectionRowsProvider = FutureProvider.autoDispose
    .family<LedgerSection, ({String ledgerId, String sectionId})>((ref, key) {
  return ref
      .read(ledgerRepositoryProvider)
      .fetchSectionRows(key.ledgerId, key.sectionId);
});
