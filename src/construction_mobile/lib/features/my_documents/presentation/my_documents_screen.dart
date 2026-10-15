import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_locales.dart';
import '../../../core/l10n/enum_labels.dart';
import '../../../core/utils/formatting.dart';
import '../../../core/widgets/auto_refresh.dart';
import '../../../core/widgets/failure_view.dart';
import '../../attachments/data/models/attachment.dart';
import '../../attachments/presentation/attachment_preview_screen.dart';
import '../../document_requests/data/document_request_repository.dart';
import '../../document_requests/presentation/document_request_screen.dart';
import '../data/my_documents_repository.dart';

/// "My documents": what the office shared with this person, and the vehicle they drive.
///
/// Read only. Nothing here is uploaded from the phone yet; requests for a missing document come
/// in a later step.
class MyDocumentsScreen extends ConsumerWidget {
  const MyDocumentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = context.l10n;
    final state = ref.watch(myDocumentsProvider);
    final requests =
        ref.watch(myDocumentRequestsProvider).value ??
        const <DocumentRequest>[];

    Future<void> refresh() async {
      ref.invalidate(myDocumentsProvider);
      ref.invalidate(myDocumentRequestsProvider);
      await ref.read(myDocumentsProvider.future);
    }

    return Scaffold(
      appBar: AppBar(title: Text(l10n.myDocumentsTitle)),
      body: SafeArea(
        child: AutoRefresh(
          onRefresh: () {
            ref.invalidate(myDocumentsProvider);
            ref.invalidate(myDocumentRequestsProvider);
          },
          child: RefreshIndicator(
            onRefresh: refresh,
            child: state.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ListView(
                children: [
                  SizedBox(
                    height: MediaQuery.of(context).size.height * 0.7,
                    child: FailureView(error: error, onRetry: refresh),
                  ),
                ],
              ),
              data: (data) => data.isEmpty && requests.isEmpty
                  ? ListView(
                      children: [
                        SizedBox(
                          height: MediaQuery.of(context).size.height * 0.7,
                          child: EmptyView(
                            message: l10n.myDocumentsEmpty,
                            icon: Icons.folder_open_outlined,
                          ),
                        ),
                      ],
                    )
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        if (requests.isNotEmpty) ...[
                          _SectionTitle(l10n.requestsSection),
                          for (final request in requests)
                            _RequestTile(request: request),
                          const SizedBox(height: 8),
                        ],
                        for (final vehicle in data.vehicles) ...[
                          _VehicleCard(vehicle: vehicle),
                          const SizedBox(height: 16),
                        ],
                        if (data.documents.isNotEmpty) ...[
                          _SectionTitle(l10n.myDocumentsPersonal),
                          for (final document in data.documents)
                            _DocumentTile(document: document),
                        ],
                      ],
                    ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(text, style: Theme.of(context).textTheme.titleSmall),
    );
  }
}

bool _isLapsed(DateTime? date) {
  if (date == null) {
    return false;
  }

  final today = DateTime.now();

  return DateTime(
    date.year,
    date.month,
    date.day,
  ).isBefore(DateTime(today.year, today.month, today.day));
}

/// How close a date is, as a colour: red once lapsed, amber inside 30 days.
Color? _dateColor(BuildContext context, DateTime? date) {
  if (date == null) {
    return null;
  }

  final today = DateTime.now();
  final days = DateTime(
    date.year,
    date.month,
    date.day,
  ).difference(DateTime(today.year, today.month, today.day)).inDays;

  if (days < 0) {
    return Theme.of(context).colorScheme.error;
  }

  if (days <= 30) {
    return Colors.amber.shade800;
  }

  return null;
}

class _DocumentTile extends StatelessWidget {
  const _DocumentTile({required this.document, this.flat = false});

  final Attachment document;

  /// Inside another card the tile is a plain row, not a card in a card.
  final bool flat;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final expiry = document.expiryDate;

    final tile = ListTile(
      contentPadding: flat ? EdgeInsets.zero : null,
      leading: Icon(
        document.isImage ? Icons.image_outlined : Icons.description_outlined,
      ),
      title: Text(
        (document.title?.isNotEmpty ?? false)
            ? document.title!
            : document.fileName,
      ),
      subtitle: Text(
        [
          enumLabel(l10n, EnumKind.attachmentCategory, document.category),
          if (expiry != null)
            _isLapsed(expiry)
                ? l10n.myDocumentsExpiredOn(formatDate(expiry))
                : l10n.myDocumentsValidUntil(formatDate(expiry)),
        ].join(' · '),
        style: TextStyle(color: _dateColor(context, expiry)),
      ),
      trailing: const Icon(Icons.chevron_right),
      onTap: () => Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => AttachmentPreviewScreen(attachment: document),
        ),
      ),
    );

    return flat
        ? tile
        : Card(margin: const EdgeInsets.only(bottom: 8), child: tile);
  }
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.vehicle});

  final MyVehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    Widget dateRow(String label, DateTime? date) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(child: Text(label)),
          Text(
            date == null ? '—' : formatDate(date),
            style: TextStyle(
              fontWeight: FontWeight.w600,
              color: _dateColor(context, date),
            ),
          ),
        ],
      ),
    );

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.directions_car_outlined),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${vehicle.name} · ${vehicle.registrationNumber}',
                    style: theme.textTheme.titleMedium,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(l10n.myDocumentsMyVehicle, style: theme.textTheme.bodySmall),
            const Divider(height: 24),
            dateRow(
              l10n.myDocumentsRegistration,
              vehicle.registrationValidUntil,
            ),
            dateRow(
              l10n.myDocumentsInspection,
              vehicle.technicalInspectionValidUntil,
            ),
            dateRow(l10n.myDocumentsInsurance, vehicle.insuranceValidUntil),
            if (vehicle.tolls.isNotEmpty) ...[
              const Divider(height: 24),
              Text(l10n.myDocumentsTolls, style: theme.textTheme.titleSmall),
              const SizedBox(height: 4),
              for (final toll in vehicle.tolls) _TollRow(toll: toll),
            ],
            if (vehicle.documents.isNotEmpty) ...[
              const Divider(height: 24),
              Text(
                l10n.myDocumentsVehicleDocuments,
                style: theme.textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              for (final document in vehicle.documents)
                _DocumentTile(document: document, flat: true),
            ],
          ],
        ),
      ),
    );
  }
}

class _TollRow extends StatelessWidget {
  const _TollRow({required this.toll});

  final MyVehicleToll toll;

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);

    final kind = switch (toll.type) {
      'Tunnel' => l10n.myDocumentsTollTunnel,
      'Passage' => l10n.myDocumentsTollPassage,
      _ => l10n.myDocumentsTollVignette,
    };

    final place = [
      toll.country,
      if (toll.routeSegment?.isNotEmpty ?? false) toll.routeSegment!,
    ].join(' · ');

    final status = !toll.paid
        ? l10n.myDocumentsTollUnpaid
        : toll.validUntil == null
        ? l10n.myDocumentsTollPaid
        : _isLapsed(toll.validUntil)
        ? l10n.myDocumentsExpiredOn(formatDate(toll.validUntil))
        : l10n.myDocumentsValidUntil(formatDate(toll.validUntil));

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(child: Text('$kind · $place')),
          Text(
            status,
            style: TextStyle(
              fontWeight: FontWeight.w600,
              color: !toll.paid
                  ? theme.colorScheme.error
                  : _dateColor(context, toll.validUntil),
            ),
          ),
        ],
      ),
    );
  }
}

class _RequestTile extends StatelessWidget {
  const _RequestTile({required this.request});

  final DocumentRequest request;

  @override
  Widget build(BuildContext context) {
    final style = requestStatusStyle(context, request);

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(style.icon, color: style.color),
        title: Text(request.title),
        subtitle: Text(
          [
            style.label,
            if (request.dueDate != null && request.canBeAnswered)
              context.l10n.requestDue(formatDate(request.dueDate)),
          ].join(' · '),
          style: TextStyle(color: style.color),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute<void>(
            builder: (_) => DocumentRequestScreen(requestId: request.id),
          ),
        ),
      ),
    );
  }
}
