import 'package:construction_mobile/core/l10n/enum_labels.dart';
import 'package:construction_mobile/features/attachments/data/models/attachment.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

Attachment attachment({
  String contentType = 'application/pdf',
  String? expiresAt,
  String category = 'Certificate',
}) {
  return Attachment(
    id: 'a1',
    fileName: 'sertifikat.pdf',
    contentType: contentType,
    sizeBytes: 2048,
    category: category,
    expiresAt: expiresAt,
    ownerType: 'Employee',
    ownerId: 'e1',
    createdAt: DateTime.utc(2026, 1, 1),
  );
}

void main() {
  group('expiry', () {
    final today = DateTime.utc(2026, 8, 3);

    test('a document without a date never expires', () {
      expect(attachment().isExpiredOn(today), isFalse);
    });

    test('yesterday has expired', () {
      expect(attachment(expiresAt: '2026-08-02').isExpiredOn(today), isTrue);
    });

    test('the last day of validity is still valid', () {
      // A certificate valid "until the 3rd" is valid all of the 3rd; comparing
      // instants rather than dates would expire it at midnight.
      expect(attachment(expiresAt: '2026-08-03').isExpiredOn(today), isFalse);
    });

    test('tomorrow has not expired', () {
      expect(attachment(expiresAt: '2026-08-04').isExpiredOn(today), isFalse);
    });

    test('a date the API sends in a shape this build cannot read is ignored', () {
      // Better a document shown as never expiring than a screen that throws.
      final unreadable = attachment(expiresAt: 'not-a-date');

      expect(unreadable.expiryDate, isNull);
      expect(unreadable.isExpiredOn(today), isFalse);
    });
  });

  group('previewing', () {
    test('recognises an image by its content type', () {
      expect(attachment(contentType: 'image/jpeg').isImage, isTrue);
      expect(attachment(contentType: 'image/png').isImage, isTrue);
      expect(attachment(contentType: 'application/pdf').isImage, isFalse);
    });
  });

  group('json', () {
    test('reads the API shape', () {
      final decoded = Attachment.fromJson(<String, dynamic>{
        'id': 'a1',
        'fileName': 'lekarski.pdf',
        'contentType': 'application/pdf',
        'sizeBytes': 4096,
        'category': 'MedicalCheck',
        'description': null,
        'expiresAt': '2026-12-31',
        'ownerType': 'Employee',
        'ownerId': 'e1',
        'ownerName': 'Ana Anić',
        'uploadedByName': 'admin@construction.local',
        'createdAt': '2026-08-03T09:00:00Z',
      });

      expect(decoded.fileName, 'lekarski.pdf');
      expect(decoded.expiryDate, DateTime.parse('2026-12-31'));
      expect(decoded.ownerName, 'Ana Anić');
    });
  });

  group('Serbian labels', () {
    late AppLocalizations sr;
    late AppLocalizations en;

    setUp(() async {
      sr = await AppLocalizations.delegate.load(const Locale('sr'));
      en = await AppLocalizations.delegate.load(const Locale('en'));
    });

    test('translate the categories', () {
      expect(
        enumLabel(sr, EnumKind.attachmentCategory, 'MedicalCheck'),
        'Lekarski pregled',
      );
      expect(
        enumLabel(sr, EnumKind.attachmentCategory, 'SiteDocument'),
        'Gradilišna dokumentacija',
      );
      expect(
        enumLabel(sr, EnumKind.attachmentCategory, 'Contract'),
        isNot(enumLabel(en, EnumKind.attachmentCategory, 'Contract')),
      );
    });

    test('fall back readably for a category this build does not know', () {
      expect(
        enumLabel(sr, EnumKind.attachmentCategory, 'SafetyBriefing'),
        'Safety Briefing',
      );
    });
  });
}
