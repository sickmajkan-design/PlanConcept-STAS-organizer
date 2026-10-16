import 'dart:io';

import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

/// The private folder where a document is written so another app can open it, and the means to empty it.
///
/// An external viewer cannot be given the bearer-token address, so the bytes are saved to a file first.
/// That file is a copy of somebody's paperwork sitting in the app's cache, so it is kept in one folder,
/// removed when the person leaves the screen, and the whole folder is emptied when the app starts and
/// when someone signs out — a phone that changes hands must not carry the last person's documents.
class OpenedDocuments {
  const OpenedDocuments._();

  static const folderName = 'opened_documents';

  /// Where documents are written. Created on demand.
  static Future<Directory> folder({Directory? under}) async {
    final base = under ?? await getTemporaryDirectory();
    final directory = Directory(p.join(base.path, folderName));

    if (!await directory.exists()) {
      await directory.create(recursive: true);
    }

    return directory;
  }

  /// Writes [bytes] under a name of its own and returns the file. The name carries nothing from the
  /// document's own name, so two documents with the same file name never collide.
  static Future<File> write(
    List<int> bytes, {
    required String extension,
    Directory? under,
  }) async {
    final directory = await folder(under: under);
    final stamp = DateTime.now().microsecondsSinceEpoch;
    final file = File(p.join(directory.path, 'doc_$stamp$extension'));

    return file.writeAsBytes(bytes, flush: true);
  }

  /// Removes every file this folder holds. Never throws: failing to tidy must not stop anything else.
  static Future<void> wipe({Directory? under}) async {
    try {
      final base = under ?? await getTemporaryDirectory();
      final directory = Directory(p.join(base.path, folderName));

      if (await directory.exists()) {
        await directory.delete(recursive: true);
      }
    } on Exception {
      // A file another app still holds open, or no cache folder to look in (a test): it goes next time.
    }
  }
}
