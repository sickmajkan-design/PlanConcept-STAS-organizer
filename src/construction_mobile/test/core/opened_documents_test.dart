import 'dart:io';

import 'package:construction_mobile/core/storage/opened_documents.dart';
import 'package:construction_mobile/core/utils/secure_screen.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late Directory base;

  setUp(() async {
    base = await Directory.systemTemp.createTemp('opened_documents_test');
  });

  tearDown(() async {
    if (await base.exists()) {
      await base.delete(recursive: true);
    }
  });

  test('a document is written under a name of its own, not the document\'s name', () async {
    final first = await OpenedDocuments.write([1, 2, 3], extension: '.pdf', under: base);
    final second = await OpenedDocuments.write([4], extension: '.pdf', under: base);

    expect(first.path, isNot(second.path));
    expect(first.path.endsWith('.pdf'), isTrue);
    expect(await first.readAsBytes(), [1, 2, 3]);
  });

  test('wiping empties the folder, and is safe when there is nothing to wipe', () async {
    await OpenedDocuments.write([1], extension: '.jpg', under: base);
    await OpenedDocuments.write([2], extension: '.png', under: base);

    await OpenedDocuments.wipe(under: base);

    final folder = Directory('${base.path}/${OpenedDocuments.folderName}');
    expect(await folder.exists(), isFalse);

    // Nothing there any more: still fine.
    await OpenedDocuments.wipe(under: base);
  });

  test('only medical, identity and visa documents keep the screen secure', () {
    expect(SecureScreen.isSensitive('MedicalCheck'), isTrue);
    expect(SecureScreen.isSensitive('Identity'), isTrue);
    expect(SecureScreen.isSensitive('Visa'), isTrue);
    expect(SecureScreen.isSensitive('Contract'), isFalse);
    expect(SecureScreen.isSensitive('Photo'), isFalse);
  });
}
