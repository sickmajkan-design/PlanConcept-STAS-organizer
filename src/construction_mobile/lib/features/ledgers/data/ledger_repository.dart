import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/ledger.dart';

/// Read-only access to the SuperAdmin's free-form "Evidencija" ledger.
///
/// Mobile offers a view only — the full spreadsheet editor (columns,
/// reordering, colors, promotions) stays a desktop-only workflow; a phone
/// screen is the wrong shape for editing a wide free-form grid. Everything
/// here mirrors a GET the desktop admin panel already uses.
class LedgerRepository extends ApiRepository {
  const LedgerRepository(super.dio);

  Future<PagedList<LedgerSummary>> fetchLedgers({
    int pageNumber = 1,
    int pageSize = 20,
    String? search,
  }) {
    return getPaged(
      '/api/v1/ledgers',
      LedgerSummary.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        search: search,
        sortBy: 'year',
        sortDescending: true,
      ),
    );
  }

  /// The ledger shell: columns and section headers with row counts, but no
  /// rows or cells — a real month can have dozens of sections and hundreds
  /// of rows, so those are fetched per-section only once opened.
  Future<LedgerDetail> fetchLedger(String id) {
    return getJson('/api/v1/ledgers/$id', LedgerDetail.fromJson);
  }

  Future<LedgerSection> fetchSectionRows(String ledgerId, String sectionId) {
    return getJson(
      '/api/v1/ledgers/$ledgerId/sections/$sectionId/rows',
      LedgerSection.fromJson,
    );
  }

  Future<LedgerSummaryPanel> fetchSummary(String ledgerId) {
    return getJson(
      '/api/v1/ledgers/$ledgerId/summary',
      LedgerSummaryPanel.fromJson,
    );
  }
}

final ledgerRepositoryProvider = Provider<LedgerRepository>((ref) {
  return LedgerRepository(ref.watch(apiClientProvider));
});
