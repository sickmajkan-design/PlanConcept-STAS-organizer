import 'package:freezed_annotation/freezed_annotation.dart';

part 'attachment.freezed.dart';
part 'attachment.g.dart';

/// Mirrors the API's `AttachmentDto`.
@freezed
abstract class Attachment with _$Attachment {
  const factory Attachment({
    required String id,
    required String fileName,
    required String contentType,
    required int sizeBytes,
    required String category,
    String? description,

    /// `YYYY-MM-DD`, or null for anything that does not lapse.
    String? expiresAt,
    required String ownerType,
    required String ownerId,
    String? ownerName,
    String? uploadedByName,
    required DateTime createdAt,
  }) = _Attachment;

  const Attachment._();

  bool get isImage => contentType.startsWith('image/');

  DateTime? get expiryDate =>
      expiresAt == null ? null : DateTime.tryParse(expiresAt!);

  /// True once the document's validity has run out, against a given day.
  ///
  /// Compared date-only: a certificate valid "until the 3rd" is valid all of
  /// the 3rd, and comparing instants would expire it at midnight.
  bool isExpiredOn(DateTime today) {
    final expiry = expiryDate;

    if (expiry == null) {
      return false;
    }

    return DateTime(expiry.year, expiry.month, expiry.day)
        .isBefore(DateTime(today.year, today.month, today.day));
  }

  factory Attachment.fromJson(Map<String, dynamic> json) =>
      _$AttachmentFromJson(json);
}
