import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import '../../auth/presentation/auth_controller.dart';

/// A file the person sent in answer to a request.
class RequestFile {
  const RequestFile({
    required this.id,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
  });

  factory RequestFile.fromJson(Map<String, dynamic> json) => RequestFile(
    id: json['id'] as String,
    fileName: json['fileName'] as String? ?? '',
    contentType: json['contentType'] as String? ?? '',
    sizeBytes: (json['sizeBytes'] as num?)?.toInt() ?? 0,
  );

  final String id;
  final String fileName;
  final String contentType;
  final int sizeBytes;
}

/// One step of a request's history. [byEmployee] says whether it was the person themselves.
class RequestEvent {
  const RequestEvent({
    required this.type,
    required this.at,
    required this.byEmployee,
    this.reason,
    this.files = const [],
  });

  factory RequestEvent.fromJson(Map<String, dynamic> json) => RequestEvent(
    type: json['type'] as String? ?? '',
    at: DateTime.parse(json['at'] as String),
    byEmployee: json['byEmployee'] as bool? ?? false,
    reason: json['reason'] as String?,
    files: _files(json['files']),
  );

  final String type;
  final DateTime at;
  final bool byEmployee;
  final String? reason;
  final List<RequestFile> files;
}

/// Mirrors the API's `DocumentRequestDto`, as the person being asked sees it.
class DocumentRequest {
  const DocumentRequest({
    required this.id,
    required this.category,
    required this.title,
    this.note,
    this.dueDate,
    required this.status,
    this.attempts = 0,
    this.returnReason,
    this.files = const [],
    this.events = const [],
  });

  factory DocumentRequest.fromJson(Map<String, dynamic> json) =>
      DocumentRequest(
        id: json['id'] as String,
        category: json['category'] as String? ?? 'Other',
        title: json['title'] as String? ?? '',
        note: json['note'] as String?,
        dueDate: json['dueDate'] is String
            ? DateTime.tryParse(json['dueDate'] as String)
            : null,
        status: json['status'] as String? ?? 'Open',
        attempts: (json['attempts'] as num?)?.toInt() ?? 0,
        returnReason: json['returnReason'] as String?,
        files: _files(json['files']),
        events: (json['events'] as List<dynamic>? ?? const [])
            .cast<Map<String, dynamic>>()
            .map(RequestEvent.fromJson)
            .toList(),
      );

  final String id;
  final String category;
  final String title;
  final String? note;
  final DateTime? dueDate;

  /// `Open`, `Submitted`, `Accepted`, `Returned` or `Cancelled`.
  final String status;
  final int attempts;
  final String? returnReason;
  final List<RequestFile> files;
  final List<RequestEvent> events;

  /// Waiting for the person to send something.
  bool get canBeAnswered => status == 'Open' || status == 'Returned';

  /// Past its date and still waiting on the person.
  bool isOverdueOn(DateTime today) {
    final due = dueDate;

    if (due == null || !canBeAnswered) {
      return false;
    }

    return DateTime(
      due.year,
      due.month,
      due.day,
    ).isBefore(DateTime(today.year, today.month, today.day));
  }
}

List<RequestFile> _files(Object? value) => (value as List<dynamic>? ?? const [])
    .cast<Map<String, dynamic>>()
    .map(RequestFile.fromJson)
    .toList();

/// A file picked on the phone, ready to be sent.
class AnswerFile {
  const AnswerFile({
    required this.path,
    required this.name,
    required this.sizeBytes,
  });

  final String path;
  final String name;
  final int sizeBytes;
}

class DocumentRequestRepository extends ApiRepository {
  const DocumentRequestRepository(super.dio);

  /// Mirrors the API's `DocumentRequestRules`, so a file is refused before it is sent.
  static const maxFiles = 5;
  static const maxFileBytes = 25 * 1024 * 1024;
  static const extensions = ['pdf', 'jpg', 'jpeg', 'png', 'webp', 'heic'];

  Future<List<DocumentRequest>> fetchMine() {
    return guard(() async {
      final response = await dio.get<List<dynamic>>(
        '/api/v1/documentrequests/mine',
      );

      return (response.data ?? const [])
          .cast<Map<String, dynamic>>()
          .map(DocumentRequest.fromJson)
          .toList();
    });
  }

  Future<DocumentRequest> fetchOne(String id) {
    return getJson('/api/v1/documentrequests/$id', DocumentRequest.fromJson);
  }

  /// Answers a request with one document, as one or several files.
  Future<DocumentRequest> submit(String id, List<AnswerFile> files) {
    return guard(() async {
      final form = FormData();

      for (final file in files) {
        form.files.add(
          MapEntry(
            'files',
            await MultipartFile.fromFile(file.path, filename: file.name),
          ),
        );
      }

      final response = await dio.post<Map<String, dynamic>>(
        '/api/v1/documentrequests/$id/submit',
        data: form,
      );

      return DocumentRequest.fromJson(response.data!);
    });
  }
}

final documentRequestRepositoryProvider = Provider<DocumentRequestRepository>((
  ref,
) {
  return DocumentRequestRepository(ref.watch(apiClientProvider));
});

/// What the office has asked of the signed-in person. Depends on the user so a shared phone never
/// shows the previous person's requests.
final myDocumentRequestsProvider =
    FutureProvider.autoDispose<List<DocumentRequest>>((ref) {
      ref.watch(currentUserProvider);

      return ref.watch(documentRequestRepositoryProvider).fetchMine();
    });

/// One request with its history. Always fetched fresh when opened.
final documentRequestProvider = FutureProvider.autoDispose
    .family<DocumentRequest, String>((ref, id) {
      ref.watch(currentUserProvider);

      return ref.watch(documentRequestRepositoryProvider).fetchOne(id);
    });
