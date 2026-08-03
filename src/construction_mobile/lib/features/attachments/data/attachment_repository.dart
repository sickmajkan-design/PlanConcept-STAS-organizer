import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/attachment.dart';

class AttachmentRepository extends ApiRepository {
  const AttachmentRepository(super.dio);

  /// Mirrors the API's AttachmentRules, so a file is refused before it is sent.
  static const maxSizeBytes = 20 * 1024 * 1024;

  Future<List<Attachment>> fetchFor({
    required String ownerType,
    required String ownerId,
  }) {
    return guard(() async {
      final response = await dio.get<List<dynamic>>(
        '/api/attachments',
        queryParameters: <String, dynamic>{
          'ownerType': ownerType,
          'ownerId': ownerId,
        },
      );

      return (response.data ?? const [])
          .cast<Map<String, dynamic>>()
          .map(Attachment.fromJson)
          .toList();
    });
  }

  /// The raw bytes of one attachment.
  ///
  /// Fetched through the authenticated client rather than by handing a URL to
  /// `Image.network`: the endpoint needs a bearer token, and an image widget
  /// would request it without one.
  Future<Uint8List> fetchContent(String id) {
    return guard(() async {
      final response = await dio.get<List<int>>(
        '/api/attachments/$id/content',
        options: Options(responseType: ResponseType.bytes),
      );

      return Uint8List.fromList(response.data ?? const []);
    });
  }

  /// Uploads a photograph taken on site.
  Future<Attachment> uploadPhoto({
    required String projectId,
    required String filePath,
    required String fileName,
    String? description,
  }) {
    return guard(() async {
      final form = FormData.fromMap(<String, dynamic>{
        'ownerType': 'Project',
        'ownerId': projectId,
        'category': 'Photo',
        'description': ?description,
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });

      final response = await dio.post<Map<String, dynamic>>(
        '/api/attachments',
        data: form,
      );

      return Attachment.fromJson(response.data!);
    });
  }
}

final attachmentRepositoryProvider = Provider<AttachmentRepository>((ref) {
  return AttachmentRepository(ref.watch(apiClientProvider));
});

/// Attachments on one record. A family so each screen caches its own.
final attachmentsProvider = FutureProvider.autoDispose
    .family<List<Attachment>, ({String ownerType, String ownerId})>((ref, owner) {
  return ref.watch(attachmentRepositoryProvider).fetchFor(
        ownerType: owner.ownerType,
        ownerId: owner.ownerId,
      );
});
